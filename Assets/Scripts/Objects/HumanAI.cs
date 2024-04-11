using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanAI : MonoBehaviour, commandable
{
	
    private Vector2 navGoal;
	private bool hasNavGoal;
	
	private interactable interact;
	
	public float activeSlow = 10.0f;
	public float passiveSlow = 1.0f;
	private Rigidbody2D rb;
	public float walkSpeed = 1.0f;
	public float sprintMod = 1.2f;
	public float roatationRate = 0.1f;
	public bool isDeactive;
	public commandable.Mode mode;
	public float movePrecision = 0.01f;
	public float interactPrecision = 0.015f;
	public GameObject obj{get{return gameObject;}}
	
	// This human was selected by a player
	public void selected(){
		// Turn on the selected indicator
		GP.i.selectIconEnabled(gameObject, true);
	}
	
	// This human is no longer selected by a player
	public void deselected(){
		// Turn off the selected indicator
		GP.i.selectIconEnabled(gameObject, false);
	}
	
	public void commandInteractable(interactable interact,commandable.Mode mode){
		commandEmpty(interact.obj.transform.position,mode); // We first move to it
		this.interact = interact; // We set this as our interact
	}
	
	// Default is move
	public void commandEmpty(Vector2 clickedPos,commandable.Mode mode){
		this.mode = mode;
		hasNavGoal = true;
		navGoal = clickedPos;
		this.interact = null;
	}
	
	// Get the human's movement speed
	public float getSpeed(){
		return walkSpeed * 
			(mode == commandable.Mode.Sprint ? sprintMod : 1) * 
			(mode == commandable.Mode.Sneak ? 1.0f/sprintMod : 1); // If we are sprinting, multiply by sprint mod
	}
	
	public bool hasTag(string tag){
		if(tag == "sit") {return true;} // Humans can be commanded to sit in a chair
		return false;
	}
	
	public void Update(){
		if(isDeactive){return;}
		if(hasNavGoal){ // If we are currently navigating towards a position
			
			Vector2 relativePos = (navGoal - (Vector2) transform.position);
			float distance = relativePos.magnitude;
			float speed = getSpeed();
			if(distance < movePrecision || (distance < interactPrecision && interact != null)){
				// Stop moving
				mode = commandable.Mode.Normal;
				rb.drag = activeSlow;
				hasNavGoal = false;
				if(interact != null){
					interact.interact(this); // Do the interaction
					interact = null;
				}
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
		isDeactive = false;
		MouseManager.track(this);
		mode = commandable.Mode.Normal;
		
		BroadcastMessage("enable",null,SendMessageOptions.DontRequireReceiver); // Wake ourselves up
	}
	
	public void OnSit(Seat seat){
		deselected(); // We deselect ourselves.
		MouseManager.untrack(this); // We can no longer be commanded
		isDeactive = true;
		
		BroadcastMessage("disable",null,SendMessageOptions.DontRequireReceiver); 
		
		// Collider
		Collider2D[] colliders = GetComponents<Collider2D>();
		foreach (Collider2D col in colliders) {col.enabled = false;}
		mode = commandable.Mode.Normal;
	}
	
	public void OnDesit(Seat seat){
		MouseManager.track(this); // We can no longer be commanded
		isDeactive = false;
		
		BroadcastMessage("enable",null,SendMessageOptions.DontRequireReceiver); 
		
		// Re-enable colliders
		Collider2D[] colliders = GetComponents<Collider2D>();
		foreach (Collider2D col in colliders) {col.enabled = true;}
		mode = commandable.Mode.Normal;
	}
}
