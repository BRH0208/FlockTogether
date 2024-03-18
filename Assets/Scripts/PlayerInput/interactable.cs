using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class interactable : MonoBehaviour
{
	// Should be called as part of start for all interactable objects
	public void setInteract(){
		PlayerInteract controller = GetComponent<PlayerInteract>();
		if(controller == null){
			Debug.LogError("interactable object needs PlayerInteract component");
		}
		if(controller.inter == null){
			controller.inter = this;
		}else{
			Debug.LogError("player controller already taken");
		}
	}
	
	// This was interacted with
	// Returns if the interaction was success
    public abstract bool interact(commandable entity);
	
	// Is the selected object capable of interacting with this object?
    public abstract bool canInteract(commandable entity);
	
	public abstract GameObject obj{get;}
}