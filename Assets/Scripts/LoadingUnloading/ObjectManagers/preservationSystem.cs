using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface preservationSystem
{
	// "Activate" the given preserved object. Any interaction with the enviroment or movement should only happend once activated
	void activate(preservable obj);
	
	// End the active state of the given object, inverse of activate
	void deactivate(preservable obj);
	
	// Called whent the object is saved to file. Any data to save should be returned. JSON is recomended
    string stash(preservable obj);
	
	// Do we want to store anything?
	// For most objects, this is always true.
	bool wantStash();
	
	// Load the entity. The string that was returned when it was stashed is now returned.
    void load(string data);
	
	// The name for this section in the save file. Shorter names prefered for entities with high count.
	string saveName{get;}
	
	// The name to display for readability
	string displayName{get;}
}
