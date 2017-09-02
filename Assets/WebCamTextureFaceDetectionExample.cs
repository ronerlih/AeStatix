﻿using UnityEngine;
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
	public class WebCamTextureFaceDetectionExample : MonoBehaviour
	{
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
					cascade.detectMultiScale (grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
						new Size (grayMat.cols () * 0.2, grayMat.rows () * 0.2), new Size ());


				OpenCVForUnity.Rect[] rects = faces.toArray ();
				for (int i = 0; i < rects.Length; i++) {
					//              Debug.Log ("detect faces " + rects [i]);

					Mat faceMat = new Mat (rects [i].x + rects [i].width, rects [i].height, CvType.CV_8UC4);
						
					Imgproc.blur( rgbaMat,rgbaMat, new Size(60,60) );
					//copyMat.copyTo(rgbaMat.submat(rects [i].x, rects [i].y, rects [i].x + rects [i].width, rects [i].y + rects [i].height));
				
					int rowRangeTop; 
					int rowRangeButtom; 
					int colRangeleft; 
					int colRangeRight; 

					if (-rectFactor + rects [i].y <= 0) {
						rowRangeTop = 0;
					} else {
						rowRangeTop = -rectFactor + rects [i].y;
					}
					if (rectFactor + rects [i].y + rects [i].height >= copyMat.height()) {
						rowRangeButtom = copyMat.height();
					} else {
						rowRangeButtom = rectFactor + rects [i].y + rects [i].height;
					}
					if (-rectFactor + rects [i].x <= 0) {
						colRangeleft = 0;
					} else {
						colRangeleft = -rectFactor + rects [i].x;
					}
					if (rectFactor + rects [i].x + rects [i].width >= copyMat.width()) {
						colRangeRight = copyMat.width();
					} else {
						colRangeRight = rectFactor + rects [i].x + rects [i].width;
					}
					
					copyMat.rowRange(rowRangeTop ,rowRangeButtom)
						.colRange( colRangeleft,  colRangeRight)
						   .copyTo(rgbaMat
							.rowRange(rowRangeTop, rowRangeButtom)
							.colRange(colRangeleft, colRangeRight));
					//Imgcodecs.imwrite ("Assets/face.jpeg", copyMat);
					Imgproc.rectangle (rgbaMat, new Point (colRangeleft, rowRangeTop), new Point (colRangeRight, rowRangeButtom), new Scalar (20, 20, 200, 255), 2);

				}


				//              Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

				Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
			}
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