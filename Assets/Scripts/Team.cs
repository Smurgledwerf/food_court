using UnityEngine;
using System.Collections;

public class Team : MonoBehaviour {

	public string teamName = "";
	public float points = 0.0f;
	public GameObject scoreBar;
	public GameObject colorBar;
	public float barHeight = 5;
	public GameObject scoreText;
	
	private float totalPoints;
	private Color color = Color.magenta;
	private int numberOfTeams;
	
	void Awake () {
		totalPoints = GameObject.Find("LevelBuilder").GetComponent<LevelBuilder>().totalPoints;
		numberOfTeams = GameObject.Find("LevelBuilder").GetComponent<LevelBuilder>().numberOfTeams;
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public Color GetColor() {
		return color;
	}
	
	public void SetColor(Color new_color) {
		color = new_color;
		colorBar.renderer.material.color = color;
	}
	
	public void GivePoints(int points_given) {
		points += points_given;
		scoreBar.transform.localScale = new Vector3((points / (totalPoints / numberOfTeams)) * barHeight, 1, 1);
		if (points >= totalPoints / numberOfTeams) {
			points = totalPoints / numberOfTeams;
			GameObject.Find("GameController").GetComponent<GameController>().EndGame(this);
		}
	}
	
	public void SetTeamName(string name) {
		teamName = name;
	}
}
