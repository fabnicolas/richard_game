﻿using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour, IMovable {
	Animator m_Animator;
	public float Speed = 10.0f;
	public float JumpSpeed = 7.0f;
	public float WallJumpSpeed = 5.0f;
	public float PolloSpeed = 10.0f;
	public LayerMask GroundLayers;
	public GameObject weapon;

	private Rigidbody2D rigidbody_2d;
	private Transform m_GroundCheckL,m_GroundCheckR,m_WallJumpTOP,m_WallJumpBOTTOM;

	private Vector3 defaultpos;
	public bool canMoveCharacter=true;

	private float ctime_pollospeed=-1;//,ctime_walljump=-1;
	// Use this for initialization
	void Start () {
		m_Animator = this.GetComponent<Animator>();
		rigidbody_2d = this.GetComponent<Rigidbody2D>();
		m_GroundCheckL = this.transform.FindChild("GroundCheckL");
		m_GroundCheckR = this.transform.FindChild("GroundCheckR");
		m_WallJumpTOP = this.transform.FindChild("WallJumpTOP");
		m_WallJumpBOTTOM = this.transform.FindChild("WallJumpBOTTOM");
		defaultpos = this.transform.position;
	}

	bool isGrounded = true;
	bool isTouchingWall = false;
	float speed = 0.0f; // Disables animation by setting speed to 0, if shouldAnimateWalk stays false.

	bool can_firePollo = true;

	bool jump = false;
	bool walljump = false;
	bool firePollo = false;

	void Update () {
		bool isGroundedL = Physics2D.OverlapPoint(m_GroundCheckL.position, GroundLayers);
		bool isGroundedR = Physics2D.OverlapPoint(m_GroundCheckR.position, GroundLayers);
		isGrounded = (isGroundedL || isGroundedR);

		bool isWJA1 = Physics2D.OverlapPoint(m_WallJumpTOP.position, GroundLayers);
		bool isWJA2 = Physics2D.OverlapPoint(m_WallJumpBOTTOM.position, GroundLayers);
		isTouchingWall = (isWJA1 && isWJA2);

		if(canMoveCharacter) {
			if(Input.GetButtonDown("Jump")) {
				if(isGrounded) {
					jump = true;
					Debug.Log("JUMP OK");
					isGrounded = false;
				} else if(isTouchingWall) {
					walljump = true;
					Debug.Log("WALLJUMP!!!");
					isGrounded = false;

					//ctime_walljump = Time.time;
				}
			}
		}
		// Callback to the animator warning him "hey, this is the actual speed".
		m_Animator.SetFloat("Speed", speed);
		m_Animator.SetBool("IsGrounded", isGrounded);
		
		//Debug.Log("IsGrounded=" + isGrounded);
		if(Input.GetButtonDown("Fire1") && ((Time.time - ctime_pollospeed) > 6f || can_firePollo)) {
			firePollo = true;
			ctime_pollospeed = Time.time;
		}
	}

	void FixedUpdate(){
		if(canMoveCharacter) {
			float horizontalInput = Input.GetAxis("Horizontal");
			if(horizontalInput > 0) {
				this.setFacing(true); // invertire = moonwalk LOL
			} else if(horizontalInput < 0) {
				this.setFacing(false);
			}
			//this.setFacingL((horizontalInput > 0)); // >0 = right, <0 = left, =0 = not moving
			//bool shouldAnimateWalk = (horizontalInput != 0); // !=0 = moving
			//if(shouldAnimateWalk) {
				// Enables animation. In this case, since condition activator is "speed", depending on horizontalInput, that if could be avoided.
			speed = (horizontalInput >= 0) ? horizontalInput : -horizontalInput;
			//}
			//Debug.Log("Velocity = " + this.rigidbody_2d.velocity.ToString());
			this.rigidbody_2d.velocity = new Vector2(horizontalInput*Speed, this.rigidbody_2d.velocity.y);
			//Vector2 newpos = new Vector2(horizontalInput*Speed, 0);
			//this.rigidbody_2d.MovePosition(new Vector2(transform.position.x, transform.position.y) + newpos* Time.deltaTime);
			//Debug.Log("VEL=" + this.rigidbody_2d.velocity);

			if(jump) {
				rigidbody_2d.AddForce(new Vector2(0, 1) * JumpSpeed, ForceMode2D.Impulse);
				jump = false;
			} else if(walljump) {
				//Vector3 direction = (transform.position + new Vector3(1*(this.getFacing() ? 1 : -1), 2, 0)).normalized;
				//rigidbody_2d.MovePosition(transform.position + direction * WallJumpSpeed * Time.deltaTime);
				//rigidbody_2d.velocity = new Vector2((this.getFacing() ? 1 : -1), 1) * WallJumpSpeed;//, ForceMode2D.Force;
				this.setFacing(!this.getFacing());
				this.StartCoroutine(MoveOverSeconds(gameObject, transform.position + new Vector3(2*-faceToInt(getFacing()), 3, 0), 0.3f));
				walljump=false;
				//this.rigidbody_2d.velocity = new Vector2(-this.rigidbody_2d.velocity.x, this.rigidbody_2d.velocity.y);
				//this.setFacingL(!this.getFacing()); // ignored?
			}
			if(firePollo) {
				can_firePollo = false;
				Vector3 weapon_position = transform.position + (getFacing() ? Vector3.right : Vector3.left);
				GameObject go = Instantiate(weapon, weapon_position, transform.rotation) as GameObject;
				IFire interface_fire = (IFire) go.GetComponent<PolloBoomerangScript>();
				interface_fire.fire(gameObject);
				firePollo = false;
			}
		}


		if(this.transform.position.y < -15) {
			this.transform.position = this.defaultpos;
		}
	}

	private void setFacing(bool is_right_facing){
		if(is_right_facing) {
			transform.localScale = new Vector3(-1, 1, 1);
		} else {
			transform.localScale = new Vector3(1, 1, 1);
		}
	}

	private bool getFacing(){
		return (transform.localScale.x<0); // if he's watching at right
	}

	private int faceToInt(bool value){
		return value ? -1 : 1; // right:-1, left:1
	}

	public void setIfMovable(bool condition){
		canMoveCharacter = condition;
		this.rigidbody_2d.velocity = new Vector2(0, 0);
		m_Animator.SetFloat("Speed", 0.0f);
	}

	public void setIfCanFirePollo(bool condition){
		can_firePollo = condition;
	}

	public IEnumerator MoveOverSpeed (GameObject objectToMove, Vector3 end, float speed){
		// speed should be 1 unit per second
		float gv = objectToMove.GetComponent<Rigidbody2D>().gravityScale;
		objectToMove.GetComponent<Rigidbody2D>().gravityScale = 0;

		if(objectToMove.layer == LayerMask.NameToLayer("Player")) {
			canMoveCharacter = false;
		}

		while (objectToMove.transform.position != end && !this.isTouchingWall)
		{
			objectToMove.transform.position = Vector3.MoveTowards(objectToMove.transform.position, end, speed * Time.deltaTime);
			yield return new WaitForEndOfFrame ();
		}

		if(objectToMove.layer == LayerMask.NameToLayer("Player")) {
			canMoveCharacter = true;
		}

		objectToMove.GetComponent<Rigidbody2D>().gravityScale = gv;
	}
	public IEnumerator MoveOverSeconds (GameObject objectToMove, Vector3 end, float seconds)
	{
		float gv = objectToMove.GetComponent<Rigidbody2D>().gravityScale;
		objectToMove.GetComponent<Rigidbody2D>().gravityScale = 0;

		if(objectToMove.layer == LayerMask.NameToLayer("Player")) {
			canMoveCharacter = false;
		}

		float elapsedTime = 0;
		Vector3 startingPos = objectToMove.transform.position;
		while (elapsedTime < seconds)
		{
			objectToMove.transform.position = Vector3.Lerp(startingPos, end, (elapsedTime / seconds));
			elapsedTime += Time.deltaTime;
			yield return new WaitForEndOfFrame();
			if(this.isTouchingWall)
				break;
		}
		if(elapsedTime >= seconds) objectToMove.transform.position = end;

		if(objectToMove.layer == LayerMask.NameToLayer("Player")) {
			canMoveCharacter = true;
		}

		objectToMove.GetComponent<Rigidbody2D>().AddForce(new Vector2(faceToInt(getFacing()), 1), ForceMode2D.Impulse);
		objectToMove.GetComponent<Rigidbody2D>().gravityScale = gv;
	}
}