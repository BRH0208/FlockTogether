using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleActor : MonoBehaviour, commandable
{
	public VehicleAI parent;
	// Call this method to turn on/off this vehicle's ability to be commanded.
	public void commandSwitch(bool state = true){
		// We tell all of our generic disablers to enable/disable themselves accordingly
		if(state == false){
			BroadcastMessage("disable",null,SendMessageOptions.DontRequireReceiver);
			deselected();
			MouseManager.untrack(this);
		} else {
			BroadcastMessage("enable",null,SendMessageOptions.DontRequireReceiver);
			MouseManager.track(this);
		}
	}
	
    // Called on select and deselect respectively
	public void selected(){
		GP.i.selectIconEnabled(gameObject, true);
	}
	public void deselected(){
		GP.i.selectIconEnabled(gameObject, false);
	}
	public Vector2 getPos(){
		return (Vector2) transform.position;
	}
	
	// Commandable entities should be able to respond to tags representing what they are
	public bool hasTag(string tag){
		if(tag == "Inactive"){return !parent.commandable;}
		if(tag == "Vehicle"){return parent.commandable;}
		return false;
	}
	
	public void commandEmpty(Vector2 clickedPos, commandable.Mode mode){
		// TODO: Implement vehicle movement
		parent.dumpPassengers();// todo temp
	}
	
	public void commandInteractable(PlayerInteract interact, commandable.Mode mode){ 
		// We don't understand how to interact, so we just move to the position
		commandEmpty((Vector2) interact.gameObject.transform.position,mode);
	} 
	
	public GameObject obj{get {return gameObject;}}
}
