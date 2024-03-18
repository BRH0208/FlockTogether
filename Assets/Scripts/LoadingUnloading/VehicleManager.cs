using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleManager : MonoBehaviour, preservationSystem
{
	// Singleton pattern
	public static VehicleManager instance;
	
    // "Activate" the given preserved object. Any interaction with the enviroment or movement should only happend once activated
	public void activate(preservable obj){}
	
	// End the active state of the given object, inverse of activate
	public void deactivate(preservable obj){}
	
	// Called whent the object is saved to file. Any data to save should be returned. JSON is recomended
    public string stash(preservable obj){return "{}";}
	
	// Do we want to store anything?.
	public bool wantStash(){return true;}
	
	// Load the entity. The string that was returned when it was stashed is now returned.
    public void load(string data){}
	
	// The name for this section in the save file. Shorter names prefered for entities with high count.
	public string saveName{get{return "v";}}
	
	// The name to display for readability
	public string displayName{get{return "Vehicle";}}
	
	void Start()
    {
		VehicleManager.instance = this; 
	}
}
