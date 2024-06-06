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
	private static Jload_Prefix[] houseData;
	private static List<string> sprite_names;
	private List<buildingDoor> controlled_doors;
	private List<Collider2D> dynamicColliders;
	private static bool fileGenerated = false;
	private const float halfPixel = 0.0078125f;
	private int seed;
	private GameObject houseObj; 
	
	public class Jsave_file
	{
		public int seed;
		public string[] doors_json; 
	}
	// Constructor
	public loadHousetile(){
		if(fileGenerated == false){
			housefile = GP.i.housefile; // TODO : This is dumb 
			houseData = JsonUtility.FromJson<Jload_fileArr>(housefile.text).arr;
			sprite_names = new List<string>();
			// Save all house files
			foreach (Jload_Prefix pre in houseData) {
				foreach (Jload_House house in pre.data) {
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
	public class Jload_fileArr // Unity JsonUtility doesn't like JSON files that start as arrays, for some reason.
	{
	  public Jload_Prefix[] arr;
	}
	[Serializable]
	public class Jload_Box
	{
	  public int x;
	  public int y;
	  public int h;
	  public int w;
	}
	
	// Class used by JSON Utility
	[Serializable]
	public class Jload_House
	{
	  public string[] names;
	  public Jload_Box[] garagedoors;
	  public Jload_Box[] doors;
	  public Jload_Box[] walls;
	}
	
	// Class used by JSON Utility
	[Serializable]
	public class Jload_Prefix
	{
	  public string prefix;
	  public Jload_House[] data;
	}
	
	public override string[] spriteList(){
		return sprite_names.ToArray();
	}
	
	public override void init(Vector2Int pos){
		controlled_doors = new List<buildingDoor>();
		dynamicColliders = new List<Collider2D>();
		base.init(pos);
	}
	
	// Opening/Closing doors, destruction of walls, etc
	public override bool modified(){
		foreach(buildingDoor door in controlled_doors){
			if(door.isOpen || door.locked){
				return true;
			}
		}
		return false;
	}
	
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
	private GameObject loadWalls(Jload_House data){
		
		// Manage walls
		// TODO: Make method
		GameObject coreObj = new GameObject();
		Vector2 pos = getPos();
		coreObj.name = "Obj"+pos;
		coreObj.layer = LayerMask.NameToLayer("OpaqueBlocker");
		coreObj.transform.position = ((Vector3) pos) + new Vector3(0.5f,0.5f,0.0f);
		
		foreach(Jload_Box rect in data.walls){
			BoxCollider2D collider = coreObj.AddComponent<BoxCollider2D>();
			collider.offset = new Vector2(halfPixel * (rect.x*2+rect.w)-0.5f,halfPixel* ((64-rect.y)*2 - 2 + rect.h)-0.5f);
			collider.size = new Vector2(rect.w*halfPixel*2,rect.h*halfPixel*2);
			collider.enabled = false;
			dynamicColliders.Add(collider);
		}
		return coreObj;
	}
	private void loadDoors(Jload_House data, GameObject coreObj){
		// Manage doors
		// TODO: Make method
		foreach (Jload_Box rect in data.doors){
			
			// Position door
			// TODO: Why the fuck is this so convoluted!?!? 17 lines jesus
			GameObject doorObj = GameObject.Instantiate(GP.i.doorPrefab, ((Vector3) ((Vector3Int) getPos())), Quaternion.identity);
			buildingDoor script = doorObj.GetComponent<buildingDoor>();
			controlled_doors.Add(script);
			doorObj.transform.parent = coreObj.transform; 
			doorObj.transform.position += new Vector3(rect.x*2*halfPixel,(63-rect.y)*2*halfPixel,0.0f);
			if(rect.w == 3){
				doorObj.transform.position += new Vector3(-halfPixel,halfPixel,0.0f);
				if(Random.Range(0.0f,1.0f) > 0.5f){
					doorObj.transform.position += new Vector3(halfPixel * 2 * (rect.w+rect.h),0.0f,0.0f);
					doorObj.transform.eulerAngles = new Vector3(0f,0f,180f);
				}
			}
			if(rect.h == 3){
				doorObj.transform.position += new Vector3(halfPixel,-halfPixel,0.0f);
				doorObj.transform.eulerAngles = new Vector3(0f,0f,90f);
				if(Random.Range(0.0f,1.0f) > 0.5f){
					doorObj.transform.position += new Vector3(0.0f,halfPixel * 2 * (rect.w+rect.h),0.0f);
					doorObj.transform.eulerAngles = new Vector3(0f,0f,-90f);
				}
			}
			
			// Find collider
			Transform colliderContainer = doorObj.transform.Find("Collider");
			if(colliderContainer == null){
				Debug.LogError("Cannot find collider gameobject for door at pos "+getPos());
			}
			Collider2D col = colliderContainer.GetComponent<Collider2D>();
			if(col == null){
				Debug.LogError("Cannot find collider component for door at pos "+getPos());
			}
			dynamicColliders.Add(col);
			col.enabled = false;
		}
		
	}
	
	private Jload_House readHouseJson(){
		Sprite houseSprite = worldGen.instance.layers[0].GetSprite((Vector3Int) getPos()); // TODO: Get a new pattern for accessing permanent gameobject instances.
		string sprite_name = houseSprite.name;
		foreach(Jload_Prefix prefixData in houseData){
			string prefix = prefixData.prefix;
			if(sprite_name.StartsWith(prefix)){
				foreach (Jload_House house in prefixData.data){
					foreach (string house_name in house.names){
						if(prefix + house_name == sprite_name){
							return house;
						}
					}
				}
			}
		}
		Debug.LogError("Cannot find "+houseSprite.name+" in house json file");
		return null;
	}
	// Load a tile for the first time
	public override void generate(int seed){
		// Load file
		Jload_House house = readHouseJson();
		// Create associated objects
		this.seed = seed;
		Random.InitState(seed); // Set the seed.
		
		if(houseObj != null) {Debug.LogError("House defined before generating "+getPos());}
		houseObj = loadWalls(house);
		loadDoors(house, houseObj);
		
		// Transform the house base object
		houseObj.transform.rotation = worldGen.instance.layers[0].GetTransformMatrix((Vector3Int) getPos()).rotation;	
	}

	// Load this tile from a file. Notably, the seed is not provided
	// If the seed is needed, it should be saved to the file. 
	public override void load(string json){
		// Load files
		Jsave_file data = JsonUtility.FromJson<Jsave_file>(json);
		Jload_House house = readHouseJson();
		this.seed = data.seed; // Note, unused but included for consistency
		Random.InitState(seed);
		if(houseObj != null) {Debug.LogError("House defined before loading "+getPos());}
		houseObj = loadWalls(house);
		
		// Transform the house base object
		houseObj.transform.rotation = worldGen.instance.layers[0].GetTransformMatrix((Vector3Int) getPos()).rotation;
		
		// Manage doors
		foreach (string doorJstr in data.doors_json){
			GameObject doorObj = GameObject.Instantiate(GP.i.doorPrefab, ((Vector3) ((Vector3Int) getPos())), Quaternion.identity);
			doorObj.transform.parent = houseObj.transform;
			buildingDoor script = doorObj.GetComponent<buildingDoor>();
			script.loadJson(doorJstr);
		}
	}
	
	// Store this tile in a file
	public override string stash(){
		// Find and save relative information
		buildingDoor[] doorsScript = houseObj.GetComponentsInChildren<buildingDoor>(false);
		string[] doorsJson = new string[doorsScript.Length];
		for (int i = 0; i < doorsScript.Length; i++){
			doorsJson[i] = doorsScript[i].saveJson();
		}
		// Create the JSON
		Jsave_file data = new Jsave_file();
		data.seed = this.seed;
		data.doors_json = doorsJson;
		return JsonUtility.ToJson(data);
	}
	
	public override void destroy(){
		// Destroy the object
		if(houseObj != null) { // Its possible we were never generated. If so, we have nothing to destroy
			UnityEngine.Object.Destroy(houseObj);
		}
	}
}
