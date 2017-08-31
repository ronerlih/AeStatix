using UnityEngine;
using System.Collections;

public class ImageManager : Singelton<ImageManager> {


//	private Texture2D _photo = new Texture2D(Screen.width , Screen.height);
	private Texture2D _photo;
	public Texture2D photo
	{
		get{return _photo;}
		set{ _photo = value; }
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
