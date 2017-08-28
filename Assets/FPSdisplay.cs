using UnityEngine;
using System.Collections;

public class FPSdisplay : MonoBehaviour
{
	float deltaTime = 0.0f;

	void Update()
	{
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
	}

	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;

		GUIStyle style = new GUIStyle();

		Rect rect = new Rect(-5, 9, w, h * 2 / 50);
		style.alignment = TextAnchor.UpperRight;
		style.fontSize = h * 2 / 60;
		style.normal.textColor = new Color (66f/255f, 1.0f, 203f/255f);


		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
		GUI.Label(rect, text, style);
	}
}