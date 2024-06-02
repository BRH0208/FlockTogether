using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;
// This class handles the loading of large and small houses
// Houses have static collisions, and state objects like doors. 
public class loadHousetile : loadtile
{
	private TextAsset housefile;
	private static JsonHousePrefix[] houseData;
	private static List<string> sprite_names;
	private static bool fileGenerated = false;
	private Vector2Int pos;
	private List<GameObject> managedObjects; // All of the STATIC objects associated with this house
	private List<Collider2D> dynamicColliders; // Colliders that turn on/off when we activate/deactivate 
		
	// Constructor
	public loadHousetile(){
		if(fileGenerated == false){
			housefile = GP.i.housefile; // TODO : This is dumb 
			houseData = JsonUtility.FromJson<fileArr>(housefile.text).arr;
			sprite_names = new List<string>();
			// Save all house files
			foreach (JsonHousePrefix pre in houseData) {
				foreach (JsonHouse house in pre.data) {
					foreach (string name in house.names) {
						sprite_names.Add(pre.prefix+name);
					}					
				}
			}
			Debug.Log("House configuration JSON file("+housefile.dataSize.ToString()+" bytes) loaded");
			fileGenerated = true;
		}
	}
	[Serializable]
	public class fileArr // Unity JsonUtility doesn't like JSON files that start as arrays, for some reason.
	{
	  public JsonHousePrefix[] arr;
	}
	[Serializable]
	public class JsonBox
	{
	  public int x;
	  public int y;
	  public int h;
	  public int w;
	}
	
	// Class used by JSON Utility
	[Serializable]
	public class JsonHouse
	{
	  public string[] names;
	  public JsonBox[] garagedoors;
	  public JsonBox[] doors;
	  public JsonBox[] walls;
	}
	
	// Class used by JSON Utility
	[Serializable]
	public class JsonHousePrefix
	{
	  public string prefix;
	  public JsonHouse[] data;
	}
	
	public override string[] spriteList(){
		return sprite_names.ToArray();
	}
	
	public override void init(Vector2Int pos){
		this.pos = pos;
		managedObjects = new List<GameObject>();
		dynamicColliders = new List<Collider2D>();
	}
	
	public override Vector2Int getPos(){
		return pos;
	}
	
	// Opening/Closing doors, destruction of walls, etc
	// Currently, no modification // TODO: implement modification
	public override bool modified(){return false;}
	
	// Turn on/off colliders.
	public override void activate(){
		foreach (Collider2D col in dynamicColliders) {
			col.enabled = true;
		}
	}
	public override void deactivate(){
		foreach (Collider2D col in dynamicColliders) {
			col.enabled = false;
		}
	}
	
	// Load the given data
	// TODO: this should be a more generic class that can handle advanced structures
	private void loadStructure(JsonHouse data){
		float halfPixel = 0.0078125f;
		// Manage walls
		// TODO: Make method
		GameObject coreObj = new GameObject();
		coreObj.name = "Obj"+pos;
		coreObj.layer = LayerMask.NameToLayer("OpaqueBlocker");
		coreObj.transform.position = ((Vector3) ((Vector3Int) pos)) + new Vector3(0.5f,0.5f,0.0f);
		managedObjects.Add(coreObj);
		foreach(JsonBox rect in data.walls){
			BoxCollider2D collider = coreObj.AddComponent<BoxCollider2D>();
			collider.offset = new Vector2(halfPixel * (rect.x*2+rect.w)-0.5f,halfPixel* ((64-rect.y)*2 - 2 + rect.h)-0.5f);
			collider.size = new Vector2(rect.w*halfPixel*2,rect.h*halfPixel*2);
			collider.enabled = false;
			dynamicColliders.Add(collider);
		}
		
		// Manage doors
		// TODO: Make method
		foreach (JsonBox rect in data.doors){
			
			// Position door
			GameObject doorObj = GameObject.Instantiate(GP.i.doorPrefab, ((Vector3) ((Vector3Int) pos)), Quaternion.identity);
			doorObj.transform.parent = coreObj.transform; 
			doorObj.transform.position += new Vector3(rect.x*2*halfPixel,(63-rect.y)*2*halfPixel,0.0f);
			if(rect.w == 3){
				doorObj.transform.position += new Vector3(-halfPixel,halfPixel,0.0f);
			}
			if(rect.h == 3){
				doorObj.transform.position += new Vector3(halfPixel,-halfPixel,0.0f);
				doorObj.transform.eulerAngles = new Vector3(0f,0f,90f);
			}
			// Find collider
			Transform colliderContainer = doorObj.transform.Find("Collider");
			if(colliderContainer == null){
				Debug.LogError("Cannot find collider gameobject for door at pos "+pos);
			}
			Collider2D col = colliderContainer.gameObject.GetComponent<Collider2D>();
			if(col == null){
				Debug.LogError("Cannot find collider component for door at pos "+pos);
			}
			col.enabled = false;
			
			// Save for later
			dynamicColliders.Add(col);
		}
		
		coreObj.transform.rotation = worldGen.instance.layers[0].GetTransformMatrix((Vector3Int) pos).rotation;	
		
	}
	
	// Load a tile for the first time
	public override void generate(int seed){
		Sprite houseSprite = worldGen.instance.layers[0].GetSprite((Vector3Int) pos); // TODO: Get a new pattern for accessing permanent gameobject instances.
		bool foundHouse = false;
		foreach(JsonHousePrefix prefixData in houseData){
			string prefix = prefixData.prefix;
			string name = houseSprite.name;
			if(name.StartsWith(prefix)){
				foreach (JsonHouse house in prefixData.data){
					foreach (string house_name in house.names){
						if(prefix + house_name == name){
							foundHouse = true;
							loadStructure(house);
							break;
						}
					}
					if(foundHouse){break;}
				}
				if(foundHouse){break;}
			}
		}
		if(foundHouse == false){
			Debug.LogError("Cannot find "+houseSprite.name+" in house json file");
		}
	}

	// Load this tile from a file. Notably, the seed is not provided
	// If the seed is needed, it should be saved to the file. 
	public override void load(string json){
		
	}

	
	// Store this tile in a file
	public override string stash(){
		foreach (GameObject obj in managedObjects) {
			UnityEngine.Object.Destroy(obj); // destroy the static objects
		}
		return "{}";
	}
}
