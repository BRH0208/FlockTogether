using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAI : MonoBehaviour, preservable
{
	public Seat driverSeat;
	public Seat[] seats; 
	private VehicleActor[] actors;
	public Color savedColor;
	public int type;
	private float dumpTime = 0.0f;
	private bool _commandable; // Cashed state
	public bool commandable{
		get{return _commandable;}
		set{
			if(value != commandable){
				foreach(VehicleActor actor in actors){
					actor.commandSwitch(value);
				}
				_commandable = value;
			}
		}
	}
	
	public List<GameObject> paintedObjects;
	
	void Update(){
		if(dumpTime > 0.0f){
			dumpTime -= Time.deltaTime;
			foreach (Seat seat in seats){
				seat.unsit();
			}
		} else {
			dumpTime = 0.0f;
		}
	}
	void Start()
    { 
        actors = GetComponentsInChildren<VehicleActor>();
		seats = GetComponentsInChildren<Seat>();
		foreach (Seat seat in seats){
			seat.messageList.Add(gameObject);
		}
		foreach (VehicleActor actor in actors){
			actor.parent = this;
		}
		
		_commandable = true; // We pretend we have been commandable
		commandableUpdate(); // This update corrects the car
		// TODO: This assumes the car does not start occupied.
		
    }
	
	// The system we use to preserve ourselves
	public preservationSystem sys{get{return VehicleManager.instance;}}
	public GameObject obj{get{return gameObject;}} // Our vehicle
	
	// Returns true if the current car is capable of movement
	public bool canMove(){
		return driverSeat.occupied; 
	}
	
	// Called when someone sits or leaves one of our chairs.
	public void seatUpdate(bool enter){
		commandableUpdate();
	}
	
	// Tell all seats to dump their passengers
	// Operates in multiple passes with a small delay to account for empty space
	public void dumpPassengers(){
		dumpTime = 2.0f;
	}
	
	public void commandableUpdate(){
		foreach (Seat seat in seats){
			if(seat.occupied){
				commandable = true;
				return;
			}
		}
		commandable = false;
	}
	// Paints this vehicle with the chosen color
	// Updates the sprite color of every gameobject in the "paintedObjects" list
	public void recolor(Color color)
	{
		savedColor = color;
		foreach (GameObject obj in paintedObjects){
			SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
			if(renderer == null){
				Debug.LogError("Vehicle painted objects includes object without a sprite");
			} else {
				color.a = 1.0f; // no invisible paint
				renderer.color = color;
			}
		}
	}
}
