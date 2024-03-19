using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seat : MonoBehaviour
{
	public const float checkDist = 0.01f;
	private RaycastHit2D[] results;
	public GameObject occupant;
	public Door exitDoor;
	public List<GameObject> messageList; // When we sit or unsit we inform this source with a send message
	public void Start(){
		results = new RaycastHit2D[1];
	}
	
    public bool occupied{get{return occupant != null;}}
	
	public void sit(GameObject person){
		// Move them to us
		person.transform.position = new Vector3(transform.position.x,transform.position.y,person.transform.position.z);
		
		// Rotate them towards us
		Vector3 personRotation = person.transform.rotation.eulerAngles;
		Vector3 ourRotation = transform.rotation.eulerAngles;
		person.transform.rotation = Quaternion.Euler(new Vector3(personRotation.x,personRotation.y,ourRotation.z));
		
		// Set occupant
		occupant = person;
		
		// We call the "OnSit" method to indicate that the player is now sitting
		person.BroadcastMessage("OnSit",this,SendMessageOptions.DontRequireReceiver);
		person.transform.parent = transform; // We are the parent now!
		
		// Propogate signal
		foreach (GameObject obj in messageList){
			obj.SendMessage("seatUpdate",true,SendMessageOptions.DontRequireReceiver);
		}
	}
	
	public bool unsit(){
		if(!occupied){return false;} // Only unsit if we have an occupant
		Vector3 newPos = exitDoor.transform.transform.position;
		Vector3 doorRotation = exitDoor.transform.rotation.eulerAngles;
		Vector3 personRotation = occupant.transform.rotation.eulerAngles;
		
		// Raycast for collisions
		// The red direction is used for "aiming" the door and should face out the vehicle
		int hits = Physics2D.Raycast((Vector2) newPos, exitDoor.transform.right, GP.i.physicsBlock, results, checkDist); 
		if(hits != 0){
			return false; // The position is blocked
		}
		
		// Move them to the door
		occupant.transform.position = new Vector3(newPos.x,newPos.y,occupant.transform.position.z);
		occupant.transform.rotation = Quaternion.Euler(new Vector3(personRotation.x,personRotation.y,doorRotation.z));
		
		// Batman them
		occupant.transform.parent = null;
		
		// We call the "OnDesit" method to indicate that the player is now sitting
		occupant.BroadcastMessage("OnDesit",this,SendMessageOptions.DontRequireReceiver);
		
		// Forget them
		occupant = null;
		
		// Propogate signal
		foreach (GameObject obj in messageList){
			obj.SendMessage("seatUpdate",false,SendMessageOptions.DontRequireReceiver);
		}
		return true;
	}
}
