using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace FrameProcessingChannels
{
    /// <summary>
    /// AeStatix real time image analysis.
	/// 
	/// 
	/// 
	/// 
	/// 
    /// </summary>
	public class FrameProcessingChannels : MonoBehaviour
    {	
		bool showProcessing = true;

		bool inversion = true;

		bool blur = true;
		[Header("detection and calculation")]
		[SerializeField]
		[Range(1,40)]
		int blurSize = 20;

		bool toneThreshold = true;
		[SerializeField]
		[Range(0,255)]
		double idealThresholdValue = 127.5f;
		int thresholdValueCap =2;

		bool centerPoint = true;

		List<Moments> moments = new List<Moments>();
		List<Moments> momentsEdge = new List<Moments>();

		public bool showRgbCenters = true;
		[SerializeField]
		bool edgeCenter = false;
		bool edgeCenterPoint = false;

		[SerializeField]
		bool mergeRgbCenters = false;

		public bool mergeEdge = false;
		[Range(0.01f,0.99f)]
		public float edgeFactor = 0.5f;

		int edgeThresh = 1;
		int lowThreshold;
		int  max_lowThreshold = 100;
		int ratio = 3;

		[SerializeField]
		bool calculateLocation = false;
		[SerializeField]
		[Range(0.51f,1f)]
		float LocationSizeFactor = 0.8f;
		[SerializeField]
		[Range(0.01f,0.99f)]
		float locationWeightFactor = 1f;
		[SerializeField]
		bool showLocationRect = true;
		[SerializeField]
		bool popToCenter = false;
		[SerializeField]
		[Range(0.51f,1f)]
		float popToCenterRectFactor = 0.55f;
		[SerializeField]
		bool showPopToCenterRect = true;

		Mat hierarchy;
		List< MatOfPoint > contours = new List<MatOfPoint>(); 	
		//take photo mat
		Mat textureInstance;
		bool stopForPhoto = false;

		List<Point> WeightedCentroid = new List<Point>();
		List<Point> WeightedCentroidEdge = new List<Point>();

		int framesDropCount =0;

		// GRAY IMG MAT
		List<Mat> channelsMats = new List<Mat>();

		/// <summary>
		/// /////////////////////////////////performance fields
		/// </summary>
		string requestedDeviceName = null;
		[Header("performance")]
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

		//calc fields
		String colorName = "color";
		Scalar colorScalar = new Scalar (100, 100, 100,0.5d);

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
        //Mat rgbMat;

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
		Texture2D texturePhoto;
		int snapshotCount;


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


		//gray mat
		Mat toneMat;

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
			for (int i = 0; i <= channelsMats.Count - 1; i++) {
				if (channelsMats[i] != null) {
					channelsMats[i].Dispose ();
					channelsMats[i] = null;
				}
			}
			if (rgbMat != null) {
				rgbMat.Dispose ();
				rgbMat = null;
			}
			if (cloneMat != null) {
				cloneMat.Dispose ();
				cloneMat = null;
			}
			if (toneMat != null) {
				toneMat.Dispose ();
				toneMat = null;
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
			texturePhoto = new Texture2D (Screen.width, Screen.height, TextureFormat.RGBA32, false);
			if (textureGray == null || textureGray.width != webCamTexture.width || textureGray.height != webCamTexture.height)
				textureGray = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.Alpha8, false);
			

			//reference mat
			cloneMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
			//analyzed Mat
			rgbMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
			//single channel mat
			toneMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
			//teke photo mat
			textureInstance = new Mat (Screen.height, Screen.width, CvType.CV_8UC4);



			Debug.Log ("number of channels: " + channelsMats.Count);

			//hierarchy = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
			//toneMat = rgbMat.clone ();
			//inversionMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1, new Scalar(255));

			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

			gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);

			Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = rgbMat.width ();
            float height = rgbMat.height ();

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
				if (!stopForPhoto) {
					Utils.webCamTextureToMat (webCamTexture, rgbMat, colors);
					Utils.webCamTextureToMat (webCamTexture, toneMat);
				}
				//Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
				//Debug.Log ("webcam ratio " + "( " + webCamTexture.width + ", " + webCamTexture.height + ")");

				if (framesDropCount >= framesToDrop) {
					processFrame ();
				}

				Imgproc.putText (rgbMat, "W:" + rgbMat.width () + " H:" + rgbMat.height (), new Point ((int)Math.Round(rgbMat.width() * 0.35), rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255,0,0,0.5), 2, Imgproc.LINE_AA, false);
				if (!stopForPhoto) {
					Utils.matToTexture2D (rgbMat, texture);
				} else {
					Utils.matToTexture2D (toneMat, texture);
				}				//display
				//Utils.matToTexture2D (textureInstance, texturePhoto);
				//Utils.matToTexture2D (toneMat, textureGray);

            }
        	


		}
		public void processFrame(){
			//channel split
			Core.split(rgbMat,channelsMats);

			for(int i = 0; i<= channelsMats.Count-2;i++){
				if (inversion) {
					//flip
					Core.bitwise_not (channelsMats[i], channelsMats[i]);
				}
				if (resize) {
					Imgproc.resize(channelsMats[i],channelsMats[i], new Size((int)Math.Round(resizeRatio*channelsMats[i].width()),(int)Math.Round(resizeRatio*channelsMats[i].height())));
				}
				//
				if (toneThreshold){
					Imgproc.threshold ( channelsMats[i], channelsMats[i], idealThresholdValue, 255, Imgproc.THRESH_BINARY );
				}
			
				if (blur) {
					Imgproc.blur( channelsMats[i], channelsMats[i], new Size(blurSize,blurSize) );
				}
				if (centerPoint) {
					moments.Add(Imgproc.moments (channelsMats[i], true));
					WeightedCentroid.Add(new Point((int)Math.Round(moments[i].m10 / moments[i].m00), (int)Math.Round(moments[i].m01 / moments[i].m00)));
					//Debug.Log("center: " + WeightedCentroid[0].x +", " + WeightedCentroid[0].y);
				}

				//draw center
				if(centerPoint ){
					Imgproc.ellipse (channelsMats[i], WeightedCentroid [i], new Size (20, 20), 1, 0.1, 360, new Scalar (180),10);
					Imgproc.putText(channelsMats[i], " Tone center point", WeightedCentroid [i], 0, 1.5, new Scalar(180),5);
				}
				if (resize) {
					Imgproc.resize(channelsMats[i],channelsMats[i], new Size((int)Math.Round((1/resizeRatio)*channelsMats[i].width()),(int)Math.Round((1/resizeRatio)*channelsMats[i].height())));
				}
				//assign to display
				if (showProcessing) {
					//Core.add (rgbMat, channelsMats[i], rgbMat);

				} else {
					rgbMat = cloneMat;
				}					
			}

			if (edgeCenter) {
				Imgproc.Canny( toneMat, toneMat, idealThresholdValue * 0.5 , idealThresholdValue);
				//Imgproc.findContours (channel, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE );
				//	
				//					foreach(MatOfPoint i in contours){
				//						Debug.Log ("contour " + i + ": " + i.ToString());
				//					}
				//Debug.Log ("contours count: " + contours.Count);
				moments.Add(Imgproc.moments (toneMat, true));
				//if (WeightedCentroidEdge.Count == 0) {
					moments.Add(Imgproc.moments (toneMat, true));
				//	WeightedCentroidEdge.Add(new Point(0,0));
				//}
				WeightedCentroidEdge.Add(new Point((int)Math.Round(moments[moments.Count-1].m10 / moments[moments.Count-1].m00), (int)Math.Round(moments[moments.Count-1].m01 / moments[moments.Count-1].m00)));

				Imgproc.ellipse (rgbMat, WeightedCentroidEdge [0], new Size (4, 4), 1, 1.5, 360, new Scalar (0,0,0,100), 10);
				Imgproc.putText (rgbMat, " edge center point", WeightedCentroidEdge [0], 0,1.3, new Scalar (0,0,0,100), 2);

			}
			//display rgb centers
			if(showRgbCenters){
				for (int i = 0; i <= channelsMats.Count - 2; i++) {
					switch (i) {
					case 0:
						colorName = " red";
						colorScalar = new Scalar (255, 0, 0, 100);
						break;
					case 1:
						colorName = " green";
						colorScalar = new Scalar (0, 255, 0, 100);
						break;
					case 2:
						colorName = " blue";
						colorScalar = new Scalar (0, 0, 255, 100);
						break;
					default:
						colorName = "color";
						break;
					}
				
				Imgproc.ellipse (rgbMat, WeightedCentroid [i], new Size (4, 4), 1, 1.5, 360, colorScalar,10);
				Imgproc.putText(rgbMat, colorName + " center " + WeightedCentroid [i], WeightedCentroid [i], 0, 1.3, colorScalar,2);	
			//	Debug.Log ("center " + i + "is: " + WeightedCentroid[i]);
				}
			}
			if (mergeRgbCenters) {
				bool edgeInRect = false;
				bool rgbInRect = false;

				Point rgbAverage = new Point {
					x = (int)Math.Round (WeightedCentroid.Average (p => p.x)),
					y = (int)Math.Round (WeightedCentroid.Average (p => p.y))
				};
				//Debug.Log ("average POINT: " + rgbAverage);
				Imgproc.ellipse (rgbMat, rgbAverage, new Size (4, 4), 1, 1.5, 360, new Scalar(120,120,120,255),10);
				Imgproc.putText(rgbMat, " merged center " + rgbAverage,rgbAverage, 0, 1.3, new Scalar(120,120,120,255),2);	

				if (calculateLocation) {
					if (showLocationRect) {
						Imgproc.rectangle (rgbMat, new Point (webCamTexture.width * LocationSizeFactor, webCamTexture.height * LocationSizeFactor),
							new Point (webCamTexture.width * (1 - LocationSizeFactor), webCamTexture.height * (1 - LocationSizeFactor)), new Scalar (255, 0, 0, 255), 2, 8, 0);
					}
					//case edge center in center rect
					if (WeightedCentroidEdge [0].x <= webCamTexture.width * LocationSizeFactor && WeightedCentroidEdge [0].x >= webCamTexture.width * (1 - LocationSizeFactor) &&
						WeightedCentroidEdge [0].y <= webCamTexture.height * LocationSizeFactor && WeightedCentroidEdge [0].y >= webCamTexture.height * (1 - LocationSizeFactor)) {
						edgeInRect = true;
					}
					//case RGB center in center rect
					if (rgbAverage.x <= webCamTexture.width * LocationSizeFactor && rgbAverage.x >= webCamTexture.width * (1 - LocationSizeFactor) &&
						rgbAverage.y <= webCamTexture.height * LocationSizeFactor && rgbAverage.y >= webCamTexture.height * (1 - LocationSizeFactor)) {
						rgbInRect = true;
					}
					//case RGB center & edge is out
					if (rgbInRect && !edgeInRect) {
						edgeFactor -= locationWeightFactor;
						if (edgeFactor <= 0) {
							edgeFactor = 0;
						}
					}
					//case edge center & RGB is out
					if (!rgbInRect && edgeInRect) {
						edgeFactor += locationWeightFactor;
						if (edgeFactor >= 1 ) {
							edgeFactor = 1;
						}
					}
					//average with location factors
					Point edgeAverage = new Point((( (1 - edgeFactor) * rgbAverage.x )+  ((edgeFactor) * WeightedCentroidEdge[0].x)),
						(( (1 - edgeFactor) * rgbAverage.y )+  ((edgeFactor) * WeightedCentroidEdge[0].y)));

					if (popToCenter) {

						//show pop rect
						if (showPopToCenterRect) {
							Imgproc.rectangle (rgbMat, new Point (webCamTexture.width * popToCenterRectFactor, webCamTexture.height * popToCenterRectFactor),
								new Point (webCamTexture.width * (1 - popToCenterRectFactor), webCamTexture.height * (1 - popToCenterRectFactor)), new Scalar (0, 0, 255, 255), 2, 8, 0);
						}
						//case point inside rect
						if (edgeAverage.x <= webCamTexture.width * popToCenterRectFactor && edgeAverage.x >= webCamTexture.width * (1 - popToCenterRectFactor) &&
							edgeAverage.y <= webCamTexture.height * popToCenterRectFactor && edgeAverage.y >= webCamTexture.height * (1 - popToCenterRectFactor)) {
							edgeAverage.x = (int)Math.Round (webCamTexture.width * 0.5);
							edgeAverage.y = (int)Math.Round (webCamTexture.height * 0.5);
						}

					}
					Imgproc.ellipse (rgbMat, edgeAverage, new Size (6,6), 1, 1.5, 360, new Scalar(244, 66, 226,255),13);
					Imgproc.putText(rgbMat, " merged center " + edgeAverage,edgeAverage, 0, 1.3, new Scalar(244, 66, 226,255),2);	

				}
				//average with no location
				if (mergeEdge && WeightedCentroidEdge.Count >= 0 && !calculateLocation) {
					Point edgeAverage = new Point ((((1 - edgeFactor) * rgbAverage.x) + ((edgeFactor) * WeightedCentroidEdge [0].x)),
						                    (((1 - edgeFactor) * rgbAverage.y) + ((edgeFactor) * WeightedCentroidEdge [0].y)));

					//average with edge factor
					Imgproc.ellipse (rgbMat, edgeAverage, new Size (6, 6), 1, 1.5, 360, new Scalar (244, 66, 226, 255), 13);
					Imgproc.putText (rgbMat, " merged center " + edgeAverage, edgeAverage, 0, 1.3, new Scalar (244, 66, 226, 255), 2);	
				


				}
				//#############DISPOSAL
				//case RGB center & edge is out  bring back value
				if (rgbInRect && !edgeInRect) {
					edgeFactor += locationWeightFactor;
					if (edgeFactor <= 0) {
						edgeFactor = 0;
					}
				}
				//case edge center & RGB is out bring back value
				if (!rgbInRect && edgeInRect) {
					edgeFactor -= locationWeightFactor;
					if (edgeFactor >= 1 ) {
						edgeFactor = 1;
					}
				}
			}

			WeightedCentroid.Clear ();
			WeightedCentroidEdge.Clear ();
			moments.Clear ();
			contours.Clear ();
			framesDropCount = 0;
		}

		public void takePhoto(){
			snapshotCount = 20;
			Debug.Log ("TAKE PHOTO");
			Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false, true);

			InvokeRepeating("pauseForPhoto", 0.001f, 0.001f);

			Imgproc.resize(rgbMat, textureInstance, new Size(Screen.width,Screen.height));
			Debug.Log ("texture is" + textureInstance.width() + ", " + textureInstance.height());
			Debug.Log ("tex is" + tex.width + ", " + tex.height);
			Utils.fastMatToTexture2D (textureInstance, tex);

			//write to singleton
			ImageManager.instance.photo = tex;

			//write image
			Imgcodecs.imwrite ("Assets/snapshot.jpeg", textureInstance);
		}
		public void pauseForPhoto(){

			if (snapshotCount <= 0) {
				CancelInvoke ();
				stopForPhoto = false;

			} else {
				
				Utils.matToTexture2D (textureInstance, texturePhoto);

				snapshotCount--;
				Debug.Log ("snapshotCount: " + snapshotCount);
				stopForPhoto = true;
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