using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace toneFrameProcessing
{
    /// <summary>
    /// WebCamTexture to mat example.
    /// An example of converting the WebCamTexture image to OpenCV's Mat format.
    /// </summary>
	public class toneFrameProcessing : MonoBehaviour
    {	
		[SerializeField]
		bool showProcessing = true;

		[SerializeField]
		bool threshold = false;
		[SerializeField]
		[Range(1,255)]
		double thresholdValue = 127.5f;

		[SerializeField]
		bool blur = false;
		[SerializeField]
		[Range(1,20)]
		int blurSize = 3;

		[SerializeField]
		bool blobs = false;

		[SerializeField]
		bool centerPoint = false;

		[SerializeField]
		bool edgeCenterPoint = false;
			
		int edgeThresh = 1;
		int lowThreshold;
		int  max_lowThreshold = 100;
		int ratio = 3;

		Mat hierarchy;
		List< MatOfPoint > contours = new List<MatOfPoint>(); 	

		List<Point> WeightedCentroid = new List<Point>();

		int framesDropCount =0;

		// GRAY IMG MAT
		Mat grayMat;

		//portatint
		[SerializeField]
		bool portrait = false;
        /// <summary>
        /// Set this to specify the name of the device to use.
        /// </summary>
        public string requestedDeviceName = null;
		//resize ration
		[SerializeField]
		[Range(1f,0.05f)]
		float resizeRatio = 0.5f;
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
		//inversionMat
		Mat inversionMat;
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
			if (grayMat != null) {
				grayMat.Dispose ();
				grayMat = null;
			}
			if (rgbMat != null) {
				rgbMat.Dispose ();
				rgbMat = null;
			}
			if (inversionMat != null) {
				inversionMat.Dispose ();
				inversionMat = null;
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
			grayMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
			hierarchy = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
			//grayMat = rgbaMat.clone ();
			inversionMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1, new Scalar(255));

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
				Utils.webCamTextureToMat (webCamTexture, grayMat);

				//Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
				//Debug.Log ("webcam ratio " + "( " + webCamTexture.width + ", " + webCamTexture.height + ")");

				if (framesDropCount >= framesToDrop) {
					processFrame ();
				}

				Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height (), new Point ((int)Math.Round(rgbaMat.width() * 0.35), rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (180,180,180), 2, Imgproc.LINE_AA, false);
				Utils.matToTexture2D (rgbaMat, texture);
				//Utils.matToTexture2D (grayMat, textureGray);

            }
        	


		}
		public void processFrame(){
			if (showProcessing) {
				//flip
				Core.bitwise_not (grayMat, grayMat);
				//
				if (threshold){
					Imgproc.threshold ( grayMat, grayMat, thresholdValue, 255, Imgproc.THRESH_BINARY );
				}
				if (blobs) {
					blobDetector.detect(grayMat, keypoints);
					Features2d.drawKeypoints(grayMat, keypoints, grayMat);
				}
				if (blur) {
					Imgproc.blur( grayMat, grayMat, new Size(blurSize,blurSize) );
				}
				if (edgeCenterPoint) {
					Imgproc.Canny (grayMat, grayMat, thresholdValue * 0.5 , thresholdValue);
					Imgproc.findContours (grayMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE );
					//	
//					foreach(MatOfPoint i in contours){
//						Debug.Log ("contour " + i + ": " + i.ToString());
//					}
					Debug.Log ("contours count: " + contours.Count);

					contours.Clear ();

				}
				if (centerPoint) {
					Moments moments = new Moments();
					moments = Imgproc.moments (grayMat, true);
					WeightedCentroid.Add(new Point((int)Math.Round(moments.m10 / moments.m00), (int)Math.Round(moments.m01 / moments.m00)));
					Debug.Log("centeroids: " + WeightedCentroid.Count);

					Imgproc.ellipse (grayMat, WeightedCentroid [0], new Size (20, 20), 1, 0.1, 360, new Scalar (180),10);
					Imgproc.putText(grayMat, "  center point", WeightedCentroid [0], 0, 1.5, new Scalar(180),5);

					WeightedCentroid.Clear ();
					//moments.Clear ();
				}
				//assign to display
				rgbaMat = grayMat;



				framesDropCount = 0;
				//Debug.Log ("frame processed, time is: " + Time.fixedTime);
			} else {
				rgbaMat = cloneMat;
			}
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