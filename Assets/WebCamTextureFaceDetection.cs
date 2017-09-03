using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
	/// <summary>
	/// WebCamTexture face detection example.
	/// An example of face detection using the CascadeClassifier class.
	/// http://docs.opencv.org/3.2.0/db/d28/tutorial_cascade_classifier.html
	/// </summary>
	[RequireComponent(typeof(WebCamTextureToMatHelper))]
	public class WebCamTextureFaceDetection : MonoBehaviour
	{
		[SerializeField]
		[Range(10,200)]
		int blurPixelSize = 100;
		[SerializeField]
		[Range(0,300)]
		int rectFactor = 100;
		/// <summary>
		/// The gray mat.
		/// </summary>
		Mat grayMat;
		Mat copyMat;
		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// The cascade.
		/// </summary>
		CascadeClassifier cascade;

		/// <summary>
		/// The faces.
		/// </summary>
		MatOfRect faces;

		/// <summary>
		/// The webcam texture to mat helper.
		/// </summary>
		WebCamTextureToMatHelper webCamTextureToMatHelper;

		//frame range
		int rowRangeTop; 
		int rowRangeButtom; 
		int colRangeleft; 
		int colRangeRight; 

		//last frame range
		int? rowRangeTopLast;
		int? rowRangeButtomLast;
		int? colRangeleftLast;
		int? colRangeRightLast;

		int rowRangeTopResult;
		int rowRangeButtomResult;
		int colRangeleftResult;
		int colRangeRightResult;

		//fframe skip
		int noFaceFrameCount = 0;
		[SerializeField]
		[Range(0,20)]
		int maxNegativeFrames = 5;
		OpenCVForUnity.Rect[] rectsLast;
		[SerializeField]
		[Range(0,200)]
		int hightCorrection = 50;
		[SerializeField]
		bool showRect = true;
		[SerializeField]
		bool stabilizeRect = false;
		//[SerializeField]
		//[Range(0.001f,0.999f)]
		float stabilizeFactor = 0.5f;
		[SerializeField]
		bool colorBlur = false;
		[SerializeField]
		[Range(0,100)]
		int redBlur = 50;
		[SerializeField]
		[Range(0,100)]
		int greenBlur = 50;
		[SerializeField]
		[Range(0,100)]
		int blueBlur = 50;
	

		#if UNITY_WEBGL && !UNITY_EDITOR
		Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
		#endif

		// Use this for initialization
		void Start ()
		{
			webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();

			#if UNITY_WEBGL && !UNITY_EDITOR
			var getFilePath_Coroutine = Utils.getFilePathAsync ("lbpcascade_frontalface.xml", (result) => {
			coroutines.Clear ();

			cascade = new CascadeClassifier ();
			cascade.load (result);

			webCamTextureToMatHelper.Initialize ();
			});
			coroutines.Push (getFilePath_Coroutine);
			StartCoroutine (getFilePath_Coroutine);
			#else
			cascade = new CascadeClassifier ();
			cascade.load (Utils.getFilePath ("lbpcascade_frontalface.xml"));
			//            cascade = new CascadeClassifier ();
			//            cascade.load (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));
			//            if (cascade.empty ()) {
			//                Debug.LogError ("cascade file is not loaded.Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
			//            }

			webCamTextureToMatHelper.Initialize ();
			#endif
		}

		/// <summary>
		/// Raises the web cam texture to mat helper initialized event.
		/// </summary>
		public void OnWebCamTextureToMatHelperInitialized ()
		{
			Debug.Log ("OnWebCamTextureToMatHelperInitialized");

			Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();
			copyMat = new Mat (webCamTextureMat.width(), webCamTextureMat.height(), CvType.CV_8UC4);
			texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

			gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
			Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


			float width = webCamTextureMat.width ();
			float height = webCamTextureMat.height ();

			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;
			if (widthScale < heightScale) {
				Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
			} else {
				Camera.main.orthographicSize = height / 2;
			}

			grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);

			faces = new MatOfRect ();
		}

		/// <summary>
		/// Raises the web cam texture to mat helper disposed event.
		/// </summary>
		public void OnWebCamTextureToMatHelperDisposed ()
		{
			Debug.Log ("OnWebCamTextureToMatHelperDisposed");

			if (grayMat != null)
				grayMat.Dispose ();

			if (faces != null)
				faces.Dispose ();
		}

		/// <summary>
		/// Raises the web cam texture to mat helper error occurred event.
		/// </summary>
		/// <param name="errorCode">Error code.</param>
		public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
		{
			Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
		}

		// Update is called once per frame
		void Update ()
		{
			if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

				Mat rgbaMat = webCamTextureToMatHelper.GetMat ();
				copyMat = rgbaMat.clone ();

				Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
				Imgproc.equalizeHist (grayMat, grayMat);


				if (cascade != null)
					cascade.detectMultiScale (grayMat,
											  faces, 
											  1.1, 2, 2, 
											  new Size (grayMat.cols () * 0.2, grayMat.rows () * 0.2), 
											  new Size ());

				OpenCVForUnity.Rect[] rects = faces.toArray ();

				if (rects.Length == 0  ) {
					noFaceFrameCount++;
					if (rectsLast != null && noFaceFrameCount <= maxNegativeFrames && rectsLast.Length > 0) {
						blurBackground (rectsLast, rgbaMat);
					} else {
					//clear last rect fields
						rowRangeTopLast = null;
						rowRangeButtomLast = null;
						colRangeleftLast = null;
						colRangeRightLast = null;
					}
				}
				if (rects != null && rects.Length > 0) {

					blurBackground (rects, rgbaMat);

					noFaceFrameCount = 0;
					rectsLast = null;
					rectsLast = new OpenCVForUnity.Rect[rects.Length];
					for (int i = 0; i < rects.Length; i++) {
						rectsLast[i] = rects[i];
					}

				}

			

				//              Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

				Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
			}
		}

		public void blurBackground(OpenCVForUnity.Rect[] _rects, Mat _mat ){
			Imgproc.blur (_mat, _mat, new Size (blurPixelSize, blurPixelSize));
			if (colorBlur) {
				Core.add (_mat, new Scalar (redBlur, greenBlur, blueBlur), _mat);
			}
			//rect and blur background
			for (int i = 0; i < _rects.Length; i++) {
				//              Debug.Log ("detect faces " + rects [i]);


				//copyMat.copyTo(rgbaMat.submat(rects [i].x, rects [i].y, rects [i].x + rects [i].width, rects [i].y + rects [i].height));

				// Check mat range
				if (-rectFactor + _rects [i].y - hightCorrection <= 0) {
					rowRangeTop = 0;
				} else {
					rowRangeTop = -rectFactor + _rects [i].y - hightCorrection;
				}
				if (rectFactor + _rects [i].y + _rects [i].height >= copyMat.height()) {
					rowRangeButtom = copyMat.height();
				} else {
					rowRangeButtom = rectFactor + _rects [i].y + _rects [i].height;
				}
				if (-rectFactor + _rects [i].x <= 0) {
					colRangeleft = 0;
				} else {
					colRangeleft = -rectFactor + _rects [i].x;
				}
				if (rectFactor + _rects [i].x + _rects [i].width >= copyMat.width()) {
					colRangeRight = copyMat.width();
				} else {
					colRangeRight = rectFactor + _rects [i].x + _rects [i].width;
				}

				if (stabilizeRect) {
					if (rowRangeTopLast != null) {
						rowRangeTopResult = (int)Math.Round((1 - stabilizeFactor) * rowRangeTop +  stabilizeFactor * (int)rowRangeTopLast);
						rowRangeButtomResult = (int)Math.Round((1 - stabilizeFactor) * rowRangeButtom + stabilizeFactor * (int)rowRangeButtomLast); 
						colRangeleftResult = (int)Math.Round((1 - stabilizeFactor) * colRangeleft + stabilizeFactor * (int)colRangeleftLast);
						colRangeRightResult = (int)Math.Round((1 - stabilizeFactor) * colRangeRight + stabilizeFactor * (int)colRangeRightLast); 
					}else{
						rowRangeTopResult = rowRangeTop;
						rowRangeButtomResult = rowRangeButtom;
						colRangeleftResult = colRangeleft;
						colRangeRightResult = colRangeRight;
					}
				} else {
					rowRangeTopResult = rowRangeTop;
					rowRangeButtomResult = rowRangeButtom;
					colRangeleftResult = colRangeleft;
					colRangeRightResult = colRangeRight;
				}



				rowRangeTopLast = rowRangeTop;
				rowRangeButtomLast = rowRangeButtom;
				colRangeleftLast = colRangeleft;
				colRangeRightLast = colRangeRight;

				//save values for stabilaztion


				copyMat.rowRange(rowRangeTopResult ,rowRangeButtomResult)
					.colRange( colRangeleftResult,  colRangeRightResult)
					.copyTo(_mat
						.rowRange(rowRangeTopResult , rowRangeButtomResult)
						.colRange(colRangeleftResult, colRangeRightResult));
				//Imgcodecs.imwrite ("Assets/face.jpeg", copyMat);
				if (showRect) {
					Imgproc.rectangle (_mat, new Point (colRangeleftResult, rowRangeTopResult ), new Point (colRangeRightResult, rowRangeButtomResult), new Scalar (100, 100, 250, 35), 2);
				}
			}
		}
		void CheckMatBorder(){
		}
		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		void OnDestroy ()
		{
			webCamTextureToMatHelper.Dispose ();

			if (cascade != null)
				cascade.Dispose ();

			#if UNITY_WEBGL && !UNITY_EDITOR
			foreach (var coroutine in coroutines) {
			StopCoroutine (coroutine);
			((IDisposable)coroutine).Dispose ();
			}
			#endif
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
			webCamTextureToMatHelper.Play ();
		}

		/// <summary>
		/// Raises the pause button click event.
		/// </summary>
		public void OnPauseButtonClick ()
		{
			webCamTextureToMatHelper.Pause ();
		}

		/// <summary>
		/// Raises the stop button click event.
		/// </summary>
		public void OnStopButtonClick ()
		{
			webCamTextureToMatHelper.Stop ();
		}

		/// <summary>
		/// Raises the change camera button click event.
		/// </summary>
		public void OnChangeCameraButtonClick ()
		{
			webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
		}
	}
}