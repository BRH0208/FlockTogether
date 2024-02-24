using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface preservable
{
	void activate(Vector2Int pos);
	
	void deactivate(Vector2Int pos);
	
	// The position to save
    string stash(Vector2Int pos);
	
	// Do we want to store anything?
	bool wantStash(Vector2Int pos);
	
	// The string to load
    void load(string json);
	
	// The name for this section in the save file. 
	string saveName();
}
