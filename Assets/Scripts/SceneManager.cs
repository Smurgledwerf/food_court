using UnityEngine;
using System.Collections;

public class SceneManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad(gameObject);
		//LoadMainMenu();
	}
	
	// Update is called once per frame
	void Update () {
	}
	
	public void StartGame() {
		//Application.LoadLevel(0);
		Application.LoadLevel("ball_scene");
	}
	
	public void LoadMainMenu(){
		Application.LoadLevel("MainMenu");
	}
	
	public void LoadWinnerScene(){
		Application.LoadLevel("WinnerScene");
	}
}
