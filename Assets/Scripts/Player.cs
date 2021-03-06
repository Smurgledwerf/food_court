using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour {
	
	public bool canMove = true;
	public bool canAnimate = true;
	
	public string InputSource = "Keyboard";
	private KeyCode LeftKey, RightKey, DownKey, JumpKey;
	public float PlayerSpeed = 25.0f;
	public float BallCarrySpeed = 20.0f;
	public float PlayerAcceleration = 300.0f;
	public float JumpHeight = 20.0f;
	public bool IsInvulnerable = false;
	public float f_InvulnerableCooldown = 1.0f;
	public bool CanJump = true;
	public bool HasBall = false;
	public bool CanCatch = true;
	public float f_CatchCooldown = 0.5f;
	public Ball CarriedBall = null;
	public Vector3 BallOffset = new Vector3(0, 1.0f, 0);
	public GameObject ScoreBar;
	public Team team;
	public float f_footStepInterval = 0.2f;
	public SoundProfile playerSoundProfile;
	
	public int playerIndex = -1;
	
	private OTAnimatingSprite sprite;
	private string playingFrameset = "";
	
	private float f_elapsedFootTime = 0.0f;
	
	private GlobalOptions options;
	
	private bool OnPlatform = false;
	public bool TouchingWall = false;
	
	private float height;
	
	void Awake () {
		sprite = GetComponentInChildren<OTAnimatingSprite>();
		options = GlobalOptions.Instance;
	}
	
	// Use this for initialization
	void Start () {
		if (playerIndex == -1) {
			return;
		}
		InputSource = options.GetPlayerInputSource(playerIndex);
		Dictionary<string,KeyCode> playerConfig = new Dictionary<string,KeyCode>();
		playerConfig = options.GetPlayerConfig(playerIndex);
		if(InputSource == "Keyboard"){
			LeftKey = playerConfig["MoveLeft"];
			RightKey = playerConfig["MoveRight"];
			DownKey = playerConfig["MoveDown"];
		}
		JumpKey = playerConfig["Jump"];
		height = collider.bounds.size.y;
	}
	
	// Update is called once per frame
	void Update () {
		if (canMove) {
		
			HandleHorizontal();
			HandleVertical();
			
			HandleJump();
		}
		if (canAnimate) {
			HandleAnimation();
		}
		HandleSound();
	}
	
	public void HandleHorizontal(){
		float horizontal = 0.0f;
		if(InputSource == "Keyboard"){
			if(Input.GetKey(LeftKey)){
				horizontal = -1.0f;
			}
			else if(Input.GetKey(RightKey)){
				horizontal = 1.0f;
			}
		}
		else{
			string[] axes = new string[3]{"LeftX", "DpadX", "RightX"};
			foreach(string axis in axes){
				float value = Input.GetAxis(InputSource + axis);
				if(Mathf.Abs(value) > 0.1f){
					horizontal = value;
					break;
				}
			}
		}
		
		Vector3 velocity = rigidbody.velocity;
		if(horizontal == 0){
			velocity.x = 0;
			rigidbody.velocity = velocity;
			return;
		}
		if(horizontal > 0){
			transform.eulerAngles = new Vector3(0, 180, 0);
		}
		else if(horizontal < 0){
			transform.eulerAngles = Vector3.zero;
		}
		velocity.x += horizontal * PlayerAcceleration * Time.deltaTime;
		float maxSpeed = HasBall ? BallCarrySpeed : PlayerSpeed;
		velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
		rigidbody.velocity = velocity;
	}
	
	public void HandleVertical(){
		float vertical = 0.0f;
		if(InputSource == "Keyboard"){
			if(Input.GetKey(DownKey)){
				vertical = -1.0f;
			}
		}
		else{
			string[] axes = new string[3]{"LeftY", "DpadY", "RightY"};
			foreach(string axis in axes){
				float value = Input.GetAxis(InputSource + axis);
				if(Mathf.Abs(value) > 0.1f){
					vertical = value;
					break;
				}
			}
		}
		
		if(vertical < -0.5f && OnPlatform){
			gameObject.layer = 12;
			CanJump = false;
			OnPlatform = false;
		}
	}
	
	public void HandleJump(){
		if(CanJump){
			if(Input.GetKeyDown(JumpKey)){
				PlayJumpSound();
				CanJump = false;
				Vector3 velocity = rigidbody.velocity;
				rigidbody.velocity = velocity + new Vector3(0, JumpHeight, 0);
				if(HasBall){
					ReleaseBall(velocity + new Vector3(0, JumpHeight, 0));
				}
			}
		}
	}
	
	public bool IsJumping(){
		if(CanJump){
			return false;
		}
		else {
			return true;
		}
	}
	
	void OnCollisionEnter(Collision collision){
		switch (collision.gameObject.tag) {
		case "Floor":
			CanJump = true;
			OnPlatform = false;
			break;
		case "Platform":
			CanJump = true;
			OnPlatform = true;
			break;
		case "Wall":
			TouchingWall = true;
			break;
		case "Player":
			PlayerCollision(collision);
			break;
		}
	}
	
	void PlayerCollision (Collision collision) {
		Player otherPlayer = collision.gameObject.GetComponent<Player>();
		if(CanStealBall(otherPlayer)){
			Ball theBall = otherPlayer.CarriedBall;
			otherPlayer.LoseBall();
			CatchBall(theBall);
			IsInvulnerable = true;
			StartCoroutine(InvulnerableCooldown(f_InvulnerableCooldown));
		}
		
		// If the colliding player's position is more than one height of a player,
		// then we can jump again!
		float distanceUp = transform.position.y - collision.transform.position.y;
		if (distanceUp > height * 0.9) {
			CanJump = true;
		}
	}
	
	bool CanStealBall(Player otherPlayer) {
		if(otherPlayer.HasBall
				&& !otherPlayer.IsInvulnerable
				&& CanCatch
				&& team != otherPlayer.team){
			return true;
		} else {
			return false;
		}
	}
	
	void OnCollisionExit(Collision collision){
		switch(collision.gameObject.tag){
		case "Wall":
			TouchingWall = false;
			break;
		}
	}
	
	public void GivePoints(int points){
		team.GivePoints(points);
	}
	
	public void CatchBall(Ball ball){
		if(CanCatch){
			HasBall = true;
			CarriedBall = ball;
			CarriedBall.rigidbody.velocity = Vector3.zero;
			CarriedBall.GrabBall(this);
			PlayCatchSound();
		}
	}
	
	public void LoseBall(){
		HasBall = false;
		CanCatch = false;
		CarriedBall = null;
		StartCoroutine(CatchCooldown(f_CatchCooldown));
		PlayHitSound();
	}
	
	public void ReleaseBall(Vector3 velocity){
		CarriedBall.ReleaseBall(velocity, this);
		LoseBall();
	}
	
	IEnumerator CatchCooldown(float cooldown){
		yield return new WaitForSeconds(cooldown);
		CanCatch = true;
	}
	
	IEnumerator InvulnerableCooldown(float cooldown){
		yield return new WaitForSeconds(cooldown);
		IsInvulnerable = false;
	}
	
	public void HandleAnimation(){
		if (HasBall) {
			if(TouchingWall){
				PlayAnimation("GrabCarry");
			}
			else if (IsJumping()) {
				// has ball, jumping
				PlayAnimation("JumpCarry");
			}
			else if (rigidbody.velocity.magnitude > 1) {
				// has ball, walking
				PlayAnimation("WalkCarry");
			}
			else {
				// has ball, standing
				PlayAnimation("StandCarry");
			}
		}
		else {
			if(TouchingWall){
				PlayAnimation("Grab");
			}
			else if (IsJumping()) {
				// jumping, no ball
				PlayAnimation("Jump");
			}
			else if (rigidbody.velocity.magnitude > 1) {
				// walking, no ball
				PlayAnimation("Walk");
			}
			else {
				// standing, no ball
				PlayAnimation("Stand");
			}
		}
	}
	
	void PlayJumpSound()
	{
		AudioManager.Get().PlaySound(GetCurrentPanAmount(), AudioManager.AUDIOTEMPLATE.JUMP, playerSoundProfile.GetJump());
	}
	
	void PlayHitSound()
	{
		AudioManager.Get().PlaySound(GetCurrentPanAmount(), AudioManager.AUDIOTEMPLATE.JUMP, playerSoundProfile.GetHit());
	}
	
	void PlayCatchSound()
	{
		AudioManager.Get().PlaySound(GetCurrentPanAmount(), AudioManager.AUDIOTEMPLATE.JUMP, playerSoundProfile.GetCatch());
	}
	
	float GetCurrentPanAmount()
	{
		Vector3 viewPos = Camera.main.WorldToViewportPoint(gameObject.transform.position);
		
		return (viewPos.x*2) - 1 ;
		
	}
	
	public void HandleSound(){
		if(f_elapsedFootTime < 1.0f)
		{
			f_elapsedFootTime += Time.deltaTime;
		}
		
		if(f_elapsedFootTime > f_footStepInterval)
		{
			if (HasBall) {
				if (!IsJumping() && rigidbody.velocity.magnitude > 1) {
					// has ball, walking
					AudioManager.Get().PlaySound(GetCurrentPanAmount(), AudioManager.AUDIOTEMPLATE.FOOTSTEP, playerSoundProfile.GetFootstep());
					f_elapsedFootTime = 0;
				}
			}
			else {
				if (!IsJumping() && rigidbody.velocity.magnitude > 1) {
					// walking, no ball
					PlayAnimation("Walk");
					AudioManager.Get().PlaySound(GetCurrentPanAmount(), AudioManager.AUDIOTEMPLATE.FOOTSTEP, playerSoundProfile.GetFootstep());
					f_elapsedFootTime = 0;
				}
			}
		}
	}
	
	public void PlayAnimation(string frameset) {
		if (playingFrameset != frameset) {
			sprite.PlayLoop(frameset);
		}
	}
	
	public Color GetColor() {
		return team.GetColor();
	}
	
	public GameObject GetScoreText () {
		return team.scoreText;
	}
	
	public Transform GetParticlePoint () {
		return team.GetParticlePoint();
	}
}
