using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Runtime.InteropServices;
using System.IO;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace AeStatix
{	
	


//	// Unity to xcode and back
//	[DllImport ("__Internal")]
//	static class  PhotoFromUnity ();

	/// <summary>
	//AeStatix - real time image analysis and feedback
	/// </summary>
	public class PixelProcessing : MonoBehaviour
	{
		// costume center object
		public class Centers{
			public int name{ get; set;}
			public Point point { get; set;}
			public Centers(int Name, Point Point){
				name = Name; //TO-DO: match int var to names of channels
				point = Point;
			}
		}
		//frame processing
		//show mats
		[Header("Analysis")]
		[SerializeField]
		public bool showCalcMats = false;
		int frameCount = 0;
		[SerializeField]
		[Range(0.016f,5f)]
		float secondsBtwProcessing = 0.5f;

		[SerializeField]
		[Range(0.01f,1f)]
		float resizeFactor = 1f;
		[SerializeField]
		[Range(1f,5f)]
		float exaggerateData = 1;
		[SerializeField]
		[Range(0,10)]
		int exaggerateDataFace = 4;
		Size resizeSize;
		Size PyrSize;
		[SerializeField]
		[Range(0.8f,1f)]
		float speed = 0.85f;
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
		Scalar crossColor = new Scalar(170,170,170,255);
		Scalar guideColor = new Scalar(50,250,50,255);
		Scalar UIgreen = new Scalar(168,221,168,255);
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
		[SerializeField]
		[Range(1,300)]
		int snapToCenterSizeFace = 20;
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
		Point zeroPoint = new Point (0, 0);

		List<MatOfPoint> triangleTrack;
		List<MatOfPoint> triangleBar;

		MatOfPoint trackPoints;
		MatOfPoint barPoints;
		Point[] barPointsArray;
		Point centerPoint;
		Point pointForTrackBarDiff;
		float totalDistance;
		float trackbarDiffFloat;
		float frameWidth;
		float frameHeight;
		Point middleOfTheFramePoint;


		//logic frame count
		bool frameProcessingInit = false;

		//ui cross
		[SerializeField]
		bool cross = true;
		[SerializeField]
		bool guide = true;

		//build prefrences
		[SerializeField]
		bool webglBuild = false;
		[SerializeField]
		bool iosBuild = true;

		//file upload
		[SerializeField]
		Texture2D fileUpload;
		bool fileUploadFlag = false;
		[Space(10)]

		//faceDetection
		[SerializeField]
		bool faceDetection = false;
		CascadeClassifier cascade;
		MatOfRect faces;
		OpenCVForUnity.Rect[] rects;
		List<int> displayFacePoints = new List<int>();
		List<int> currentFacePoints = new List<int>();
		bool facesFlag = false;
		int lastFaceFrame = 0;
		[SerializeField]
		[Range(1,20)]
		int numberOfFramesWithNoFace = 10;
		OpenCVForUnity.Range horiRange;
		OpenCVForUnity.Range vertRange;
		int[] intMaxDetections = new int[1];
		MatOfInt maxDetections; 
		bool flippedForPhoto = false;
		int faceMiddleX = 0;
		int faceMiddleY = 0;
		bool trackbarFace = false;

		Color faceBackgroundColor = new Color (255, 255, 0,255);
		Scalar faceSubmatColor = new Scalar (0, 0, 0, 60);
		Size maxFaceSize;
		int rectsY;
		int rectsWidth;
		[Space(10)]

		//heatmap
		[SerializeField]
		bool heatmap = true;
		[SerializeField]
		Size blurKernalSize = new Size (15, 15);
		[SerializeField]
		[Range(-10,50)]
		int int1 =0;
		[SerializeField]
		[Range(-10,50)]
		int int2 =0;
		/////////////////////////////////

		/// <summary>
		/// Set this to specify the name of the device to use.
		/// </summary>
		 string requestedDeviceName = null;

		/// <summary>
		/// Set the requested width of the camera device.
		/// </summary>
		//int requestedWidth = 1534;
		int requestedWidth = 960;
		
		/// <summary>
		/// Set the requested height of the camera device.
		/// </summary>
		//int requestedHeight = 1050;
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
		//gray mat
		Mat grayFaceMat;

		//faceMat
		Mat faceMat;
		//to copy to resize mat
		Mat faceRefMat;

		//resize mat
		Mat resizeMat;	

		//resize mat
		Mat heatmapMat;

		//resize mat
		Mat locationMat;

		//white mat
		Mat whiteMat;

		//black mat
		Mat blackMat;
		//resize mat
		Mat GUImat;

		//file upload mat
		Mat fileUploadMat;
		byte[] fileUploadData;

		//submat
		Mat submat;
		//submat
		Mat faceSubmat;

		//copy mat GUI
		Mat copyMat;

		//photo mat
		Mat photoMat;

		//photo border mat
		Mat photoWhiteMat;

		//pyramid mat for heatmap
		Mat pyrMat;
		Mat pyrMatRGB;
		//cascade rotate mat
		Mat rotateMat;
		//channels List
		List<Mat> channels;

		//center Points list
		List<Point> centerPoints;

		/// <summary>
		/// The colors.
		/// </summary>
		Color32[] colors;
		Color32[] fileColors;

		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// file texture.
		/// </summary>
		Texture2D fileTexture;

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
			cross = false;
			guide = false;
			faceDetection = false;
			heatmap = false;

			//build settings
			if (iosBuild) {
				requestedWidth = 1534;
				requestedHeight = 1050;
			}
			if (webglBuild) {
				requestedWidth = 960;
				requestedHeight = 540;
			}

			//webGL load cascade

			#if UNITY_WEBGL && !UNITY_EDITOR
			var getFilePath_Coroutine = Utils.getFilePathAsync("lbpcascade_frontalface.xml", 
			(result) => {
			coroutines.Clear ();

			cascade = new CascadeClassifier ();
			cascade.load(result);
			Initialize ();
			}, 
			(result, progress) => {
			Debug.Log ("getFilePathAsync() progress : " + result + " " + Mathf.CeilToInt (progress * 100) + "%");
			});
			coroutines.Push (getFilePath_Coroutine);
			StartCoroutine (getFilePath_Coroutine);
			#else
			cascade = new CascadeClassifier ();
			cascade = new CascadeClassifier (Utils.getFilePath ("lbpcascade_frontalface.xml"));
//			cascade = new CascadeClassifier (Utils.getFilePath ("lbpcascade_frontalface_improved.xml"));
			//cascade.load (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));

			//            if (cascade.empty ()) {
			//                Debug.LogError ("cascade file is not loaded.Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
			//            }
			Initialize ();
			#endif

			// init before face detection
			//Initialize ();
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
				Debug.Log ("deviceName is " + requestedDeviceName);
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

			Debug.Log ("<unity> in camera initiation\n" +
				"<unity> webCamTexture.videoRotationAngle: " + webCamTexture.videoRotationAngle +"\n" +
				"<unity> name" + webCamTexture.deviceName + "\n" +
				"<unity> webCamTexture.videoVerticallyMirrored: " + webCamTexture.videoVerticallyMirrored + "\n" +
				"<unity> webCamTexture.wrapMode: " + 	webCamTexture.wrapMode );
			// Starts the camera.
			webCamTexture.Play ();

			while (true) {
				// If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/).
				#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				if (webCamTexture.width > 16 && webCamTexture.height > 16) {
				#else
				if (webCamTexture.didUpdateThisFrame && webCamTexture.width > 100) {
				#if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2                                    
				while (webCamTexture.width <= 16) {
				webCamTexture.GetPixels32 ();
				yield return new WaitForEndOfFrame ();
				} 
				#endif
				#endif
					webCamTexture.requestedFPS = 30;
					Debug.Log ("<unity> Camera: (" + webCamTexture.width + "px," + webCamTexture.height + "px) " + webCamTexture.requestedFPS + "fps");
					//Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

					Debug.Log("<unity> bfr OnInitiated(), camera texture width" + webCamTexture.width );

					isInitWaiting = false;
					hasInitDone = true;

					OnInited ();

					break;
				} else {
					Debug.Log("<unity> yield, camera texture width: " + webCamTexture.width );
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
			faceDetection = false;
			heatmap = false;

			//individualColorCoeficients = false;

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
			if (fileUploadMat != null) {
				fileUploadMat.Dispose ();
				fileUploadMat = null;
			}
			if (grayMat != null) {
				grayMat.Dispose ();
				grayMat = null;
			}
			if (grayFaceMat != null) {
				grayFaceMat.Dispose ();
				grayFaceMat = null;
			}
			if (faceMat != null) {
				faceMat.Dispose ();
				faceMat = null;
			}
			if (faceRefMat != null) {
				faceRefMat.Dispose ();
				faceRefMat = null;
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
			if (heatmapMat != null) {
				heatmapMat.Dispose ();
				heatmapMat = null;
			}
			if (submat != null) {
				submat.Dispose ();
				submat = null;
			}
			if (faceSubmat != null) {
				faceSubmat.Dispose ();
				faceSubmat = null;
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
			if (rotateMat != null) {
				rotateMat.Dispose ();
				rotateMat = null;
			}
			if (pyrMat != null) {
				pyrMat.Dispose ();
				pyrMat = null;
			}
			if (pyrMatRGB != null) {
				pyrMatRGB.Dispose ();
				pyrMatRGB = null;
			}
		}

		////////////
		/// MAIN INITIATION (after getting the WebcamTexture)
		///////////

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
			PyrSize = new Size ((int)Math.Round (webCamTexture.width * resizeFactor/1.3 ), (int)Math.Round (webCamTexture.height * resizeFactor/1.3));
			pyrMat = new Mat (PyrSize, CvType.CV_8UC3);
			pyrMatRGB = new Mat (PyrSize, CvType.CV_8UC3);
			resizeMat = new Mat (resizeSize, CvType.CV_8UC3);
			rotateMat = new Mat ((int) resizeSize.width,(int)resizeSize.height, CvType.CV_8UC3);
			heatmapMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
			Debug.Log ("<unity> analysis size: " + resizeSize.width + "px, " + resizeSize.height + "px");
			locationMat = new Mat( resizeSize, CvType.CV_8UC3, new Scalar(0,0,0));
			whiteMat = new Mat(resizeSize, CvType.CV_8UC1, new Scalar(255));
			photoWhiteMat = new Mat(webCamTexture.height,webCamTexture.width, CvType.CV_8UC3, new Scalar(255,255,255,255));
			blackMat = new Mat(resizeSize, CvType.CV_8UC3, new Scalar(0,0,0));
			copyMat = new Mat(resizeSize, CvType.CV_8UC3);
			GUImat = new Mat( resizeSize, CvType.CV_8UC1);
			grayMat = new Mat (resizeSize, CvType.CV_8UC1);
			grayFaceMat = new Mat (resizeSize, CvType.CV_8UC1);
			faceMat = new Mat (resizeSize, CvType.CV_8UC1);
			faceRefMat = new Mat (resizeSize, CvType.CV_8UC3);
			photoMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

			channels = new List<Mat>();

			//assemble location Mat
			OpenCVForUnity.Rect sub = new OpenCVForUnity.Rect (new Point((int)Math.Round( locationMat.width() * rationOfScreen),(int)Math.Round( locationMat.height() * rationOfScreen)),
				new Point((int)Math.Round( locationMat.width() * (1- rationOfScreen)),(int)Math.Round( locationMat.height() * (1 - rationOfScreen))));
			submat = new Mat (new Size(sub.width,sub.height), CvType.CV_8UC3, new Scalar (255, 255, 255));
			submat.copyTo(locationMat.colRange((int)Math.Round (locationMat.width() * rationOfScreen), (int)Math.Round (locationMat.width() * (1 - rationOfScreen) ))
				.rowRange((int)Math.Round (locationMat.height() * rationOfScreen), (int)Math.Round (locationMat.height() * (1 - rationOfScreen))));

			//average center
			averageCenter = new Centers (4,new Point(webCamTexture.width/2,webCamTexture.height/2));
			middleOfTheFramePoint = new Point (webCamTexture.width/2, webCamTexture.height/2);
				
			displayCenters.Clear();
			for(int c = 0; c<3 ; c++){
				currentCenters.Add (new Centers(c, new Point(webCamTexture.width/2,webCamTexture.height/2)));
			}
			displayCenters.Clear ();
			for(int c = 0; c<3 ; c++){
				displayCenters.Add (new Centers(c, new Point(webCamTexture.width/2,webCamTexture.height/2)));
			}

			//faces
			faces = new MatOfRect ();
			faceSubmat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
			intMaxDetections[0] = 1;
			maxDetections = new MatOfInt (intMaxDetections); 
			maxFaceSize = new Size (frameWidth, frameHeight);

			//textures
			if ( GUItexture == null || GUItexture.width != resizeSize.width || GUItexture.height != resizeSize.height)
				GUItexture = new Texture2D ((int)resizeSize.width, (int)resizeSize.height, TextureFormat.RGBA32, false);
			
			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

			gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
			Debug.Log ("<unity> Screen size: (" + Screen.width + "px, " + Screen.height + "px) Screen.orientation " + Screen.orientation);

			//trackBar UI
	//		Point centerPoint = new Point(rgbMat.width()/2,rgbMat.height()/2);
			totalDistance = (float) Math.Sqrt(( (rgbaMat.width()/2 ) * ( rgbaMat.width()/2) ) + ( (rgbaMat.height()/2) * (rgbaMat.height()/2) )); 
			//Debug.Log("max distance from center (trackbar feedback): " + totalDistance + "px\n");
			Point[] trackPointArray = new Point[3] {
				new Point (frameWidth, frameHeight),
				new Point (frameWidth - triHight, 0),
				new Point (frameWidth, 0)
			};
	
			//bar points
			Point bottomLeft = new Point (rgbMat.width(), rgbMat.height());
			Point topRight = bottomLeft;
			Point bottomRight = bottomLeft;
			//barPointsArray = new Point[3];
			barPointsArray = new Point[] {bottomLeft,topRight,bottomRight};

			trackPoints = new MatOfPoint ();
			barPoints = new MatOfPoint ();
			barPoints = new MatOfPoint (barPointsArray);
			trackPoints = new MatOfPoint(trackPointArray);

			triangleTrack = new List<MatOfPoint>();
			triangleBar = new List<MatOfPoint>();
			triangleTrack.Add (trackPoints);
			triangleBar.Add (barPoints);



			//camera position
			float width = rgbaMat.width ();
			float height = rgbaMat.height ();
			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;
			Quaternion baseRotation = Camera.main.transform.rotation;

			if (iosBuild) {
				if (widthScale < heightScale) {
					Camera.main.transform.rotation = new Quaternion (0, 0, 1, 1);
					Camera.main.orthographicSize = (height * (float)Screen.height / (float)Screen.width) / 2;
					Camera.main.transform.position = new Vector3 (Camera.main.transform.position.x -2, Camera.main.transform.position.y, Camera.main.transform.position.z + 10);
				} else {

					Camera.main.orthographicSize = width / 2;
					Camera.main.transform.position = new Vector3 (Camera.main.transform.position.x -2, Camera.main.transform.position.y, Camera.main.transform.position.z + 10);
				}
			}
			if (webglBuild) {
				if (widthScale > heightScale) {
					Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
				} else {
					Camera.main.orthographicSize = height / 2;
				}

			}//camera rotation
			//

			//start processing
			StartCoroutine(processFrame());
		}

		////////////
		/// DRAWING - update loop - called once per frame
		///////////

		void Update ()
		{	
			//got a camera frame
			if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame && webCamTexture.width > 100) {
				if (frameCount >= photoStartFrame + pauseFrames) {

					if (frameProcessingInit) {
						if (!fileUpload) {
							Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);
							Utils.webCamTextureToMat (webCamTexture, rgbMat, colors);
						} else {
							Imgproc.resize(fileUploadMat,rgbaMat,new Size(rgbaMat.width (),rgbaMat.height ()));
						}

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
						//device testig stats
//						if (iosBuild) {
//							Imgproc.putText (rgbaMat, " W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " | analysing frame every " + secondsBtwProcessing + "s", new Point (5, rgbaMat.height () - 20), 2, 1.5, UIgreen, 2, Imgproc.LINE_AA, false);
//						}			

						if (heatmap) {

							//actual analysis
							//							rgbaMat.create (rgbaMat.size (), CvType.CV_32F);
							//							Imgproc.cvtColor (resizeMat, heatmapMat, Imgproc.COLOR_RGB2HLS);
							//							Imgproc.blur (heatmapMat, heatmapMat, new Size(blurSize, blurSize) );
							//							Imgproc.resize(heatmapMat,rgbaMat, rgbaMat.size());


							//
							Imgproc.resize(resizeMat,pyrMat, PyrSize);
							Imgproc.resize(rgbaMat,pyrMatRGB, PyrSize);
//							pyrMatRGB -= new Scalar (10, 0, 0);
							Imgproc.cvtColor (pyrMat, pyrMat, Imgproc.COLORMAP_HSV);
							Imgproc.cvtColor (pyrMatRGB, pyrMatRGB, Imgproc.COLOR_RGBA2RGB);
							Core.bitwise_not (pyrMat, pyrMat);

							Core.bitwise_and ( pyrMatRGB,pyrMat, pyrMatRGB);
							Imgproc.blur (pyrMatRGB, pyrMatRGB, blurKernalSize);
							Imgproc.resize(pyrMatRGB,rgbaMat, rgbMat.size());

														if (faceDetection) {
															Core.flip (heatmapMat, heatmapMat, 1);
															}

							Utils.matToTexture2D (rgbaMat, texture, colors);

						}

						if (snapToCenter) {
							SnapToCenters ();
						}
							
						checkForCentersData ();

						// draw centers
						if (weightedAverage) {
							if (!faceDetection || (faceDetection &&  frameCount >= 5 && (frameCount - lastFaceFrame <= numberOfFramesWithNoFace))){
							Imgproc.circle (rgbaMat, averageCenter.point, 8, averageColor, 13,Imgproc.LINE_AA,0);
							//Imgproc.putText (rgbaMat, "  weighted average", averageCenter.point, 2, 2, averageColor, 2, Imgproc.LINE_AA, false);
							}
						} else {
							if (!faceDetection || (faceDetection == true && frameCount >= 5 && (frameCount - lastFaceFrame <= numberOfFramesWithNoFace))) {

								for (int c = 0; c < currentCenters.Count; c++) {

									switch (c) {
									case 0:
										Imgproc.circle (rgbaMat, currentCenters [c].point, 8, red, 13, Imgproc.LINE_AA, 0);
								//	Imgproc.putText (rgbaMat, "  red", currentCenters [c].point, 2, 2, red, 2,Imgproc.LINE_AA, false);
										break;
									case 1:
										Imgproc.circle (rgbaMat, currentCenters [c].point, 8, green, 13, Imgproc.LINE_AA, 0);
								//	Imgproc.putText (rgbaMat, "  green", currentCenters [c].point, 2, 2, green, 2,Imgproc.LINE_AA, false);
										break;
									case 2:
										Imgproc.circle (rgbaMat, currentCenters [c].point, 8, blue, 13, Imgproc.LINE_AA, 0);
								//	Imgproc.putText (rgbaMat, "  blue", currentCenters [c].point, 2, 2, blue, 2,Imgproc.LINE_AA, false);
										break;
									default:
										Imgproc.circle (rgbaMat, currentCenters [c].point, 8, red, 13, Imgproc.LINE_AA, 0);
								//	Imgproc.putText (rgbaMat, "  default", currentCenters [c].point, 2, 2, red, 2,Imgproc.LINE_AA, false);
										break;
									}
								}
							}
						}



						//trackBar
						if (showTrackBar) {
							if (currentCenters [0] != null) {
								precentageToCenter = TrackbarDiff (averageCenter.point);
								Imgproc.fillPoly (rgbaMat, TriangleBar (precentageToCenter), 
									new Scalar( (int)Math.Round ( ((1 - precentageToCenter*precentageToCenter ) * 300)  ),
												(int)Math.Round ((precentageToCenter*precentageToCenter) * 235) ,
												(int)Math.Round ( ((1 - precentageToCenter*precentageToCenter ) * 300)  )/1.5,
												255),
									Imgproc.LINE_AA, 0, zeroPoint);
							}
						}

						//draw face detection
						if (faceDetection) {
							if (iosBuild) {
								Core.flip (rgbaMat, rgbaMat, 0);
							} else {
								Core.flip (rgbaMat, rgbaMat, 1);
							}
							if (rects != null && rects.Length > 0 ) {


								checkForFacesData ();

//								Debug.Log ("detect faces " + rects [0]);
								faceSubmat.create (rects [0].size (), CvType.CV_8UC4);

								//draw faces
								faceSubmat = rgbaMat.rowRange (currentFacePoints [1], currentFacePoints [3]).colRange (currentFacePoints [0], currentFacePoints [2]);
								faceSubmat -= faceSubmatColor;

								//change rect color
//								faceBackgroundColor.r = (1 - precentageToCenter) - 0.2f;
//								faceBackgroundColor.g = precentageToCenter;
//								Camera.main.backgroundColor = faceBackgroundColor;
									
								Core.bitwise_and(rgbaMat.rowRange (currentFacePoints [1], currentFacePoints [3]).colRange (currentFacePoints [0], currentFacePoints [2]),faceSubmat,rgbaMat.rowRange (currentFacePoints [1], currentFacePoints [3]).colRange (currentFacePoints [0], currentFacePoints [2]));
							
							} else {
								if (frameCount >= 15 && (frameCount - lastFaceFrame <= numberOfFramesWithNoFace)) {
									faceSubmat = rgbaMat.rowRange (currentFacePoints [1], currentFacePoints [3]).colRange (currentFacePoints [0], currentFacePoints [2]);
									faceSubmat -= faceSubmatColor;

									//change rect color
//									faceBackgroundColor.r = (1 - precentageToCenter) - 0.2f;
//									faceBackgroundColor.g = precentageToCenter;
//									Camera.main.backgroundColor = faceBackgroundColor;

									Core.bitwise_and(rgbaMat.rowRange (currentFacePoints [1], currentFacePoints [3]).colRange (currentFacePoints [0], currentFacePoints [2]),faceSubmat,rgbaMat.rowRange (currentFacePoints [1], currentFacePoints [3]).colRange (currentFacePoints [0], currentFacePoints [2]));
									//opposite
//									rgbaMat -= new Scalar (0, 0, 0, 150);
//									Core.bitwise_or(rgbaMat.rowRange (vertRange).colRange (horiRange),faceSubmat,rgbaMat.rowRange (vertRange).colRange (horiRange));

								}
							}
							
						}

						if (cross) {
							//TO-DO: new point 
							Imgproc.circle (rgbaMat,new Point( frameWidth/2,frameHeight/2) , 30, crossColor, 2,Imgproc.LINE_AA,0);
							for (int dotted = 10; dotted <= (frameWidth / 2); dotted += 10) {
								if (dotted % 40 == 10 && dotted >= 110) {
									//top left
									Imgproc.line (rgbaMat, new Point ((int)(frameWidth / 2) - (dotted)     , (int)((frameHeight / 2) - ((frameHeight/frameWidth) * (dotted)))  ), 
													 	   new Point ((int)(frameWidth / 2) - (10 + dotted), (int)((frameHeight / 2) - ((frameHeight/frameWidth) * (10 + dotted)))), crossColor, 1, Imgproc.LINE_AA, 0);
									//buttom left
									Imgproc.line (rgbaMat, new Point ((int)(frameWidth / 2) - (dotted),      (int)((frameHeight / 2) + ((frameHeight / frameWidth) * (dotted)))), 
														   new Point ((int)(frameWidth / 2) - (10 + dotted), (int)((frameHeight / 2) + ((frameHeight / frameWidth) * (10 + dotted)))), crossColor, 1, Imgproc.LINE_AA, 0);
									//buttom right
									Imgproc.line (rgbaMat, new Point ((int)(frameWidth / 2) + (dotted),      (int)((frameHeight / 2) + ((frameHeight / frameWidth) * (dotted)))), 
														   new Point ((int)(frameWidth / 2) + (10 + dotted), (int)((frameHeight / 2) + ((frameHeight / frameWidth) * (10 + dotted)))), crossColor, 1, Imgproc.LINE_AA, 0);
									//top right
									Imgproc.line (rgbaMat, new Point ((int)(frameWidth / 2) + (dotted),      (int)((frameHeight / 2) - ((frameHeight / frameWidth) * (dotted)))), 
														   new Point ((int)(frameWidth / 2) + (10 + dotted), (int)((frameHeight / 2) - ((frameHeight / frameWidth) * (10 + dotted)))), crossColor, 1, Imgproc.LINE_AA, 0);
								}
							}
						}
						if (guide) {
							for (int dotted = 10; dotted <= (frameHeight * 2) ; dotted += 10) {
								if (dotted % 40 == 10) {
									Imgproc.line (rgbaMat, new Point ((int)(frameWidth / 3), dotted), new Point ((int)(frameWidth / 3), (10 + dotted)), crossColor, 0, Imgproc.LINE_AA, 0);
									Imgproc.line (rgbaMat, new Point ((int)(frameWidth * 2 / 3), dotted), new Point ((int)(frameWidth * 2 / 3), (10 + dotted)), crossColor, 0, Imgproc.LINE_AA, 0);
									Imgproc.line (rgbaMat, new Point (dotted, (int)frameHeight/3), new Point ((10 + dotted), (int)frameHeight/3), crossColor, 0, Imgproc.LINE_AA, 0);
									Imgproc.line (rgbaMat, new Point (dotted, (int)frameHeight*2/3), new Point ((10 + dotted), (int)(int)frameHeight*2/3), crossColor, 0, Imgproc.LINE_AA, 0);
								}
							
							}
						}


					}
					Utils.matToTexture2D (rgbaMat, texture, colors);
					flippedForPhoto = false;
				} else {
					if (faceDetection && !flippedForPhoto) {
						Core.flip (rgbMat, rgbMat, 1);
						flippedForPhoto = true;
					}

					// photo border
					photoWhiteMat.colRange (0, 20).copyTo (rgbMat.colRange (0, 20));
					photoWhiteMat.colRange (photoWhiteMat.cols () - 20, photoWhiteMat.cols ()).copyTo (rgbMat.colRange (photoWhiteMat.cols () - 20, photoWhiteMat.cols ()));
					photoWhiteMat.rowRange (0, 20).copyTo (rgbMat.rowRange (0, 20));
					photoWhiteMat.rowRange (photoWhiteMat.rows () - 20, photoWhiteMat.rows ()).copyTo (rgbMat.rowRange (photoWhiteMat.rows () - 20, photoWhiteMat.rows ()));

					Utils.matToTexture2D (rgbMat, texture, colors);
				}
				frameCount++;
			}

		}
		public void SnapToCenters(){
			if (frameCount >= 0  && displayCenters.Count > 0){
				for (int q = 0; q < displayCenters.Count; q++) {
//					Debug.Log ("\n displayCenters [q].point.x: " + displayCenters [q].point.x);

					if (!faceDetection) {
						//channel center inside rect
						if (displayCenters [q].point.x >= (frameWidth / 2) - snapToCenterSize
							&& displayCenters [q].point.x <= (frameWidth / 2) + snapToCenterSize
							&& displayCenters [q].point.y >= (frameHeight / 2) - snapToCenterSize
							&& displayCenters [q].point.y <= (frameHeight / 2) + snapToCenterSize) {

							displayCenters [q].point = middleOfTheFramePoint;
						}
					
						
					} else {
						//channel center inside rect
						if(rects != null && rects.Length > 0){

							faceMiddleX = (int)Math.Round( frameWidth - (( rects [0].x + rects [0].width/2)/resizeFactor));
							faceMiddleY = (int)Math.Round( ( rects [0].y + rects [0].height/2)/resizeFactor);

							if (   (displayCenters [q].point.x)  >= (faceMiddleX) - snapToCenterSizeFace
								&& (displayCenters [q].point.x)  <= (faceMiddleX) + snapToCenterSizeFace
								&& ( displayCenters [q].point.y) >= (faceMiddleY) - snapToCenterSizeFace
								&& ( displayCenters [q].point.y) <= (faceMiddleY) + snapToCenterSizeFace) {

								displayCenters [q].point = new Point (faceMiddleX, faceMiddleY);
						}

						}
					}
				}
			}
		}
		public void checkForCentersData(){
			//check for first tiem frame processing - Initiate centers - place in the center
			if(displayCenters[0].point.x.ToString() == "NaN"){
				displayCenters.Clear ();
				for (int o = 0; o <= 2; o++) {
					displayCenters.Add (new Centers (o, zeroPoint));
				}
			}

			if (displayCenters!= null && currentCenters.Count == 0 || !centersFlag) {
				currentCenters.Clear ();

				//initiate currentCenters
				for (int d = 0; d < displayCenters.Count; d++) {
						currentCenters.Add (new Centers (d, new Point (rgbaMat.width () * 0.5, rgbaMat.height () * 0.5)));
					}
			
			}

			// currentCenters step
			if (displayCenters.Count > 1) {

				for (int h = 0; h < displayCenters.Count; h++) {

					currentCenters [h].point.x = (int)Math.Round( speed * currentCenters [h].point.x + displayCenters [h].point.x * (1 - speed));
					currentCenters [h].point.y = (int)Math.Round( speed * currentCenters [h].point.y + displayCenters [h].point.y * (1 - speed));

				}

				//centers center - weighted average
				averageCenter.point = WeightedAverageThree (currentCenters [0].point, currentCenters [1].point, currentCenters [2].point);
			}

			centersFlag = true;

		}
		public void checkForFacesData(){
			//check for first tiem frame processing - Initiate centers - place in the center
			if(displayFacePoints == null){
				displayFacePoints.Clear ();
				//for (int o = 0; o <= 3; o++) {
				displayFacePoints.Add ((int)(rects[0].x / resizeFactor));
				displayFacePoints.Add ((int)(rects[0].y / resizeFactor));
				displayFacePoints.Add ((int)(rects[0].width / resizeFactor));
				displayFacePoints.Add ((int)(rects[0].height / resizeFactor));
				//}
			}

			if (displayFacePoints!= null && currentFacePoints.Count == 0 || !facesFlag) {
				currentFacePoints.Clear ();
				//initiate currentCenters
				for (int d = 0; d < displayFacePoints.Count; d++) {
					currentFacePoints.Add ((int)(rects[0].x / resizeFactor));
					currentFacePoints.Add ((int)(rects[0].y / resizeFactor));
					currentFacePoints.Add ((int)((rects[0].x + rects[0].width) / resizeFactor));
					currentFacePoints.Add ((int)((rects[0].y + rects[0].height) / resizeFactor));
				}
			}

			// currentCenters step
			if (displayFacePoints.Count > 1) {
				for (int h = 0; h < displayFacePoints.Count; h++) {
					currentFacePoints [h] = (int) Math.Round( speed * currentFacePoints [h] + displayFacePoints [h] * (1 - speed));
//					Debug.Log ("currentFacePoints[h]: " + currentFacePoints [h]);
				}


			}
			facesFlag = true;

		}

		////////////
		/// A-Sync Frame Processing
		///////////

		private IEnumerator processFrame(){
			while (true && webCamTexture.width > 100) {

				//resize down
				if (resizeMat != null) {


					////////////
					/// file Uplaod
					///////////

					if (fileUpload != null) {
						#if UNITY_EDITOR

						fileUploadFlag = true;

						try{
							//initiate texture (output) And it's size acordingto file
							Debug.Log("path: " + AssetDatabase.GetAssetPath(fileUpload));
							fileUploadData =  File.ReadAllBytes(AssetDatabase.GetAssetPath(fileUpload));
						}catch (IOException e){
							Debug.Log ("CAUGHT - incompatible file exception: " + e);
							fileUpload = null;
							//reload
							SceneManager.LoadScene( SceneManager.GetActiveScene().name );
						}
						if (fileColors == null || fileColors.Length != fileUpload.width * fileUpload.height)
							fileColors = new Color32[fileUpload.width * fileUpload.height];
						if (fileTexture == null || fileTexture.width != fileUpload.width || fileTexture.height != fileUpload.height)
							fileTexture = new Texture2D (fileUpload.width, fileUpload.height, TextureFormat.RGBA32, false);

//						fileUploadMat = new Mat (fileTexture.height, fileTexture.width, CvType.CV_8UC3);
//
//						Utils.texture2DToMat (fileTexture, fileUploadMat);
//						Imgproc.resize (fileUploadMat, resizeMat, resizeSize, 0.5, 0.5, Core.BORDER_DEFAULT);
						fileTexture.LoadImage (fileUploadData);
						fileUploadMat = new Mat (fileTexture.height, fileTexture.width, CvType.CV_8UC3);

						OpenCVForUnity.Utils.texture2DToMat (fileTexture, fileUploadMat);
						//	Utils.matToTexture2D (fileUploadMat, fileUpload, fileColors);
						Imgproc.resize (fileUploadMat, resizeMat, resizeSize, 0.5, 0.5, Core.BORDER_DEFAULT);

					#endif
						
					} else {
							if (fileUploadFlag) {
							//return texture to camera after file upload
							gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
							fileUploadFlag = false;
							}
						Imgproc.resize (rgbMat, resizeMat, resizeSize, 0.5, 0.5, Core.BORDER_DEFAULT);
//						Imgproc.resize (rgbMat, resizeMat, resizeSize);

					}

					////////////
					/// Process frame
					///////////

					if (!faceDetection) {

						//flip values
						if (frameProcessingInit == true ) {
							Core.bitwise_not (resizeMat, resizeMat);

						}

						//flip colors position
						Imgproc.cvtColor (resizeMat, resizeMat, Imgproc.COLOR_RGB2BGR);

						//edge detection and wights
						if (edgeBias || heatmap) {
							grayMat = resizeMat.clone();
							Imgproc.cvtColor (grayMat, grayMat, Imgproc.COLOR_BGR2GRAY);

							Imgproc.Canny (grayMat, grayMat, cannyThreshold, cannyThreshold);
//							Imgproc.blur (grayMat, grayMat, new Size (blurSize, blurSize));
							if (thresh) {
								Imgproc.threshold (grayMat, grayMat, edgeThreshold, 255, Imgproc.THRESH_BINARY);
							}

							//weights
							Imgproc.cvtColor (grayMat, grayMat, Imgproc.COLOR_GRAY2RGB);
							//Debug.Log("sample pixel before calc: " + resizeMat.get (100, 100).GetValue(0));
							Core.addWeighted (resizeMat, (1 - edgeWeight), grayMat, edgeWeight, edgeGamma, resizeMat);
							//Debug.Log("sample pixel after calc: " + resizeMat.get (100, 100).GetValue(0));
						}

						if (loactionBias || heatmap) {
							//TO-DO: weighted average CHANGE + track bar################################
							Core.addWeighted (resizeMat, (1 - locationWeight), locationMat, locationWeight, 0.0, resizeMat);
						}

						//split channels and 
						Core.split (resizeMat, channels);

						//clear last cenbters
						displayCenters.Clear ();
						//center for each channel
						for (int i = 0; i < channels.Count; i++) {
							displayCenters.Add (getCenterPointFromMat (channels [i], i));
						}
					} 

					//////////
					// process face detection
					//////////


					else {
						Debug.Log ("face detection on");

						//Mat for detection
						if (iosBuild) {
							Core.flip (resizeMat, resizeMat, 1);
						} else {
							Core.flip (resizeMat, resizeMat, 3);
						
						}
						Imgproc.cvtColor (resizeMat, grayMat, Imgproc.COLOR_RGB2GRAY);

						//Hist correction - optional
						Imgproc.equalizeHist (grayMat, grayMat);

						if (iosBuild) {
							
							Core.rotate (grayMat, rotateMat, 2);
							// actual cascade face detection // LBS fast dataset 10% less accurate - change to haar cascade dataset at cascade initiation
							if (cascade != null) {
								cascade.detectMultiScale2 (rotateMat, faces, maxDetections, 1.1, 2, 2, new Size (50, 50), maxFaceSize);
								//cascade.detectMultiScale (grayMat, faces, 1.1, 2, 2, new Size (20, 20), new Size ());
							}
						}else {
							// actual cascade face detection // LBS fast dataset 10% less accurate - change to haar cascade dataset at cascade initiation
							if (cascade != null) {
								cascade.detectMultiScale2 (grayMat, faces, maxDetections, 1.1, 2, 2, new Size (50, 50), maxFaceSize);
								//cascade.detectMultiScale (grayMat, faces, 1.1, 2, 2, new Size (20, 20), new Size ());
							}
						}
						rects = faces.toArray ();


						if (rects.Length > 0  ) {

							lastFaceFrame = frameCount;
							if (iosBuild) {
								//90 deg clockwise transformation
//								rectsY = rects [0].y;
//								rectsWidth = rects [0].width;
//								rects [0].y = rects [0].width - rects [0].x;
//								rects [0].x = rectsY;
//								rects [0].width = rects [0].height;
//								rects [0].height = rectsWidth;

								//270 deg clockwise transformation
//								Debug.Log("rect[0]: " +rects[0].ToString());
								rectsY = rects [0].x;
								rectsWidth = rects [0].width;
								rects [0].x =  rects [0].y ;
								rects [0].y = (int)resizeSize.height - rectsY -rects[0].width ;
								rects [0].width = rects [0].height;
								rects [0].height = rectsWidth;

//								Debug.Log ("AFTER rect[0]: " + rects [0].ToString ());
									

							}
							//change calc mats sizes according to face
							faceRefMat.create (rects [0].size(), CvType.CV_8UC3);
							grayFaceMat.create (rects [0].size(), CvType.CV_8UC1);

							faceRefMat = resizeMat.submat( rects [0]);
							//alternativly
//							resizeMat.submat( rects [0]).copyTo (faceRefMat);

							//flip values
//							Core.bitwise_not (faceRefMat, faceRefMat);

							//edge detection and wights
							if (edgeBias  || heatmap) {
								Imgproc.cvtColor (faceRefMat, grayFaceMat, Imgproc.COLOR_RGB2GRAY);

								Imgproc.Canny (grayFaceMat, grayFaceMat, cannyThreshold, cannyThreshold);
								Imgproc.blur (grayFaceMat, grayFaceMat, new Size (blurSize, blurSize));
								if (thresh) {
									Imgproc.threshold (grayFaceMat, grayFaceMat, edgeThreshold, 255, Imgproc.THRESH_BINARY);
								}

								// add aweights
								Imgproc.cvtColor (grayFaceMat, grayFaceMat, Imgproc.COLOR_GRAY2RGB);
								Core.addWeighted (faceRefMat, (1 - edgeWeight), grayFaceMat, edgeWeight, edgeGamma, faceRefMat);
							}

							//Location isn't relevant for face detection mode
//							if (loactionBias) {
//								Core.addWeighted (faceRefMat, (1 - locationWeight), locationMat, locationWeight, 0.0, faceRefMat);
//							}

							//split channels
							Core.split (faceRefMat, channels);

							//clear last cenbters
							displayCenters.Clear ();
							//center for each channel
							for (int i = 0; i < channels.Count; i++) {
								displayCenters.Add (getCenterPointFromMat (channels [i], i));
							}

							//clear last faces
							displayFacePoints.Clear ();
							//add face point
							displayFacePoints.Add ( (int)(rects[0].x/resizeFactor));
							displayFacePoints.Add ( (int)(rects [0].y / resizeFactor));
							displayFacePoints.Add ( (int)(rects[0].x/resizeFactor + rects[0].width/resizeFactor));
							displayFacePoints.Add ( (int)(rects [0].y / resizeFactor + rects [0].height / resizeFactor));
						}
					}
					moments.Clear ();
					centersObj.Clear ();
					frameProcessingInit = true;
				}
				yield return new WaitForSeconds(secondsBtwProcessing);
			}
		}

		public Centers getCenterPointFromMat(Mat _mat, int channel){

			if (frameCount <= 1) {
				point = middleOfTheFramePoint;
			} else {
				
				// 3rd order moment center of mass
				moments.Add (Imgproc.moments (_mat, false));
				point = new Point ((int)Math.Round ((moments [channel].m10 / moments [channel].m00)), (int)Math.Round ((moments [channel].m01 / moments [channel].m00)));

				if (point.x.ToString () == "NaN" || point.x < 0 || point.y < 0) {
					point = middleOfTheFramePoint;
					Debug.Log ("INSIDE CATCH point: " + point);
				}
				//resize point up
				if (!faceDetection) {
					point.x = (int)Math.Round (map ((float)point.x, 0, (float)resizeSize.width, (float)webCamTexture.width - (float)webCamTexture.width * exaggerateData, (float)webCamTexture.width * exaggerateData));
					point.y = (int)Math.Round (map ((float)point.y, 0, (float)resizeSize.height, (float)webCamTexture.height - (float)webCamTexture.height * exaggerateData, (float)webCamTexture.height * exaggerateData));
				} else {
					if (currentFacePoints.Count > 0) {

						//map results to frame with exaggeration
//					point.x = (int)Math.Round( map ((float)point.x, 0, (float)rects[0].width, (float)rects[0].width - (float)rects[0].width * (exaggerateData + exaggerateDataFace), (float)rects[0].width * (exaggerateData + exaggerateDataFace) ));
//					point.y =(int)Math.Round( map ((float)point.y, 0, (float)rects[0].height, (float)rects[0].height - (float)rects[0].height * (exaggerateData + exaggerateDataFace), (float)rects[0].height * (exaggerateData + exaggerateDataFace)));

						point.x = (int)Math.Round (map ((float)point.x, 0, (float)rects [0].width, 
							(float)rects [0].width * (exaggerateData + exaggerateDataFace), (float)rects [0].width - (float)rects [0].width * (exaggerateData + exaggerateDataFace)));
						point.y = (int)Math.Round (map ((float)point.y, 0, (float)rects [0].height, 
							(float)rects [0].height * (exaggerateData + exaggerateDataFace), (float)rects [0].height - (float)rects [0].height * (exaggerateData + exaggerateDataFace)));

//					Debug.Log ("AFTER MAP point: " + point);

						//flip results horizontaly
						point.x = (int)Math.Round ((webCamTexture.width - (point.x + rects [0].x) / resizeFactor));
						point.y = (int)Math.Round ((point.y + rects [0].y) / resizeFactor);



					}
				}
			}
			centersObj.Add(new Centers(channel, point) );

			return centersObj [channel];
		}

		void OnEnable(){
			// ImageCroppedEvent.OnCropped += AnalyseImage();

			//resume when active
			Time.timeScale = 1;
			Debug.Log ("<unity> resumed view");

		}
		void OnDisable(){
			// ImageCroppedEvent.OnCropped += AnalyseImage();

			//pause when inactive
			Time.timeScale = 0;
			Debug.Log ("<unity> paused view");

		}
		public void takePhoto(){
			Debug.Log ("TAKE PHOTO");
			photoStartFrame = frameCount;
			//PhotoFromUnity ();
			//audio
			AudioSource audio = GetComponent<AudioSource>();		
			audio.Play ();
			//write to singleton
			ImageManager.instance.photo = texture;
			//TO-DO: emmit event for Markus
			//UnityToXcodePhotoEvent;
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
					//TO-DO: switch between face detectiuon ui and regular
//					GUImat = faceRefMat.clone ();
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
		public void showFace(){
			faceDetection = !faceDetection;
		}
		public void showHeatmap(){
			heatmap = !heatmap;
		}
		public void showEdge(){
			edgeBias = !edgeBias;
		}
		public void showCenter(){
			loactionBias = !loactionBias;
		}
		public void showcolor(){
			weightedAverage = !weightedAverage;
		}
		public void showGuide(){
			guide = !guide;
		}
		public void showCross(){
			cross = !cross;
		}
		public void ExaggerationSlider(){
			//to-do : init up 
			float sliderGet = GameObject.Find ("exaggeration").GetComponent <Slider> ().value;
			exaggerateData = sliderGet;

			Text _text = GameObject.Find ("exaggerationText").GetComponent<UnityEngine.UI.Text>();
			_text.text = exaggerateData.ToString ();
		}

		//calculate the trackbar bar
		public List<MatOfPoint> TriangleBar(float _percentToCenter){
			barPointsArray = new Point[]{ new Point (rgbMat.width(), rgbMat.height()),
				new Point (rgbaMat.width() - (triHight * _percentToCenter),rgbaMat.height()-( (rgbaMat.height() * _percentToCenter ))),
				new Point(rgbaMat.width() , rgbaMat.height()-( (rgbaMat.height() * _percentToCenter )))};
			
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

			//TO-DO: optimize initiation with flag condition 
			if (!faceDetection) {
				totalDistance = (float)Math.Sqrt (((frameWidth / 2) * (frameWidth / 2)) + ((frameHeight / 2) * (frameHeight / 2)));
		
				trackbarDiffFloat = (float) (Math.Sqrt ((
					(_current.x - (frameWidth / 2)) * (_current.x - (frameWidth / 2))) + ((_current.y - (frameHeight / 2)) * (_current.y - (frameHeight / 2)))));

				if (trackbarDiffFloat > totalDistance - 10)
					trackbarDiffFloat = totalDistance;
				if (trackbarDiffFloat < 10)
					trackbarDiffFloat = 0;
			} else {
				//face detection 

				if (rects != null && rects.Length > 0 ) {
					

					totalDistance = (float)Math.Sqrt (((rects[0].width / 2)*(rects[0].width / 2) +  (rects[0].height / 2)*(rects[0].height / 2)));
					_current.x = (float)((frameWidth -  _current.x) * resizeFactor - rects [0].x);
					_current.y = (float)(  _current.y * resizeFactor - rects [0].y);
//					Debug.Log ("_current: " + _current);
//					Imgproc.circle (rgbaMat, _current, 8, green, 13, Imgproc.LINE_AA, 0);


					trackbarDiffFloat = (float) (Math.Sqrt ((
						( _current.x - (rects[0].width / 2)) * (_current.x - (rects[0].width / 2))) + ((_current.y - (rects[0].height / 2)) * (_current.y - (rects[0].height / 2)))));

//					Debug.Log ("total disstance: " + totalDistance);
//					Debug.Log ("trackbarDiffFloat: " + trackbarDiffFloat);


				} else {
					return 0.01f;
				}
			}

//			Debug.Log ("return: " + (1 - trackbarDiffFloat / totalDistance));
			return (1 - trackbarDiffFloat/totalDistance);
		}
				public Point WeightedAverageThree(Point _redPoint, Point _greenPoint, Point _bluePoint){
			return new Point( (int) Math.Round(((_redPoint.x * redCoeficiente) + (_greenPoint.x * greenCoeficiente) + (_bluePoint.x * blueCoeficiente)) / (redCoeficiente + greenCoeficiente + blueCoeficiente)),
							  (int) Math.Round(  ((_redPoint.y * redCoeficiente) + (_greenPoint.y * greenCoeficiente) + (_bluePoint.y * blueCoeficiente)) / (redCoeficiente + greenCoeficiente + blueCoeficiente)));
		}

	}

}