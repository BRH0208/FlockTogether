using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface commandable
{
	enum Mode {
		Normal, // Default 
		Sprint, // Usually activated with double tap, travel faster than normal at some cost
		Sneak, // Activated with slow tap, Travel slower but be seen from less distance
	}
	// Called on select and deselect respectively
	void selected();
	void deselected();

	// Get the commandable's position
	// Used for orientation. Should be centered, but exact position shouldn't matter
	Vector2 getPos();
	
	// Commandable entities should be able to respond to tags representing what they are
	// Tags include "Human", "Vehicle", "Inactive" // TODO: Make this an enum
	bool hasTag(string tag);
	
	// In a command, sprint implies we want to do this action quickly. 
	// It will have different effects depending on the commandable.
	void commandEmpty(Vector2 clickedPos, Mode mode); // We got a command without an associated interactable
	void commandInteractable(PlayerInteract interact, Mode mode); // We got a command to interact with interactable
	GameObject obj{get;}
}