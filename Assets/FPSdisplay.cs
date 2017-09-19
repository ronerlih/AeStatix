using UnityEngine;
using System.Collections;


public class FPSDisplay : MonoBehaviour
{
	float deltaTime = 0.0f;
	[SerializeField]
	Color color = new Color (50/255f, 250f/255f, 50f/255f);

	void Update()
	{
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
	}

	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;

		GUIStyle style = new GUIStyle();

		Rect rect = new Rect(-5, 5, w, h * 2 / 50);
		style.alignment = TextAnchor.UpperRight;
		style.fontSize = h * 2 / 80;
		//style.fontStyle = FontStyle.Bold;
		style.normal.textColor = color;


		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
		GUI.Label(rect, text, style);
	}
}