using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI;

public class UnityToXcodePhotoEvent : MonoBehaviour
{
	UnityEvent UnityToXcodePhoto;

	void Start()
	{
		if (UnityToXcodePhoto == null)
			UnityToXcodePhoto = new UnityEvent();

		UnityToXcodePhoto.AddListener(Ping);
		Button photoButton = GameObject.Find ("takePhoto").GetComponent<Button> ();
		photoButton.onClick.AddListener (InvokeEvent); 
	}


	void InvokeEvent()
	{
		new WaitForSeconds (0.2f);
		UnityToXcodePhoto.Invoke();
		Debug.Log("event invoked");
	}
	//event action
	void Ping()
	{
		Debug.Log("Photo Event");
	}
}