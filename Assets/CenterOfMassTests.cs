using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity;
using System.IO;
using System;

public class CenterOfMassTests : MonoBehaviour {

	// Use this for initialization
	void Start () {

		// moments
		List <Moments> moments = new List<Moments> ();
		Point pointMoments;

		//display
		Scalar gray = new Scalar (120);
		Scalar darkGray = new Scalar (90);

		//manual
		double weightsSum=0;
		double colsWeightedSum=0;
		double rowsWeightedSum=0;
		double manualX=0;
		double manualY=0;

		//IO
		string inputPath = "/Users/ron/Desktop/AeStatic-unity/AeStatix/Assets/StreamingAssets/center-of-mass-tests/";
		string outputPath = "/Users/ron/Desktop/AeStatic-unity/AeStatix/Assets/StreamingAssets/output/";
		string[] fileNames = Directory.GetFiles (inputPath);

		int fileCount = 0;

		//clean directory

		DirectoryInfo directoryInfo = new DirectoryInfo(outputPath);

		foreach (FileInfo file in directoryInfo.GetFiles())
		{
			file.Delete(); 
		}

		//TESTS:
		foreach (string fileName in fileNames) {
			if (Path.GetExtension (fileName).ToUpperInvariant() == ".JPG") {
				Mat inputMat = Imgcodecs.imread (fileName);
				Imgproc.cvtColor (inputMat, inputMat, Imgproc.COLOR_BGR2GRAY);

				//get moments
				moments.Add (Imgproc.moments (inputMat));
				pointMoments = new Point ((moments [0].m10 / moments [0].m00), (moments [0].m01 / moments [0].m00));

				//display
				Imgproc.circle (inputMat, pointMoments, 6, gray, 10);
				Imgproc.putText (inputMat, " opencv center " + pointMoments, new Point ((moments [0].m10 / moments [0].m00), (moments [0].m01 / moments [0].m00) - 10), 1, 1, gray, 2);

				//maunal
				Debug.Log("file name: " + fileName + "\n");
				for (int x = 0; x < inputMat.cols () ; x++) {
					for (int y = 0; y < inputMat.rows () ; y++) {
						
						double[] buff = inputMat.get (y, x);

						weightsSum = weightsSum + buff [0];

						//no correction
//						colsWeightedSum = colsWeightedSum + ( x ) * (buff [0]);
//						rowsWeightedSum = rowsWeightedSum + ( y ) * (buff [0]);

//						//0 row correction
						if (x != 0 && y != 0) {
							colsWeightedSum = colsWeightedSum + (x) * (buff [0]);
							rowsWeightedSum = rowsWeightedSum + (y) * (buff [0]);
						} 
						if (x == 0) {
							colsWeightedSum = colsWeightedSum + (buff [0]);
							rowsWeightedSum = rowsWeightedSum + (y) * (buff [0]);
						}
						if (y == 0) {
							colsWeightedSum = colsWeightedSum + (x) * (buff [0]);
							rowsWeightedSum = rowsWeightedSum +  (buff [0]);
						}
					}
				}
				//point
				manualX = colsWeightedSum / weightsSum;
				manualY = rowsWeightedSum / weightsSum;
				Debug.Log ("manual point: (" + manualX + ", " + manualY + ")\n");
				Debug.Log ("opencv point: (" + pointMoments.x + ", " + pointMoments.y + ")\n");

				//display
				Imgproc.circle (inputMat, new Point (manualX, manualY), 2, darkGray, 5);
				Imgproc.putText (inputMat, " manual center (" + manualX.ToString() + ", " + manualY.ToString() , new Point (manualX, manualY + 10), 1, 1, darkGray, 2);

				//write file
				Imgcodecs.imwrite (outputPath + fileCount + ".jpg", inputMat);
				Debug.Log (outputPath + fileCount + ".jpg\n");

				manualX = 0;
				manualY = 0;
				weightsSum = 0;
				colsWeightedSum = 0;
				rowsWeightedSum = 0;

				moments.Clear ();
				fileCount++;
			}
		}


	}
}
