using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanAI : MonoBehaviour, commandable
{
	
    private Vector2 navGoal;
	private bool hasNavGoal;
	
	private PlayerInteract interact;
	
	private float activeSlow = 200.0f;
	private float passiveSlow = 50.0f;
	private Rigidbody2D rb;
	private float walkSpeed = 4.2f;
	private float sprintMod = 2f;
	private float roatationRate = 450f;
	public bool isDeactive;
	public commandable.Mode mode;
	private float movePrecision = 0.025f;
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
	
	public Vector2 getPos(){
		return (Vector2) transform.position;
	}
	public void commandInteractable(PlayerInteract interact,commandable.Mode mode){
		commandEmpty(interact.gameObject.transform.position,mode); // We first move to it
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
		if(tag == "hands") {return true;} // Humans can be commanded to sit in a chair
		return false;
	}
	public void FixedUpdate(){
		if(hasNavGoal){
			Vector2 relativePos = (navGoal - (Vector2) transform.position);
			relativePos = relativePos / relativePos.magnitude;
			rb.drag = passiveSlow;
			rb.AddForce(relativePos * getSpeed(),ForceMode2D.Force);
		}
	}
	public void Update(){
		if(isDeactive){return;}
		if(hasNavGoal){ // If we are currently navigating towards a position
			
			Vector2 relativePos = (navGoal - (Vector2) transform.position);
			float distance = relativePos.magnitude;
			if(interact != null){
				Collider col = interact.gameObject.GetComponent<Collider>();
				if(col != null){
					Vector2 closestPoint = (Vector2) col.ClosestPoint(transform.position);
					distance = (closestPoint - (Vector2) transform.position).magnitude;
				}
			}
			float speed = getSpeed();
			if(distance < movePrecision){
				// Stop moving
				mode = commandable.Mode.Normal;
				rb.drag = activeSlow;
				hasNavGoal = false;
				if(interact != null){
					interact.inter.interact(this); // Do the interaction
					interact = null;
				}
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
