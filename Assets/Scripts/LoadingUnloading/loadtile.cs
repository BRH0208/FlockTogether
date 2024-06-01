using UnityEngine;

// A loadable entity exists for every tile in our loaded range. 
// At any one time, there should only be one loadable entity per tile
// TODO: Loadtile should not exist, loading tiles should just read the relevant JSON and create the structures
// Atleast loadtile should include the neccesary structure loading code
public abstract class loadtile{
	// List of sprite names to use this loader with, the first matching loader will be used. 
	public abstract string[] spriteList();
	
	// Ran once before any other method is called. 
	public abstract void init(Vector2Int pos);
	
	// Returns the position.
	// Assumed to agree with the x,y given to the init
	// Violating that assumption leads to unexpected behavior
	public abstract Vector2Int getPos();
	
	// Do we have anything to stash, or can we just regenerate?
	// Regenerating is prefered if at all possible. 
	public abstract bool modified();
	
	// Called to activate dyanmic objects
	// dynamic objects are objects that can move or be moved
    // "waking up" creatures like zombies that belong to this tile
	public abstract void activate();
	
	// Called to deactivate dynamic objects
	// If dynamic objects are within this tile, or tied to this tile, deactivate them
	// Importantly: If an object can leave tiles on its own, it should deactivate ITSELF
	public abstract void deactivate();
	
	// Load a tile for the first time
	public abstract void generate(int seed);

	// Load this tile from a file. Notably, the seed is not provided
	// If the seed is needed, it should be saved to the file. 
	public abstract void load(string json);

	
	// Store this tile in a file
	public abstract string stash();	
}