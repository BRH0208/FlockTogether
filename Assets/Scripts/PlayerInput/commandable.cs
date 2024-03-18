using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface commandable
{
	// Called on select and deselect respectively
	void selected();
	void deselected();
	
	// Commandable entities should be able to respond to tags representing what they are
	// Tags include "Human", "Vehicle" // TODO: This should be an enum
	bool hasTag(string tag);
	
	void commandEmpty(Vector2 clickedPos); // We got a command without an associated interactable
	void commandInteractable(interactable interact); // We got a command to interact with interactable
	GameObject obj{get;}
}
