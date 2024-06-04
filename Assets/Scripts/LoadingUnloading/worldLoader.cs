using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection; // TODO: I hate reflection
using System.IO;
using UnityEngine;

public class worldLoader : MonoBehaviour
{
	// Instance things
	Vector2Int pos;
	private bool doStart;
	public const string saveLoc = "active/";
	
	void OnDestroy(){
		instances.Remove(this);
	}
	
	public void deactivate(){
		load(pos,true);
		doStart = false;
	}
    // Start is called before the first frame update
    void Start()
    {
		needUpdate = false;
		doStart = false;
		instances.Add(this);
    }

	public void activate(){
		pos = (Vector2Int) Vector2Int.FloorToInt(gameObject.transform.position);
		doStart = true;
		needUpdate = true;
		load(pos,false);
	}
	void LateUpdate()
    {
        if(needUpdate){
			update();
		}
    }
    // Update is called once per frame
    void Update()
    {
		if(!doStart){return;}
		Vector2Int newPos = (Vector2Int) Vector2Int.FloorToInt(gameObject.transform.position);
		if(pos != newPos){
			load(pos,true);
			load(newPos,false);
			pos = newPos;
			needUpdate = true;
		}
    }
	
	// Static things
	private static bool debugMode = false;
	public const int activateRange = 2; // The range at which we activate. Circular. 
	public const int lightBoundry = 3; // The range at which we load,generate and de-activate. Circular
	public const int heavyBoundry = 3; // The range at which we unload to file. Square
	// This program will assume activateRange <= lightBoundry <= heavyBoundry
	// TODO: This assumption may be more strict, more testing is required.
	private class rangeCounter{
		public int activateCount;
		public int lightBoundry;
		public int heavyBoundry;
		public bool didSpawn; // Did the associated instance ever fall within the lightBoundry?
		
		public loadtile script;
		public bool isNew;
		public int activateCount_old;
		public int lightBoundry_old;
		public int heavyBoundry_old;
		
		public void updateOld(){
			activateCount_old = activateCount;
			lightBoundry_old = lightBoundry;
			heavyBoundry_old = heavyBoundry;
			isNew = false;
		}
		public rangeCounter(loadtile script){
			activateCount = 0; // TODO: Make this one line, less visual repetition
			lightBoundry = 0; // TODO: Rename this to use "count"
			heavyBoundry = 0;
			
			activateCount_old = 0;
			lightBoundry_old = 0;
			heavyBoundry_old = 0;
			
			isNew = true;
			didSpawn = false;
			this.script = script;
		}
		
	}
	
	public static List<worldLoader> instances = new List<worldLoader>();
	private static bool needUpdate;
	private static Dictionary<(int,int),rangeCounter> managedTiles = new Dictionary<(int,int),rangeCounter>(); 
	private static loadtile[] loadables = null;
	
	// Get if a tile at the given x,y coordinates is currently activating everything in it
	// used for dynamic spawning(like zombies)
	public static bool getActive(int x, int y){
		(int,int) pos = (x,y);
		if(!managedTiles.ContainsKey(pos)) {return false;}
		if(managedTiles[pos].activateCount > 0){return true;}
		return false;
	}
	// Todo, this fails the state-limiting idea as this info is also stored across _every instance of each loadable_
	private static Dictionary<string,loadtile> loadableDict = null;
	// The null is so we only construct it once 

	// TODO: This whole system is dumb. It has the bloat of a more expandable system,
	// but this case switch has to be manually updated with the identifer values
	// Bad! Bad! Bad!
	public static loadtile getLoader(Vector3Int pos){
		if(loadables == null){
			// First time setup of dict
			
			// Include all loadables in this list
			loadtile[] all_loadable = {new loadEmpty(), new loadZombietile(), new loadHousetile()};
			loadables = all_loadable;
			loadableDict = new Dictionary<string,loadtile>();
			foreach (loadtile phantom_loader in loadables) {
				foreach (string sprite_name in phantom_loader.spriteList()){
					loadableDict[sprite_name] = phantom_loader;
				}
			}
		}
		loadtile loader = new loadEmpty();
		
		// TODO: Get the primary layer a better way(GameObject.Find)
		Sprite sprite = worldGen.instance.layers[0].GetSprite(pos);
		if(sprite == null){return loader;} // Null tile(water) dealt with by loadEmpty
		if(loadableDict.ContainsKey(sprite.name)){
			loadtile desiredLoadable = loadableDict[sprite.name];
			loader = (loadtile)Activator.CreateInstance(desiredLoadable.GetType());
		}
		
		loader.init((Vector2Int) pos); 
		return loader;
	}
	
