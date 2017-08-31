using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace FrameProcessing
{
	/// <summary>
	/// AeStatix real time image analysis.
	/// 
	/// 
	/// 
	/// 
	/// 
	/// </summary>
	public class FrameProcessing : MonoBehaviour
	{	
		[SerializeField]
		bool showProcessing = true;

		[SerializeField]
		bool inversion = false;

		[SerializeField]
		bool blur = false;
		[SerializeField]
		[Range(1,20)]
		int blurSize = 3;

		[SerializeField]
		bool toneThreshold = false;
		[SerializeField]
		[Range(0,255)]
		double thresholdValue = 127.5f;
		int thresholdValueCap =2;

		[SerializeField]
		bool blobs = false;

		[SerializeField]
		bool centerPoint = false;

		List<Moments> moments = new List<Moments>();

		[SerializeField]
		bool edge = false;
		[SerializeField]
		bool edgeCenterPoint = false;

		[SerializeField]
		bool mergeCenters = false;

		int edgeThresh = 1;
		int lowThreshold;
		int  max_lowThreshold = 100;
		int ratio = 3;

		Mat hierarchy;
		List< MatOfPoint > contours = new List<MatOfPoint>(); 	

		List<Point> WeightedCentroid = new List<Point>();

		int framesDropCount =0;

		// GRAY IMG MAT
		Mat toneMat;


		public string requestedDeviceName = null;
		[SerializeField]
		bool resize = false;
		//resize ratio
		[SerializeField]
		[Range(1f,0.05f)]
		float resizeRatio = 1f;
		/// <summary>
		/// 
		/// 
		/// </summary>
		[SerializeField]
		[Range(0.0001f,4.0f)]
		float processingSecondsInterval = 0.0001f;
		/// <summary>
		/// Set the requested width of the camera device.
		/// </summary>
		public int requestedWidth = 640;

		/// <summary>
		/// Set the requested height of the camera device.
		/// </summary>
		public int requestedHeight = 480;

		//portatint
		[SerializeField]
		bool portrait = false;
		/// <summary>
		/// Set this to specify the name of the device to use.
		/// </summary>
		/// <summary>
		/// Set the requested to using the front camera.
		/// </summary>
		public bool requestedIsFrontFacing = false;

		/// <summary>
		/// The webcam texture.
		/// </summary>
		WebCamTexture webCamTexture;

		/// <summary>
		/// The webcam device.
		/// </summary>
		WebCamDevice webCamDevice;

		/// <summary>
		/// The rgba mat.
		/// </summary>
		Mat rgbaMat;

		/// <summary>
		/// The colors.
		/// </summary>
		Color32[] colors;

		//gray
		int grayscale = CvType.CV_8UC1;
		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;
		Texture2D textureGray;

		/// <summary>
		/// Indicates whether this instance is waiting for initialization to complete.
		/// </summary>
		bool isInitWaiting = false;

		/// <summary>
		/// Indicates whether this instance has been initialized.
		/// </summary>
		bool hasInitDone = false;



		[SerializeField]
		int framesToDrop = 0;



		//rgb mat
		Mat rgbMat;

		//clone mat (temp)
		Mat cloneMat;

		//blob detector
		FeatureDetector blobDetector = FeatureDetector.create(FeatureDetector.SIMPLEBLOB);
		//key point 
		MatOfKeyPoint keypoints = new MatOfKeyPoint();

		//Size var
		Size size;
		//channels array
		List<Mat> Channels = new List<Mat>();


		// Use this for initialization
		void Start ()
		{
			//
			Debug.Log ("channels mat: " + Channels);

			Initialize ();
		}

		/// <summary>
		/// Initialize of web cam texture.
		/// </summary>
		private void Initialize ()
		{
			if (isInitWaiting)
				return;

			StartCoroutine (_Initialize ());
		}

		/// <summary>
		/// Initialize of webcam texture.
		/// </summary>
		/// <param name="deviceName">Device name.</param>
		/// <param name="requestedWidth">Requested width.</param>
		/// <param name="requestedHeight">Requested height.</param>
		/// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
		private void Initialize (string deviceName, int requestedWidth, int requestedHeight, bool requestedIsFrontFacing)
		{
			if (isInitWaiting)
				return;

			this.requestedDeviceName = deviceName;
			this.requestedWidth = requestedWidth;
			this.requestedHeight = requestedHeight;
			this.requestedIsFrontFacing = requestedIsFrontFacing;

			StartCoroutine (_Initialize ());
		}

		/// <summary>
		/// Initialize of webcam texture by coroutine.
		/// </summary>
		private IEnumerator _Initialize ()
		{

			if (hasInitDone)
				Dispose ();

			isInitWaiting = true;

			if (!String.IsNullOrEmpty (requestedDeviceName)) {
				//Debug.Log ("deviceName is "+requestedDeviceName);
				webCamTexture = new WebCamTexture (requestedDeviceName, requestedWidth, requestedHeight);
			} else {
				//Debug.Log ("deviceName is null");
				// Checks how many and which cameras are available on the device
				for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
					if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestedIsFrontFacing) {

						//Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
						webCamDevice = WebCamTexture.devices [cameraIndex];
						webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);

						break;
					}
				}
			}

			if (webCamTexture == null) {
				if (WebCamTexture.devices.Length > 0) {
					webCamDevice = WebCamTexture.devices [0];
					webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
				} else {
					webCamTexture = new WebCamTexture (requestedWidth, requestedHeight);
				}
			}

			webCamTexture.requestedFPS = 60;
			// Starts the camera.
			webCamTexture.Play ();

			while (true) {
				// If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/).
				#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				if (webCamTexture.width > 16 && webCamTexture.height > 16) {
				#else
				if (webCamTexture.didUpdateThisFrame) {
				#if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2                                    
				while (webCamTexture.width <= 16) {
				webCamTexture.GetPixels32 ();
				yield return new WaitForEndOfFrame ();
				} 
				#endif
				#endif

					Debug.Log ("webcam ratio " + "( " + webCamTexture.width + ", " + webCamTexture.height + ") | fps: " + webCamTexture.requestedFPS);
					Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

					isInitWaiting = false;
					hasInitDone = true;

					OnInited ();

					break;
				} else {
					yield return 0;
				}
			}
			yield return new WaitForSeconds (processingSecondsInterval);
			Debug.Log("Yield, time is:" + Time.fixedTime);
		}

		/// <summary>
		/// Releases all resource.
		/// </summary>
		private void Dispose ()
		{
			isInitWaiting = false;
			hasInitDone = false;

			if (webCamTexture != null) {
				webCamTexture.Stop ();
				webCamTexture = null;
			}
			if (rgbaMat != null) {
				rgbaMat.Dispose ();
				rgbaMat = null;
			}
			if (toneMat != null) {
				toneMat.Dispose ();
				toneMat = null;
			}
			if (rgbMat != null) {
				rgbMat.Dispose ();
				rgbMat = null;
			}
			if (cloneMat != null) {
				cloneMat.Dispose ();
				cloneMat = null;
			}

		}

		/// <summary>
		/// Initialize completion handler of the webcam texture.
		/// </summary>
		private void OnInited ()
		{
			if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
				colors = new Color32[webCamTexture.width * webCamTexture.height];
			if (texture == null || texture.width != webCamTexture.width || texture.height != webCamTexture.height)
				texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
			if (textureGray == null || textureGray.width != webCamTexture.width || textureGray.height != webCamTexture.height)
				textureGray = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.Alpha8, false);


			// 3 OR 4 channels
			rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
			cloneMat = rgbaMat.clone ();
			//rgbMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
			toneMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
			hierarchy = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
			//toneMat = rgbaMat.clone ();
			//inversionMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1, new Scalar(255));

			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
			gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
			Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


			float width = rgbaMat.width ();
			float height = rgbaMat.height ();

			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;
			if (widthScale > heightScale) {
				Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
			} else {
				Camera.main.orthographicSize = height / 2;
			}
		}

		// Update is called once per frame
		void Update ()
		{

			framesDropCount++;

			if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {
				Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);
				Utils.webCamTextureToMat (webCamTexture, toneMat);

				//Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
				//Debug.Log ("webcam ratio " + "( " + webCamTexture.width + ", " + webCamTexture.height + ")");

				if (framesDropCount >= framesToDrop) {
					processFrame ();
				}

				Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height (), new Point ((int)Math.Round(rgbaMat.width() * 0.35), rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (180,1), 2, Imgproc.LINE_AA, false);
				Utils.matToTexture2D (rgbaMat, texture);
				//Utils.matToTexture2D (toneMat, textureGray);

			}



		}
		public void processFrame(){
			if (inversion) {
				//flip
				Core.bitwise_not (toneMat, toneMat);
			}
			if (resize) {
				Imgproc.resize(toneMat,toneMat, new Size((int)Math.Round(resizeRatio*toneMat.width()),(int)Math.Round(resizeRatio*toneMat.height())));
			}
			//
			if (toneThreshold){
				Imgproc.threshold ( toneMat, toneMat, thresholdValue, 255, Imgproc.THRESH_BINARY );
			}
			if (blobs) {
				blobDetector.detect(toneMat, keypoints);
				Features2d.drawKeypoints(toneMat, keypoints, toneMat);
			}
			if (blur) {
				Imgproc.blur( toneMat, toneMat, new Size(blurSize,blurSize) );
			}
			if (centerPoint) {
				moments.Add(Imgproc.moments (toneMat, true));
				WeightedCentroid.Add(new Point((int)Math.Round(moments[0].m10 / moments[0].m00), (int)Math.Round(moments[0].m01 / moments[0].m00)));
				Debug.Log("center: " + WeightedCentroid[0].x +", " + WeightedCentroid[0].y);
			}
			if (edge) {
				Imgproc.Canny (toneMat, toneMat, thresholdValue * 0.5 , thresholdValue);
				//Imgproc.findContours (toneMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE );
				//	
				//					foreach(MatOfPoint i in contours){
				//						Debug.Log ("contour " + i + ": " + i.ToString());
				//					}
				//Debug.Log ("contours count: " + contours.Count);
				moments.Add(Imgproc.moments (toneMat, true));
				if (WeightedCentroid.Count == 0) {
					moments.Add(Imgproc.moments (toneMat, true));
					WeightedCentroid.Add(new Point(0,0));
				}
				WeightedCentroid.Add(new Point((int)Math.Round(moments[1].m10 / moments[1].m00), (int)Math.Round(moments[1].m01 / moments[1].m00)));

				if (thresholdValue >= thresholdValueCap && edgeCenterPoint == true) {
					Imgproc.ellipse (toneMat, WeightedCentroid [1], new Size (20, 20), 1, 0.1, 360, new Scalar (180), 10);
					Imgproc.putText (toneMat, " Edge center point", WeightedCentroid [1], 0, 1.5, new Scalar (180), 5);
				}
			}
			//draw center
			if(centerPoint ){
				Imgproc.ellipse (toneMat, WeightedCentroid [0], new Size (20, 20), 1, 0.1, 360, new Scalar (180),10);
				Imgproc.putText(toneMat, " Tone center point", WeightedCentroid [0], 0, 1.5, new Scalar(180),5);
			}
			if (resize) {
				Imgproc.resize(toneMat,toneMat, new Size((int)Math.Round((1/resizeRatio)*toneMat.width()),(int)Math.Round((1/resizeRatio)*toneMat.height())));
			}
			//assign to display
			if (showProcessing) {
				rgbaMat = toneMat;
			} else {
				rgbaMat = cloneMat;
			}

			WeightedCentroid.Clear ();
			moments.Clear ();
			contours.Clear ();

			framesDropCount = 0;

		}
	
		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		void OnDestroy ()
		{
			Dispose ();
		}

		/// <summary>
		/// Raises the back button click event.
		/// </summary>
		public void OnBackButtonClick ()
		{
			#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("OpenCVForUnityExample");
			#else
			Application.LoadLevel ("OpenCVForUnityExample");
			#endif
		}

		/// <summary>
		/// Raises the play button click event.
		/// </summary>
		public void OnPlayButtonClick ()
		{
			if (hasInitDone)
				webCamTexture.Play ();
		}

		/// <summary>
		/// Raises the pause button click event.
		/// </summary>
		public void OnPauseButtonClick ()
		{
			if (hasInitDone)
				webCamTexture.Pause ();
		}

		/// <summary>
		/// Raises the stop button click event.
		/// </summary>
		public void OnStopButtonClick ()
		{
			if (hasInitDone)
				webCamTexture.Stop ();
		}

		/// <summary>
		/// Raises the change camera button click event.
		/// </summary>
		public void OnChangeCameraButtonClick ()
		{
			if (hasInitDone)
				Initialize (null, requestedWidth, requestedHeight, !requestedIsFrontFacing);
		}
	}
}