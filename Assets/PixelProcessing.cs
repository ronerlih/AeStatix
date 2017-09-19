using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace AeStatix
{	
	public class Centers{
		public int name{ get; set;}
		public Point point { get; set;}
		public Centers(int Name, Point Point){
			name = Name; //TO-DO: match int var to names of channels
			point = Point;
		}

	}
	/// <summary>
	//AeStatix - real time image analysis and feedback
	/// </summary>
	public class PixelProcessing : MonoBehaviour
	{

		//frame processing
		//show mats
		[Header("Analysis")]
		[SerializeField]
		public bool showCalcMats = false;
		int frameCount = 0;
		[SerializeField]
		//		[Range(1,100)]
		//		int skipFrames = 1;
		[Range(0.016f,5f)]
		float secondsBtwProcessing = 0.5f;

		[SerializeField]
		[Range(0.01f,1f)]
		float resizeFactor = 1f;
		[SerializeField]
		[Range(1f,5f)]
		float exaggerateData = 1;
		Size resizeSize;
		[SerializeField]
		bool displaySpeed = true;
		[SerializeField]
		[Range(0.8f,1f)]
		float speed = 0.1f;
		bool centersFlag = false;
		[Space(10)]


	

		//centers
		float x, y, z;
		List<Centers> centersObj = new List<Centers>();
		List<Centers> displayCenters = new List<Centers>();
		List<Centers> currentCenters = new List<Centers>();
		Centers averageCenter;
		// temp center point
		Point point;
		//moments array
		List<Moments> moments = new List<Moments>();

		//draw
		Scalar red = new Scalar(200,50,50,255);
		Scalar green = new Scalar(50,250,50,255);
		Scalar blue = new Scalar(50,50,250,255);
		Scalar averageColor = new Scalar(123,123,204,255);


		//edge
		[Header("Edge")]
		[SerializeField]
		bool edgeBias = false;
		[SerializeField]
		[Range(0,1)]
		double 	edgeWeight = 0.5;
		[SerializeField]
		[Range(1,30)]
		int blurSize = 10;
		[SerializeField]
		[Range(0,100)]
		double edgeGamma = 0;
		[SerializeField]
		bool thresh = true;
		[SerializeField]
		[Range(0,255)]
		double edgeThreshold = 100;
		[SerializeField]
		[Range(0,255)]
		double cannyThreshold = 100;



		//location bias
		[Header("Location")]
		[SerializeField]
		bool loactionBias = false;
		[SerializeField]
		[Range(0f,1f)]
		float rationOfScreen = 0.3333f;
		[SerializeField]
		bool drawRect = true;
		[SerializeField]
		[Range(0f,1f)]
		float locationWeight = 0.5f;
		UnityEngine.Rect unityRect;
		[Space(10)]

		//color coeficientes
		[Header("Color")]
		//weighted average
		[SerializeField]
		bool weightedAverage = false;
// TO-DO: rgb co-ef module to remove
//		[SerializeField]
		bool individualColorCoeficients = false;
		[SerializeField]
		[Range(0.01f,1f)]
		float redCoeficiente = 0.3f;
		[SerializeField]
		[Range(0.01f,1f)]
		float greenCoeficiente = 0.3f;
		[SerializeField]
		[Range(0.01f,1f)]
		float blueCoeficiente = 0.3f;
		[Space(10)]

		//snap to center
		[SerializeField]
		bool snapToCenter = true;
		[SerializeField]
		bool snapToCenterShowRect = true;

		[SerializeField]
		[Range(1,300)]
		int snapToCenterSize = 50;
		OpenCVForUnity.Rect snapToCenterRect;

		//take photo
		static int pauseFrames = 12;
		int photoStartFrame = (0 - (pauseFrames + 1));

		//trackbar
		[SerializeField]
		bool showTrackBar = true;

		float precentageToCenter = 0.1f;
		[SerializeField]
		[Range(0,150)]
		int triHight = 30;
		int[] polyVertexCountTrack = new int[3];
		int[] polyVertexCountBar = new int[3];
		Scalar trackColor = new Scalar(0,0,0,255);
		//background blue: // Scalar barColor = new Scalar(82,137,206,255);
		Scalar barColor = new Scalar(255,255,255,255);
		int nContours = 3;

		List<MatOfPoint> triangleTrack = new List<MatOfPoint>();
		List<MatOfPoint> triangleBar = new List<MatOfPoint>();

		MatOfPoint trackPoints = new MatOfPoint ();
		MatOfPoint barPoints = new MatOfPoint ();
		Point[] barPointsArray = new Point[3];
		Point centerPoint;
		float totalDistance;
		float trackbarDiffFloat;
		float frameWidth;
		float frameHeight;


		//logic frame count
		bool frameProcessingInit = false;

		/////////////////////////////////

		/// <summary>
		/// Set this to specify the name of the device to use.
		/// </summary>
		 string requestedDeviceName = null;

		/// <summary>
		/// Set the requested width of the camera device.
		/// </summary>
		 int requestedWidth = 960;

		/// <summary>
		/// Set the requested height of the camera device.
		/// </summary>
		 int requestedHeight = 540;

		/// <summary>
		/// Set the requested to using the front camera.
		/// </summary>
		 bool requestedIsFrontFacing = false;

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

		//gray mat
		Mat grayMat;

		//resize mat
		Mat resizeMat;

		//resize mat
		Mat locationMat;

		//white mat
		Mat whiteMat;

		//black mat
		Mat blackMat;
		//resize mat
		Mat GUImat;

		//submat
		Mat submat;

		//copy mat GUI
		Mat copyMat;

		//photo mat
		Mat photoMat;

		//photo border mat
		Mat photoWhiteMat;

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

		//GUI texture
		Texture2D GUItexture;

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
			//ui reset
			loactionBias = false;
			edgeBias = false;
			weightedAverage = false;

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
					webCamTexture.requestedFPS = 30;
					Debug.Log ("Camera: (" + webCamTexture.width + "px," + webCamTexture.height + "px) " + webCamTexture.requestedFPS + "fps");
					//Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

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

			//ui reset
			loactionBias = false;
			edgeBias = false;
			individualColorCoeficients = false;

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
			if (grayMat != null) {
				grayMat.Dispose ();
				grayMat = null;
			}
			if (whiteMat != null) {
				whiteMat.Dispose ();
				whiteMat = null;
			}
			if (blackMat != null) {
				blackMat.Dispose ();
				blackMat = null;
			}
			if (locationMat != null) {
				locationMat.Dispose ();
				locationMat = null;
			}
			if (resizeMat != null) {
				resizeMat.Dispose ();
				resizeMat = null;
			}
			if (submat != null) {
				submat.Dispose ();
				submat = null;
			}
			if (copyMat != null) {
				copyMat.Dispose ();
				copyMat = null;
			}
			if (GUImat != null) {
				GUImat.Dispose ();
				GUImat = null;
			}
			if (photoMat != null) {
				photoMat.Dispose ();
				photoMat = null;
			}
			if (photoWhiteMat != null) {
				photoWhiteMat.Dispose ();
				photoWhiteMat = null;
			}
		}

		/// <summary>
		/// Initialize completion handler of the webcam texture.
		/// </summary>
		private void OnInited ()
		{	
			//texture initiation
			if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
				colors = new Color32[webCamTexture.width * webCamTexture.height];
			if (texture == null || texture.width != webCamTexture.width || texture.height != webCamTexture.height)
				texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

			//mats sizes initiation
			rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
			rgbMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
			frameWidth = rgbMat.width ();
			frameHeight = rgbMat.height ();
			resizeSize = new Size ((int)Math.Round (webCamTexture.width * resizeFactor), (int)Math.Round (webCamTexture.height * resizeFactor));
			resizeMat = new Mat (resizeSize, CvType.CV_8UC3);
			Debug.Log ("analysis size: " + resizeSize.width + "px, " + resizeSize.height + "px");
			locationMat = new Mat( resizeSize, CvType.CV_8UC3, new Scalar(0,0,0));
			whiteMat = new Mat(resizeSize, CvType.CV_8UC1, new Scalar(255));
			photoWhiteMat = new Mat(webCamTexture.height,webCamTexture.width, CvType.CV_8UC3, new Scalar(255,255,255,255));
			blackMat = new Mat(resizeSize, CvType.CV_8UC3, new Scalar(0,0,0));
			copyMat = new Mat(resizeSize, CvType.CV_8UC3);
			GUImat = new Mat( resizeSize, CvType.CV_8UC1);
			grayMat = new Mat (resizeSize, CvType.CV_8UC1);
			photoMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

			//assemble location Mat
			OpenCVForUnity.Rect sub = new OpenCVForUnity.Rect (new Point((int)Math.Round( locationMat.width() * rationOfScreen),(int)Math.Round( locationMat.height() * rationOfScreen)),
				new Point((int)Math.Round( locationMat.width() * (1- rationOfScreen)),(int)Math.Round( locationMat.height() * (1 - rationOfScreen))));
			submat = new Mat (new Size(sub.width,sub.height), CvType.CV_8UC3, new Scalar (255, 255, 255));
			submat.copyTo(locationMat.colRange((int)Math.Round (locationMat.width() * rationOfScreen), (int)Math.Round (locationMat.width() * (1 - rationOfScreen) ))
				.rowRange((int)Math.Round (locationMat.height() * rationOfScreen), (int)Math.Round (locationMat.height() * (1 - rationOfScreen))));

			//average center
			averageCenter = new Centers (4,new Point(rgbMat.width()/2,rgbMat.height()/2));
				
			//textures
			if ( GUItexture == null || GUItexture.width != resizeSize.width || GUItexture.height != resizeSize.height)
				GUItexture = new Texture2D ((int)resizeSize.width, (int)resizeSize.height, TextureFormat.RGBA32, false);
			
			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

			gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
			Debug.Log ("Screen size: (" + Screen.width + "px, " + Screen.height + "px) Screen.orientation " + Screen.orientation);

			//trackBar UI
			Point centerPoint = new Point(rgbMat.width()/2,rgbMat.height()/2);
			totalDistance =(float) Math.Sqrt(( (rgbMat.width()/2 ) * ( rgbMat.width()/2) ) + ( (rgbMat.height()/2) * (rgbMat.height()/2) )); 
			//Debug.Log("max distance from center (trackbar feedback): " + totalDistance + "px\n");
			Point[] trackPointArray = new Point[3] {
				new Point (0, rgbMat.height()),
				new Point (rgbMat.width(), rgbMat.height() - triHight),
				new Point (rgbMat.width(), rgbMat.height())
			};
			//bar points
			Point bottomLeft = new Point (0, rgbMat.height());
			Point topRight = bottomLeft;
			Point bottomRight = bottomLeft;
			barPointsArray = new Point[] {bottomLeft,topRight,bottomRight};

			barPoints = new MatOfPoint (barPointsArray);
			trackPoints = new MatOfPoint(trackPointArray);

			triangleTrack.Add (trackPoints);
			triangleBar.Add (barPoints);

			//camera position
			float width = rgbaMat.width ();
			float height = rgbaMat.height ();
			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;

			if (widthScale < heightScale) {
				Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) /2;
				Camera.main.transform.position = new Vector3 (Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z + 10);
			} else {
				Camera.main.orthographicSize = height / 2;
				Camera.main.transform.position = new Vector3 (Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z + 10);
			}

			//start processing
			StartCoroutine(processFrame());
		}

		// called once per frame
		void Update ()
		{	
			//got a camera frame
			if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {
				if (frameCount >= photoStartFrame + pauseFrames) {

					if (frameProcessingInit) {
						Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);
						Utils.webCamTextureToMat (webCamTexture, rgbMat, colors);

						//green LOCATION rect GUI
						if (showCalcMats && loactionBias && drawRect) {
							Point locationPoint1 = new Point ((int)Math.Round (rgbaMat.width () * rationOfScreen), (int)Math.Round (rgbaMat.height () * rationOfScreen));
							Point locationPoint2 = new Point ((int)Math.Round (rgbaMat.width () * (1 - rationOfScreen)), (int)Math.Round (rgbaMat.height () * (1 - rationOfScreen)));
							Imgproc.rectangle (rgbaMat, locationPoint1, locationPoint2, green, 2);
							Imgproc.putText (rgbaMat, "location bias", locationPoint1, 0, 0.8, green, 2);
						}
						if (snapToCenterShowRect) {
							Imgproc.rectangle (rgbaMat, new Point ((rgbaMat.width () / 2) - snapToCenterSize, (rgbaMat.height () / 2) - snapToCenterSize), new Point ((rgbaMat.width () / 2) + snapToCenterSize, (rgbaMat.height () / 2) + snapToCenterSize), blue, 2);
							Imgproc.putText (rgbaMat, "snap to center", new Point ((rgbaMat.width () / 2) - snapToCenterSize, (rgbaMat.height () / 2) - snapToCenterSize - 5), 0, 0.8, blue, 2);
						}
						Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " | analysing frame every " + secondsBtwProcessing + "s", new Point (5, 28), 0, 0.8, green, 2);
						//draw centers

						Debug.Log ("before snapToCenter");

						if (snapToCenter) {
							SnapToCenters ();
						}
						Debug.Log ("before checkForCentersData");

						checkForCentersData ();
						centersFlag = true;

						Debug.Log ("after checkForCentersData\n red center: " + currentCenters [0].point);

						if (weightedAverage) {
							Imgproc.circle (rgbaMat, averageCenter.point, 3, averageColor, 5);
							Imgproc.putText (rgbaMat, "  weighted average", averageCenter.point, 2, 1, averageColor, 1);
						} else {
							for (int c = 0; c < currentCenters.Count; c++) {
								switch (c) {
								case 0:
									Imgproc.circle (rgbaMat, currentCenters [c].point, 3, red, 5);
									Imgproc.putText (rgbaMat, "  red", currentCenters [c].point, 2, 1, red, 1);
									break;
								case 1:
									Imgproc.circle (rgbaMat, currentCenters [c].point, 3, green, 5);
									Imgproc.putText (rgbaMat, "  green", currentCenters [c].point, 2, 1, green, 1);
									break;
								case 2:
									Imgproc.circle (rgbaMat, currentCenters [c].point, 3, blue, 5);
									Imgproc.putText (rgbaMat, "  blue", currentCenters [c].point, 2, 1, blue, 1);
									break;
								default:
									Imgproc.circle (rgbaMat, currentCenters [c].point, 3, red, 5);
									Imgproc.putText (rgbaMat, "  default", currentCenters [c].point, 2, 1, red, 1);
									break;
								}
							}
						}

						//trackBar
						if (showTrackBar) {
							//Imgproc.fillPoly (rgbaMat, triangleTrack, trackColor,Imgproc.LINE_AA,0,new Point(0,0));
							if (currentCenters [0] != null) {
								precentageToCenter = TrackbarDiff (averageCenter.point);
								Imgproc.fillPoly (rgbaMat, TriangleBar (precentageToCenter), barColor, Imgproc.LINE_AA, 0, new Point (0, 0));
							}
						}

						Utils.matToTexture2D (rgbaMat, texture, colors);
					}
				} else {
					// photo border
					photoWhiteMat.colRange(0,20).copyTo(rgbMat.colRange(0,20));
					photoWhiteMat.colRange(photoWhiteMat.cols() -20 ,photoWhiteMat.cols()).copyTo(rgbMat.colRange(photoWhiteMat.cols() -20 ,photoWhiteMat.cols()));
					photoWhiteMat.rowRange(0,20).copyTo(rgbMat.rowRange(0,20));
					photoWhiteMat.rowRange(photoWhiteMat.rows() -20 ,photoWhiteMat.rows()).copyTo(rgbMat.rowRange(photoWhiteMat.rows() -20 ,photoWhiteMat.rows()));

					Utils.matToTexture2D (rgbMat, texture, colors);
				}
				frameCount++;

				Debug.Log ("frame no.: " + frameCount);
			}

		}
		public void SnapToCenters(){
			if (frameCount >=0){
				for (int q = 0; q < displayCenters.Count; q++) {
					//channel center inside rect
					if (displayCenters [q].point.x >= (rgbaMat.width () / 2) - snapToCenterSize
					   && displayCenters [q].point.x <= (rgbaMat.width () / 2) + snapToCenterSize
					   && displayCenters [q].point.y >= (rgbaMat.height () / 2) - snapToCenterSize
					   && displayCenters [q].point.y <= (rgbaMat.height () / 2) + snapToCenterSize) {

						displayCenters [q].point = new Point (rgbaMat.width () / 2, rgbaMat.height () / 2);
					}
				}
			}
		}
		public void checkForCentersData(){
			//check for first tiem frame processing - Initiate centers - place in the center
			if (displayCenters!= null && currentCenters.Count == 0 || frameCount <= 4 || !centersFlag) {
				currentCenters.Clear ();
				//initiate currentCenters
				for (int d = 0; d < displayCenters.Count; d++) {
						currentCenters.Add (new Centers (d, new Point (rgbaMat.width () * 0.5, rgbaMat.height () * 0.5)));
					}
			}
				
			// currentCenters step
			for (int h = 0; h < displayCenters.Count; h++) {
				currentCenters [h].point.x = speed * currentCenters [h].point.x + displayCenters [h].point.x * (1 - speed);
				currentCenters [h].point.y = speed * currentCenters [h].point.y + displayCenters [h].point.y * (1 - speed);
			}
			//centers center - weighted average
				averageCenter.point = WeightedAverageThree (currentCenters [0].point, currentCenters [1].point, currentCenters [2].point);
				//Debug.Log ("average point: " + averageCenter.point);

		}

		private IEnumerator processFrame(){
			while (true) {
				//resize down
				if (resizeMat != null) {
					resizeSize = new Size ((int)Math.Round (webCamTexture.width * resizeFactor), (int)Math.Round (webCamTexture.height * resizeFactor));

					frameProcessingInit = true;
					//resizeMat = new Mat (resizeSize, CvType.CV_8UC3);
					Imgproc.resize (rgbMat, resizeMat, resizeSize, 0.5, 0.5, Core.BORDER_DEFAULT);

					//clear last cenbters
					displayCenters.Clear();

					//edge detection and wights
					if(edgeBias){
						grayMat = resizeMat.clone ();
						Imgproc.cvtColor( grayMat, grayMat, Imgproc.COLOR_RGB2GRAY);

						Imgproc.Canny (grayMat, grayMat, cannyThreshold, cannyThreshold);
						Imgproc.blur (grayMat, grayMat, new Size (blurSize, blurSize));
						if(thresh){
						Imgproc.threshold ( grayMat, grayMat, edgeThreshold, 255, Imgproc.THRESH_BINARY );
						}


						//weights
						Imgproc.cvtColor (grayMat,grayMat, Imgproc.COLOR_GRAY2RGB);
						//Debug.Log("sample pixel before calc: " + resizeMat.get (100, 100).GetValue(0));
						Core.addWeighted(resizeMat, (1 - edgeWeight), grayMat, edgeWeight , edgeGamma, resizeMat);
						//Debug.Log("sample pixel after calc: " + resizeMat.get (100, 100).GetValue(0));
					}

					if (loactionBias) {
						//TO-DO: weighted average CHANGE + track bar################################
						Core.addWeighted(resizeMat, (1 - locationWeight), locationMat, locationWeight , 0.0, resizeMat);
					}

					//split channels
					Core.split (resizeMat, channels);

					//center for each channel
					for (int i = 0; i < channels.Count; i++) {
						displayCenters.Add(getCenterPointFromMat (channels[i], i));
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
			point = new Point ((moments [channel].m10 / moments [channel].m00), (moments [channel].m01 / moments [channel].m00));

			//resize point up
			point.x = map((float)point.x,0,(float)resizeSize.width ,(float)webCamTexture.width - (float)webCamTexture.width * exaggerateData,(float)webCamTexture.width * exaggerateData) ;
			point.y = map((float)point.y,0,(float)resizeSize.height ,(float)webCamTexture.height - (float)webCamTexture.height * exaggerateData,(float)webCamTexture.height * exaggerateData) ;

			centersObj.Add(new Centers(channel, point) );

			return centersObj [channel];
		}



		public void takePhoto(){
			Debug.Log ("TAKE PHOTO");
			photoStartFrame = frameCount;

			//TO-DO: PLAY audio
			AudioSource audio = GetComponent<AudioSource>();		
			audio.Play ();
			//write to singleton
			ImageManager.instance.photo = texture;
			//TO-DO: emmit event for Markus

			//camera to Jpeg rgba to bgr
			Imgproc.cvtColor (rgbaMat, photoMat, Imgproc.COLOR_RGBA2BGR);
			//write image
			Imgcodecs.imwrite ("Assets/snapshot-with-data.jpeg", photoMat);
			Imgproc.cvtColor (rgbMat, photoMat, Imgproc.COLOR_RGB2BGR);
			Imgcodecs.imwrite ("Assets/snapshot-photo.jpeg", photoMat);



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
			SceneManager.LoadScene ("AeStatix");
			#else
			Application.LoadLevel ("AeStatix");
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
		void OnGUI(){
			if (showCalcMats && frameCount >= 10) {
				
				//only black rect
				unityRect = new UnityEngine.Rect (5f, 20f, (float)resizeSize.width /2, (float)resizeSize.height/2 );

				if (!loactionBias && !edgeBias) {
					GUImat = blackMat.clone ();
					}
				if (loactionBias && !edgeBias && GUItexture != null) {
					//GUImat = blackMat.clone ();
				
					Core.addWeighted (locationMat, locationWeight, blackMat, (1 - locationWeight), 0.0, GUImat);


					}
				if (edgeBias && loactionBias) {
					Core.addWeighted (blackMat, (1-edgeWeight), grayMat, (edgeWeight), edgeGamma, GUImat);
					Core.addWeighted (locationMat, locationWeight, GUImat, (1 - locationWeight), 0.0, GUImat);


				}
				if ( !loactionBias && edgeBias  ) {

					GUImat = grayMat.clone ();
					Core.addWeighted (blackMat, (1-edgeWeight), grayMat, (edgeWeight), edgeGamma, GUImat);

				}

				Utils.matToTexture2D (GUImat, GUItexture);
				GUI.DrawTexture (unityRect, GUItexture);

			}

		}

		//ui controls
		public void showEdge(){
			edgeBias = !edgeBias;
		}
		public void showCenter(){
			loactionBias = !loactionBias;
		}
		public void showcolor(){
			weightedAverage = !weightedAverage;
		}
		//calculate the trackbar bar
		public List<MatOfPoint> TriangleBar(float _percentToCenter){
			barPointsArray = new Point[]{ new Point (0, rgbMat.height()),
				new Point (rgbaMat.width()  * _percentToCenter, (rgbaMat.height() - (triHight * _percentToCenter))),
				new Point(rgbaMat.width()  * _percentToCenter, rgbMat.height())};
			
//			foreach (Point _point in barPointsArray) {
//				Debug.Log ("point = " + _point);
//			}

			barPoints = new MatOfPoint(barPointsArray);

			triangleBar.Clear ();
			triangleBar.Add (barPoints);

			return triangleBar;
			//either wnough kto sync or clear and add array to mat and to list

		}
		public float TrackbarDiff(Point _current){

			trackbarDiffFloat = (float)(Math.Sqrt ((
				(_current.x - ( frameWidth/2) ) * (_current.x - (frameWidth/2) )) + ((_current.y - (frameHeight/2) ) * (_current.y-(frameHeight/2) ))) );

			if (trackbarDiffFloat > totalDistance - 10)
				trackbarDiffFloat = totalDistance;
			if (trackbarDiffFloat < 10)
				trackbarDiffFloat = 0;

			return (1 - trackbarDiffFloat/totalDistance);
		}
				public Point WeightedAverageThree(Point _redPoint, Point _greenPoint, Point _bluePoint){
					return new Point( ((_redPoint.x * redCoeficiente) + (_greenPoint.x * greenCoeficiente) + (_bluePoint.x * blueCoeficiente)) / (redCoeficiente + greenCoeficiente + blueCoeficiente),
						    		  ((_redPoint.y * redCoeficiente) + (_greenPoint.y * greenCoeficiente) + (_bluePoint.y * blueCoeficiente)) / (redCoeficiente + greenCoeficiente + blueCoeficiente));
		}

	}

}