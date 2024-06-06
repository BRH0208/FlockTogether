using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;
public class ZombieManager : MonoBehaviour, preservationSystem
{
	
	// Singleton pattern
	public static ZombieManager instance;
	
	
	public ulong zombieCount; // the fact this is a long makes me happy. 
	[Serializable]
	public class JsonObject
	{
		// Zombies don't store anything besides their position.
		public float x; 
		public float y; 
	}
	
	public GameObject spawnZombie(float x, float y, bool creation = true){
		if (creation){
			zombieCount++;
		}
		Quaternion rotation = Quaternion.Euler(0f,0f,Random.Range(0.0f,360.0f));
		GameObject zombieInstance = Instantiate(GP.i.zombiePrefab, new Vector3(x, y, 0), rotation,transform);
		Vector2Int position = Vector2Int.FloorToInt(new Vector2(x,y));
		preserveZombie zombiePreserve = zombieInstance.GetComponent<preserveZombie>();
		if(zombiePreserve == null){
			Debug.LogError("Zombie was created without having zombie preserve object");
		}
		ObjectManager.instance.trackObject(zombiePreserve,position);
		return zombieInstance;
		
	}
	// Take all the zombies in a position and remove them, then send the stored data to JSON. 
	public string stash(preservable instance){
		// Destroy the zombie
		Destroy(instance.obj); // We just kill it
		
		// Save the object
		JsonObject data = new JsonObject();
		data.x = instance.obj.transform.position.x;
		data.y = instance.obj.transform.position.y;
		return JsonUtility.ToJson(data);
	}
	
	public void activate(preservable instance){
		ZombieAI ai = instance.obj.GetComponent<ZombieAI>();
		if(ai == null){
			Debug.LogError("Cannot Activate at ("+instance.obj.transform.position.x+","+instance.obj.transform.position.y+"): ZombieManager preservable attached to gameObject without ZombieAI");
		} else {
			ai.wake();
		}
		return;
	}
	
	public void deactivate(preservable instance){
		ZombieAI ai = instance.obj.GetComponent<ZombieAI>();
		if(ai == null){
			Debug.LogError("Cannot Deactivate at ("+instance.obj.transform.position.x+","+instance.obj.transform.position.y+"): ZombieManager preservable attached to gameObject without ZombieAI");
		} else {
			ai.deactivate();
		}
		return;
	}
	
	
	public void load(string json){
		if(json == "{}"){return;} // Failsafe for empty.
		
		JsonObject data = JsonUtility.FromJson<JsonObject>(json);
		spawnZombie(data.x,data.y,false); // Spawn a zombie with the saved position
		// false is so that we don't double count the zombie.
	}
	
	public bool wantStash(){return true;} // We always want to be saved.
	
	public string saveName{get{return "z";}} // We are identified just by "z", we have a lot of zombies so we keep it short
	
	// The name to display for readability
	public string displayName{get{return "Zombie";}}
	
	void Awake()
    {
		ZombieManager.instance = this; 
	}
}