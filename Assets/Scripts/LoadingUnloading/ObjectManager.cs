using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;

// Any object can be taged with "preservable" by adding hte preservable component
// This component specifies a preservation system. This system will be used in loading/unloading
// and activating/deactivating the object as the situation calls for it.
public class ObjectManager : MonoBehaviour
{
	[Serializable]
	public class JsonData
	{
		public List<JsonObject> pickles; // Get it, because they are preservables?
	}
	[Serializable]
	public class JsonObject
	{
		public string label; // The label used to identify the preservable object which "owns" the json
		public string json; // The data to be loaded/unloaded to file
	}
	
	// Singleton pattern;
	public static ObjectManager instance;
	private Dictionary<(int,int),List<preservable>> objectLists;
	private List<preservationSystem> knownSystems; // For a system to unload, it must have been stashed first.
	
	public bool checkMovement(preservable instance,Vector2 oldPos, Vector2 newPos){
		Vector2Int oldPosInt = Vector2Int.FloorToInt(oldPos);
		Vector2Int newPosInt = Vector2Int.FloorToInt(newPos);
		if(newPosInt != oldPosInt){
			return trackObject(instance,oldPosInt,true) // Unload at old pos
			&& trackObject(instance,newPosInt,false); // Load at new pos
		}
		return true;
	}
		
	// Used to track or detrack an object. Best used on first creation, otherwise use checkmovement after creation.
	public bool trackObject(preservable instance, Vector2Int pos, bool untrack = false){
		(int,int) key = (pos.x,pos.y);
		if(!objectLists.ContainsKey(key)){
			objectLists[key] = new List<preservable>();
		}
		if(untrack){
			bool didRemove = objectLists[key].Remove(instance);
			if(!didRemove){
				Debug.LogError("ObjectManager failed to remove object at "+key+" with name \""+instance.name+"\"");
				return false;
			}
		} else {
			objectLists[key].Add(instance);
		}
		return true;
	}
	public bool hasEntityInTile(Vector2Int pos){
		(int,int) tuple = (pos.x,pos.y);
		return objectLists.ContainsKey(tuple) && objectLists[tuple].Count != 0;
	}
	
	// Take all the objects in a position and stash them, then send the stashed data to JSON. 
	public string stash(Vector2Int pos){
		JsonData data = new JsonData();
		data.pickles = new List<JsonObject>();
		foreach (preservable obj in objectLists[(pos.x,pos.y)]) {
			preservationSystem sys = obj.sys;
			JsonObject pickle = new JsonObject();
			pickle.label = sys.saveName;
			pickle.json = sys.stash(obj);
			if(knownSystems == null){
				knownSystems = new List<preservationSystem>();
			}
			if (!knownSystems.Contains(sys)){
				knownSystems.Add(sys); // Keep track of the new system.
			}
			data.pickles.Add(pickle);
		}
		bool didClear = objectLists.Remove((pos.x,pos.y));
		if(!didClear){ Debug.LogError ("ObjectManager failed to clean after stash");}
		return JsonUtility.ToJson(data);
	}
	
	private preservationSystem getSystem(string name){
		foreach (preservationSystem sys in knownSystems){
			if (sys.saveName == name){
				return sys;
			}
		}
		Debug.LogError("Failed to find system with name \""+name+"\"");
		return null;
	}
	// Load from stashed data
	public void load(String json){
		if(json == "{}"){return;} // Failsafe for empty.
		
		JsonData data = JsonUtility.FromJson<JsonData>(json);
		foreach (JsonObject pickle in data.pickles) {
			preservationSystem sys = getSystem(pickle.label);
			sys.load(pickle.json);
		}
	}
	
	public void activate(Vector2Int pos){
		if(!hasEntityInTile(pos)){ return; }
		
		foreach (preservable obj in objectLists[(pos.x,pos.y)]) {
			obj.sys.activate(obj); // Propogate activation signal
		}
		return;
	}
	
	public void deactivate(Vector2Int pos){
		if(!hasEntityInTile(pos)){ return; }
		
		foreach (preservable obj in objectLists[(pos.x,pos.y)]) {
			obj.sys.deactivate(obj);
		}
		return;
	}
	
    // Start is called before the first frame update
    void Start()
    {
		ObjectManager.instance = this; 
		objectLists = new Dictionary<(int,int),List<preservable>>();
    }
}
