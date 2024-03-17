using UnityEngine;

// A loadable entity exists for every tile in our loaded range. 
// At any one time, there should only be one loadable entity per tile
// However, if there are multiple 
public interface loadtile{
	// Ran once before any other method is called. 
	void init(Vector2Int pos);
	
	// Returns the position.
	// Assumed to agree with the x,y given to the init
	// Violating that assumption leads to unexpected behavior
	Vector2Int getPos();
	
	// Do we have anything to stash, or can we just regenerate?
	// Regenerating is prefered if at all possible. 
	bool modified();
	
	// Called to activate dyanmic objects
	// dynamic objects are objects that can move or be moved
    // "waking up" creatures like zombies that belong to this tile
	void activate();
	
	// Called to deactivate dynamic objects
	// If dynamic objects are within this tile, or tied to this tile, deactivate them
	// Importantly: If an object can leave tiles on its own, it should deactivate ITSELF
	void deactivate();
	
	// Load a tile for the first time
	void generate(int seed);

	// Load this tile from a file. Notably, the seed is not provided
	// If the seed is needed, it should be saved to the file. 
	void load(string json);

	
	// Store this tile in a file
	string stash();	
}