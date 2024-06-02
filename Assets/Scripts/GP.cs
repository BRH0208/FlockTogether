using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This singleton "Global Properties "class can be used to set static states of the world and manage static variables.
// Because this is an instance, we can modify these values easily in the editor
// This also includes generic helper functions where a full class would add unwanted complexity 
// This class is an exeption to normal naming conventions in order to make "GI.i." a short syntax for accessing global variables 
public class GP : MonoBehaviour
{
	public static GP i; // Instance of global properties 
    // Start is called before the first frame update
    void Awake() // Happends before start so we must do on awake. In theory this should never be asleep
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
	
	// Used by seat to check if the player can fit somewhere. 
	// Should include players and walls but not zombies. 
	public ContactFilter2D physicsBlock; 

	// Used in Vehicle Manger. Inludes all colors that it is reasonable for a car to have
	public List<Color> carPaint;
	/*
		World State variables
		Variables that are part of the state of the world. 
	*/
	
	// Used in ZombieManager. This is the start of the infection, the base model from which all zombies are derived
	public GameObject zombiePrefab; 
	
	// Used in various, finds the child object called "SelectIcon" and turns off its sprite renderer
	// Part of the visual effect for selecting an object.
	public void selectIconEnabled(GameObject obj, bool enable) {
		GameObject selectObj = obj.transform.Find("SelectIcon").gameObject;
		if(selectObj == null){return;}
		Renderer selected = selectObj.GetComponent<SpriteRenderer>();
		selected.enabled = enable;
	}
	
	// Used in vehicle manger, a list of gameobject prefabs for various cars
	public GameObject[] cars;
	
	// The door prefab, used in loadHousetile.cs
	public GameObject doorPrefab;
	
	// Hashing Algorithm, okay for very cheap random numbers
	public uint wang_hash(float time)
	{
		unsafe {
			float* timePtr = &time;
			uint* timeIntPtr = (uint*) (timePtr); // I am sorry. 
			uint seed = *timeIntPtr;
			seed = (seed ^ 61) ^ (seed >> 16);
			seed *= 9;
			seed = seed ^ (seed >> 4);
			seed *= 0x27d4eb2d;
			seed = seed ^ (seed >> 15);
			return seed;
		}
	}
	
	// Used in housetile.cs
	// Should be .json
	public TextAsset housefile;
}
