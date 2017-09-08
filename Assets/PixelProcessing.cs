using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{	
	public class Centers{
		public int name{ get; set;}
		public Point point{ get; set;}
		public Centers(int Name, Point Point){
			name = Name;
			point = Point;
		}

	}
	/// <summary>
	/// WebCamTexture to mat example.
	/// An example of converting the WebCamTexture image to OpenCV's Mat format.
	/// </summary>
	public class PixelProcessing : MonoBehaviour
	{

		//frame processing
		int frameCount = 0;
		[SerializeField]
//		[Range(1,100)]
//		int skipFrames = 1;
		[Range(0.016f,5f)]
		float secondsBtwProcessing = 0.5f;

		[SerializeField]
		[Range(0.01f,1f)]
		float resizeFactor = 1f;
		Size resizeSize;

		//centers
		float x, y, z;
		//	List <Point> centers;
		List<Centers> centersObj = new List<Centers>();
		List<Centers> displayCenters = new List<Centers>();
		// temp center point
		Point point;
		//moments array
		List<Moments> moments = new List<Moments>();

		//draw
		Scalar red = new Scalar(200,50,50,255);
		Scalar green = new Scalar(50,250,50,255);
		Scalar blue = new Scalar(50,50,250,255);

		//pointSpeed
		[SerializeField]
		[Range(1,50)]
		int speed = 1;

		/////////////////////////////////

		/// <summary>
		/// Set this to specify the name of the device to use.
		/// </summary>
		public string requestedDeviceName = null;

		/// <summary>
		/// Set the requested width of the camera device.
		/// </summary>
		public int requestedWidth = 640;

		/// <summary>
		/// Set the requested height of the camera device.
		/// </summary>
		public int requestedHeight = 480;

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

		// MATS:

		/// <summary>
		/// The rgba mat.
		/// </summary>
		Mat rgbaMat;

		/// The rgb mat.
		Mat rgbMat;

		//resize mat
		Mat resizeMat;

		//channels List
		List<Mat> channels = new List<Mat>();

		//center Points list
		List<Point> centerPoints = new List<Point>();

		/// <summary>
		/// The colors.
		/// </summary>
		Color32[] colors;

		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// Indicates whether this instance is waiting for initialization to complete.
		/// </summary>
		bool isInitWaiting = false;

		/// <summary>
		/// Indicates whether this instance has been initialized.
		/// </summary>
		bool hasInitDone = false;

		// Use this for initialization
		void Start ()
		{
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

					Debug.Log ("name " + webCamTexture.name + " width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
					Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

					isInitWaiting = false;
					hasInitDone = true;

					OnInited ();

					break;
				} else {
					yield return 0;
				}
			}
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
			if (rgbMat != null) {
				rgbMat.Dispose ();
				rgbMat = null;
			}
			if (resizeMat != null) {
				resizeMat.Dispose ();
				resizeMat = null;
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


			rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
			rgbMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

			resizeSize = new Size ((int)Math.Round (webCamTexture.height * resizeFactor), (int)Math.Round (webCamTexture.width * resizeFactor));
			resizeMat = new Mat (resizeSize, CvType.CV_8UC3);

			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

			gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
			Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


			float width = rgbaMat.width ();
			float height = rgbaMat.height ();

			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;
			if (widthScale < heightScale) {
				Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
			} else {
				Camera.main.orthographicSize = height / 2;
			}


			//start processing
			StartCoroutine(processFrame());
		}

		// called once per frame
		void Update ()
		{
			if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {
				Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);
				Utils.webCamTextureToMat (webCamTexture, rgbMat, colors);


				Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
				//draw center
				point = new Point (x, y);

				for (int c = 0; c < displayCenters.Count; c++) {
					switch(c){
					case 0:
						Imgproc.circle (rgbaMat, displayCenters [c].point, 20, red, 40);
						Imgproc.putText( rgbaMat, "  red", displayCenters[c].point, 2,2, red,3);
						break;
					case 1:
						Imgproc.circle (rgbaMat, displayCenters [c].point, 20, green, 40);
						Imgproc.putText( rgbaMat, "  green" , displayCenters[c].point, 2,2, green,3);
						break;
					case 2:
						Imgproc.circle (rgbaMat, displayCenters [c].point, 20, blue, 40);
						Imgproc.putText( rgbaMat, "  blue" , displayCenters[c].point, 2,2, blue,3);
						break;
					default:
						Imgproc.circle (rgbaMat, displayCenters [c].point, 20, red, 40);
						Imgproc.putText( rgbaMat, "  default" , displayCenters[c].point, 2,2, red,3);
						break;
					}
				}
				Utils.matToTexture2D (rgbaMat, texture, colors);

				frameCount++;
			}
		}

		private IEnumerator processFrame(){
			while (true) {
				//resize down
				if (resizeMat != null) {
					resizeSize = new Size ((int)Math.Round (webCamTexture.height * resizeFactor), (int)Math.Round (webCamTexture.width * resizeFactor));
					//resizeMat = new Mat (resizeSize, CvType.CV_8UC3);
					Imgproc.resize (rgbMat, resizeMat, resizeSize, 0.5, 0.5, Core.BORDER_DEFAULT);

					//clear last cenbters
					displayCenters.Clear();

					//split channels
					Core.split (resizeMat, channels);

					//center for each channel
					for (int i = 0; i < channels.Count; i++) {
						Debug.Log ("channel " + i + "is: " + channels [i]);
						displayCenters.Add(getCenterPointFromMat (channels[i], i));
					//	);
					}

					moments.Clear ();
					centersObj.Clear ();
				}
				yield return new WaitForSeconds(secondsBtwProcessing);
			}
		}

		public Centers getCenterPointFromMat(Mat _mat, int channel){

// 3rd order moment center of mass

			moments.Add(Imgproc.moments(_mat,false));
			point = new Point ((int)Math.Round (moments [channel].m10 / moments [channel].m00), (int)Math.Round (moments [channel].m01 / moments [channel].m00));

			//resize point up
			point.x = map((float)point.x,0,(float)resizeSize.width,0,(float)webCamTexture.width) ;
			point.y = map((float)point.y,0,(float)resizeSize.height,0,(float)webCamTexture.height) ;

			centersObj.Add(new Centers(channel, point) );
		
			return centersObj [channel];


//avaerage mean

//			Mat row_mean = new Mat(1,1, CvType.CV_8UC1 );
//			Mat col_mean = new Mat(1,1, CvType.CV_8UC1 );
//			//to-do: innitiate point onStart
//			Core.reduce (_mat,row_mean, 0, Core.REDUCE_AVG);
//			Core.reduce (_mat,col_mean, 1, Core.REDUCE_AVG);
//
//			Debug.Log ("row_mean: " + row_mean);
//			Debug.Log ("col_mean: " + col_mean);


///run through pixels
//
//			Byte[] buff = new Byte[1];
//			long xPos = 0;
//			long yPos = 0;
//			long zPos = 0;
//
//
//			for(int j = 0;j < _mat.rows(); j++){
////			     for memory address - to-do
////				 IntPtr _matJ = _mat.nativeObj;
//				 yPos += j;
//		 		 Debug.Log ("");
//				 for(int k = 0; k < _mat.cols(); k++){
//					_mat.get (k, j, buff);
//					//Debug.Log ("row number: " + j + "###" + Environment.NewLine + ", col number: "+ k + "### delta time: " + Time.deltaTime);
//					Debug.Log ("value: " + buff[0]);
//					xPos += k;
//					zPos += buff [0];
//
//				 }
//			}
//				
//
//			z =  (xPos / _mat.cols () + yPos / _mat.rows ()) / 255;
//			x = ((zPos / 255) + (yPos / _mat.rows ())) / _mat.cols ();
//			y = ((zPos / 255) + (xPos / _mat.cols ())) / _mat.rows ();
//
//			Debug.Log("x, y, z: " + x + ", " + y + ", " + z);
//			Debug.Log ("mat rows: " + _mat.rows ());
//			Debug.Log ("mat cols: " + _mat.cols ());
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
		public float map(float s, float a1, float a2, float b1, float b2)
		{
			return b1 + (s-a1)*(b2-b1)/(a2-a1);
		}

	}
}