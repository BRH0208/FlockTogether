using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : interactable
{
	public List<Seat> accessableSeats; // An ordered list of the seats. 
	// The first seat will take priority.
	
	void Start(){
		Transform parent = transform.parent;
		for (int i = 0; i < parent.childCount; i++) { // Implictly ordered by hirearchy order
			GameObject obj = parent.GetChild(i).gameObject;
			Seat seat = obj.GetComponent<Seat>();
			if(seat != null){
				accessableSeats.Add(seat);
			}
		}
		setInteract();
	}
    // This was interacted with
    public override bool interact(commandable entity){
		foreach (Seat seat in accessableSeats){
			if(!seat.occupied){
				seat.sit(entity.obj);
				return true;
			}
		}
		return false;
	}
	
	// TODO: Optimise this using events so its not checked every frame during travel.
	public bool haveSeat(){
		foreach (Seat seat in accessableSeats){
			if(!seat.occupied){return true;}
		}
		return false;
	}
	// Is the selected object capable of interacting with this object?
    public override bool canInteract(commandable entity){
		return entity.hasTag("sit") && haveSeat();
	}
	
	public override GameObject obj{get{return gameObject;}}
}
