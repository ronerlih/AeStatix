using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity;
using System.IO;

public class CenterOfMassTests : MonoBehaviour {

	// Use this for initialization
	void Start () {
		//
		List <Mat> folder = new List<Mat> ();
		List <Moments> moments = new List<Moments> ();
		Point pointMoments;
		Scalar gray = new Scalar (120);

		string inputPath = "/Users/ron/Desktop/AeStatic-unity/AeStatix/Assets/StreamingAssets/center-of-mass-tests/";
		string[] fileNames = Directory.GetFiles (inputPath);

		int fileCount = 0;
		foreach (string fileName in fileNames) {
			Mat inputMat = Imgcodecs.imread (fileName);
			Imgproc.cvtColor (inputMat, inputMat, Imgproc.COLOR_BGR2GRAY);

			//get moments
			moments.Add( Imgproc.moments(inputMat));
			pointMoments = new Point ((moments [0].m10 / moments [0].m00), (moments [0].m01 / moments [0].m00));
			Imgproc.circle (inputMat,pointMoments, 6, gray, 10);
			Imgproc.putText (inputMat, "opencv center " + pointMoments, new Point ((moments [0].m10 / moments [0].m00), (moments [0].m01 / moments [0].m00) - 10), 1, 1, gray, 1);
			//write file
			Imgcodecs.imwrite ( inputPath + "output/" + fileCount + ".jpg", inputMat);
			Debug.Log (inputPath + "output/" + fileCount + ".jpg");

			moments.Clear ();
			fileCount++;
				
		}

	}
}
