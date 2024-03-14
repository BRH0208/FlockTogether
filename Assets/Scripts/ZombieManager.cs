using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;
public class ZombieManager : MonoBehaviour, preservable
{
	[Serializable]
	public class JsonData
	{
		public List<JsonZombie> zombies;
	}
	[Serializable]
	public class JsonZombie
	{
		public JsonZombie(Vector2 pos) {
			x = pos.x;
			y = pos.y;
		}
		public float x;
		public float y;
	}
	
	// Singleton pattern;
	public static ZombieManager instance;
	
	public GameObject zombieObj; 
	private static Dictionary<(int,int),List<GameObject>> zombieLists;
	private ulong zombieCount; // the fact this is a long makes me happy. 
	
	public GameObject spawnZombie(float x, float y,bool creation = true){
		if (creation){
			zombieCount++;
		}
		Quaternion rotation = Quaternion.Euler(0f,0f,Random.Range(0.0f,360.0f));
		GameObject zombieInstance = Instantiate(zombieObj, new Vector3(x, y, 0), rotation,transform);
		Vector2Int position = Vector2Int.FloorToInt(new Vector2(x,y));
		trackZombie(zombieInstance,position);
		return zombieInstance;
	}
	
	public bool checkMovement(GameObject zombieInstance,Vector2 oldPos, Vector2 newPos){
		Vector2Int oldPosInt = Vector2Int.FloorToInt(oldPos);
		Vector2Int newPosInt = Vector2Int.FloorToInt(newPos);
		if(newPosInt != oldPosInt){
			return trackZombie(zombieInstance,oldPosInt,true) // Unload at old pos
			&& trackZombie(zombieInstance,newPosInt,false); // Load at new pos
		}
		return true;
	}
		
	private bool trackZombie(GameObject zombieInstance, Vector2Int pos, bool untrack = false){
		(int,int) key = (pos.x,pos.y);
		if(!zombieLists.ContainsKey(key)){
			zombieLists[key] = new List<GameObject>();
		}
		if(untrack){
			bool didRemove = zombieLists[key].Remove(zombieInstance);
			if(!didRemove){
				Debug.LogError("Fail to remove zombie at "+key);
				return false;
			}
		} else {
			zombieLists[key].Add(zombieInstance);
		}
		return true;
	}
	public bool hasEntityInTile(Vector2Int pos){
		(int,int) tuple = (pos.x,pos.y);
		return zombieLists.ContainsKey(tuple) && zombieLists[tuple].Count != 0;
	}
	public bool wantStash(Vector2Int pos) {
		return hasEntityInTile(pos);
	}
	public string saveName(){ return "ZMan"; } // TODO: This should be a property
	
	// Take all the zombies in a position and remove them, then send the stored data to JSON. 
	public string stash(Vector2Int pos){
		if(!hasEntityInTile(pos)){ return "{}"; }
		// For now, we don't do any clever zombie management. We store and load zombies positions, but nothing else about them
		JsonData data = new JsonData();
		data.zombies = new List<JsonZombie>();
		foreach (GameObject obj in zombieLists[(pos.x,pos.y)]) {
			Vector2 zombiePos = (Vector2) obj.transform.position;
			data.zombies.Add(new JsonZombie(zombiePos));
			Destroy(obj);
		}
		bool didClear = zombieLists.Remove((pos.x,pos.y));
		if(!didClear){ Debug.LogError ("Failed to clean after stash");}
		return JsonUtility.ToJson(data);
	}
	
	public void activate(Vector2Int pos){
		if(!hasEntityInTile(pos)){ return; }
		
		foreach (GameObject obj in zombieLists[(pos.x,pos.y)]) {
			ZombieAI ai = obj.GetComponent<ZombieAI>();
			ai.wake();
		}
		return;
	}
	
	public void deactivate(Vector2Int pos){
		if(!hasEntityInTile(pos)){ return; }
		
		foreach (GameObject obj in zombieLists[(pos.x,pos.y)]) {
			ZombieAI ai = obj.GetComponent<ZombieAI>();
			ai.deactivate();
		}
		return;
	}
	
	
	public void load(string json){
		if(json == "{}"){return;} // If we wrote this, then we didn't save anything
		JsonData data = JsonUtility.FromJson<JsonData>(json);
		foreach (JsonZombie zomb in data.zombies) {
			spawnZombie(zomb.x,zomb.y,false); // False so we don't double-count the zombies.
		}
	}
    // Start is called before the first frame update
    void Start()
    {
		ZombieManager.instance = this; 
        zombieCount = 0;
		zombieLists = new Dictionary<(int,int),List<GameObject>>();
    }
}
