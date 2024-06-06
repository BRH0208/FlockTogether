using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Literally just a container for the system this instance uses.
public interface preservable 
{
    public preservationSystem sys{get;} // The system to preserve the object with
	public GameObject obj{get;} // All preservables must have associated gameobjects associated with the preservation
	// If the object attached to the preservable does not match the system, unexpected behavior may occur. 
}
