using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TakePhotoEvent : MonoBehaviour {

	public delegate void TakePhoto(GameObject _webCamTexture);
	public static event TakePhoto OnTakePhoto;

	void isButton(){
		//button.onClick.AddListener(TaskOnClick);
		UnityEngine.UI.Button button = GameObject.Find("takePhoto").GetComponent<UnityEngine.UI.Button>();
		if(OnTakePhoto != null){
			//button.onClick.OnTakePhoto (GameObject.Find  );
		}
	}
}
