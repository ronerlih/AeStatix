using UnityEngine;
using System.Collections;

public class Singelton<T> : MonoBehaviour where T : MonoBehaviour {

	private static T _instance;

	public static T instance{

		get{
			//is instance null?
			if (_instance == null){

				//try to find
				_instance = GameObject.FindObjectOfType<T>();

				if (_instance == null) {
					GameObject singelton = new GameObject (typeof(T).Name);
					_instance = singelton.AddComponent<T> ();
				}
			}
			return _instance;
		}
	}

	public virtual void Awake(){
		if (_instance == null) {
			_instance = this as T;
			DontDestroyOnLoad (gameObject);
		} else {
			Destroy (gameObject);	
		}
	}
}
