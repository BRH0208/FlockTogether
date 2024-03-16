using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This singleton "Global Properties "class can be used to set static states of the world and manage static variables.
// Because this is an instance, we can modify these values easily in the editor
// This class is an exeption to normal naming conventions in order to make "GI.i." a short syntax for accessing global variables 
public class GP : MonoBehaviour
{
	public static GP i; // Instance of global properties 
    // Start is called before the first frame update
    void Start()
    {
        i = this;
    }
	
	/*
		Effective Constants
		They are technically instance variables(for modification in testing) but should be treated as constants
	*/
	
	// Used in PlayerZombieAlert and Zombie AI this filter should block any physics2d raycast that would block a zombies vision
	// It should not include the target of the zombie's vision.
	public ContactFilter2D alertBlockFilter;
	
	/*
		World State variables
		Variables that are part of the state of the world. 
	*/
}