	public static void load(Vector2Int origin, bool unload){
		int val = 1; // By what value do we change the count by?
		if(unload){ val = -1;} // Unload decrements
		
		Vector2Int heavyOffset = Vector2Int.one * heavyBoundry; 
		BoundsInt heavyBounds = new BoundsInt((Vector3Int) (origin - heavyOffset), (Vector3Int) (2 * heavyOffset) + Vector3Int.one);
		
		foreach (Vector3Int pos3 in heavyBounds.allPositionsWithin)
        {
			(int,int) pos = (pos3.x,pos3.y);
			if(!managedTiles.ContainsKey(pos)) {
				if(unload) {
					Debug.LogError("Attempted to unload unknown chunk at "+pos3);
				}
				managedTiles[pos] = new rangeCounter(getLoader(pos3));
			}
			managedTiles[pos].heavyBoundry+=val;
			
			int dis = ((Vector2Int) pos3 - origin).sqrMagnitude;
			if (dis <= lightBoundry*lightBoundry) {
				managedTiles[pos].lightBoundry+=val;
				if (dis <= activateRange*activateRange) {
					managedTiles[pos].activateCount+=val;
				}
			}
        }
	}
	
	private static int seedGen(Vector2Int pos){
		return pos.x+worldGen.instance.maxX*pos.y+worldGen.instance.seed;
	}
	private static string fileLoc(Vector2Int pos){
		return saveLoc+"("+pos.x+","+pos.y+").sv";
	}
	[Serializable]
	public class JsonData
	{
		public string tileData;
		public string objData;
	}
	
	// Update does a lot, including file load/unloads
	public static void update(){
		ObjectManager objMan = ObjectManager.instance; // Shorthand
		needUpdate = false;
		
		if(managedTiles == null){
			managedTiles = new Dictionary<(int,int),rangeCounter>();
		}
		// TODO: If this is slow, it can be done in parallel
		List<(int,int)> deleteList = new List<(int,int)>();
		foreach (var item in managedTiles) {
			rangeCounter counter = item.Value;
			loadtile script = counter.script;
			Vector2Int pos = script.getPos();
			Vector3 pos3 = (Vector3) ((Vector3Int) pos); // TODO: REMOVE
			
			if(debugMode) {
				Debug.DrawLine(pos3,pos3+new Vector3(0.0f,1.0f,0.0f),Color.white);
				Debug.DrawLine(pos3+new Vector3(0.0f,1.0f,0.0f),pos3+new Vector3(1.0f,1.0f,0.0f),Color.white);
				Debug.DrawLine(pos3+new Vector3(1.0f,1.0f,0.0f),pos3+new Vector3(1.0f,0.0f,0.0f),Color.white);
				Debug.DrawLine(pos3+new Vector3(1.0f,0.0f,0.0f),pos3,Color.white);
			}
			
			if(counter.heavyBoundry == 0){
				// We remove the instance
				bool hasEntity = objMan.hasEntityInTile(pos);
				bool tileChanged = script.modified();
				if(tileChanged || hasEntity){
					JsonData json = new JsonData();
					
					if(hasEntity){
						json.objData = objMan.stash(pos);
					}else{
						json.objData = "{}";
					}
					
					if(tileChanged){
						json.tileData = script.stash();
					}else{
						json.tileData = "{}";
					}
					// Actually write to file
					using (StreamWriter writer = new StreamWriter(fileLoc(pos), false)){
						string data = JsonUtility.ToJson(json);
						writer.Write(data); // TODO: Batch these operations so we have less files
					}
				}
				if(debugMode) {
					Debug.DrawLine(pos3,pos3+new Vector3(1.0f,1.0f,0.0f),Color.red,0.5f);
					Debug.DrawLine(pos3+new Vector3(1.0f,0.0f,0.0f),pos3+new Vector3(0.0f,1.0f,0.0f),Color.red,0.5f);
				}
				script.destroy();
				deleteList.Add(item.Key);
				continue; 
			}
			if(counter.lightBoundry_old == 0 && counter.lightBoundry > 0 && counter.didSpawn == false){
				if(debugMode) {
					Debug.DrawLine(pos3,pos3+new Vector3(1.0f,1.0f,0.0f),Color.green,0.5f);
					Debug.DrawLine(pos3+new Vector3(1.0f,0.0f,0.0f),pos3+new Vector3(0.0f,1.0f,0.0f),Color.green,0.5f);
				}
				counter.didSpawn = true;
				
				// Check for a save file. If it exists, load it instead of generating.
				string filePath = fileLoc(pos);
				if(File.Exists(filePath)){
					string fileContents = File.ReadAllText(filePath);
					JsonData json = JsonUtility.FromJson<JsonData>(fileContents);
					
					// Object Manager load, if it wanted to when the file was saved.
					if(json.objData != "{}"){
						objMan.load(json.objData);
					}
					
					// Tile Load, if it wanted to when the file was saved
					if(json.tileData != "{}"){
						script.load(json.tileData);
					}else{
						script.generate(seedGen(pos));
					}
				} else {
					script.generate(seedGen(pos));
				}
			}else if(counter.lightBoundry_old > 0 && counter.lightBoundry == 0){
				script.deactivate();
				objMan.deactivate(pos);
			}
			if(counter.activateCount_old == 0 && counter.activateCount > 0){
				script.activate();
				objMan.activate(pos);
			}
			
			// Update the old values
			counter.updateOld();
		}
		// We remove all of the old entities. We do this now to not preturb the enumerable.
		foreach ((int,int) key in deleteList) {
			managedTiles.Remove(key);
		}
	}
}
