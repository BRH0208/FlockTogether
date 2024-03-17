using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanAI : MonoBehaviour
{
    private Vector2 navGoal;
	private bool hasNavGoal;
	
	public float activeSlow = 10.0f;
	public float passiveSlow = 1.0f;
	private Rigidbody2D rb;
	public float maxSpeed = 1.0f;
	public float roatationRate = 0.1f;
	public float orderCompleteDistance = 0.01f;
	
	
	// This human was selected by a player
	public void selected(){
		// Turn on the selected indicator
		GameObject selectObj = GameObject.Find("SelectIcon");
		Renderer selected = selectObj.GetComponent<SpriteRenderer>();
		selected.enabled = true;
	}
	
	// This human is no longer selected by a player
	public void deselected(){
		// Turn off the selected indicator
		GameObject selectObj = GameObject.Find("SelectIcon");
		Renderer selected = selectObj.GetComponent<SpriteRenderer>();
		selected.enabled = false;
	}
	
	// This human was told to move to a position
	public void commandMove(Vector2 pos){
		navGoal = pos;
		hasNavGoal = true;
	}
	
	// Get the current speed
	public float getSpeed(){
		return maxSpeed;
	}
	
	public void Update(){
		if(hasNavGoal){ // If we are currently navigating towards a position
			
			Vector2 relativePos = (navGoal - (Vector2) transform.position);
			float distance = relativePos.magnitude;
			float speed = getSpeed();
			if(distance < orderCompleteDistance){
				// Stop moving
				rb.drag = activeSlow;
				hasNavGoal = false;
			}else{
				// Move more
				relativePos = relativePos / distance;
				rb.drag = passiveSlow;
				rb.AddForce(relativePos * getSpeed(),ForceMode2D.Force);
			}
		}
		
		// We always rotate player strictly in direction of velocity
		Quaternion desiredAngle = Quaternion.Euler(0, 0,Mathf.Rad2Deg * Mathf.Atan2(rb.velocity.y, rb.velocity.x)-90.0f);
		Quaternion currentAngle = transform.rotation;
		transform.rotation = Quaternion.RotateTowards(currentAngle,desiredAngle,roatationRate*Time.deltaTime);
	}
	public void Start(){
		rb = GetComponent<Rigidbody2D>();
		deselected(); // We begin deselected
	}
}
