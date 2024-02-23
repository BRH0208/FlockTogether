using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class worldLoader : MonoBehaviour
{
	// Instance things
	Vector2Int pos;
	private bool doStart;
	
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
	public const int activateRange = 1; // The range at which we activate. Circular. 
	public const int lightBoundry = 2; // The range at which we load,generate and de-activate. Circular
	public const int heavyBoundry = 3; // The range at which we unload to file. Square
	// This program will assume activateRange < lightBoundry < heavyBoundry
	private class rangeCounter{
		public int activateCount;
		public int lightBoundry;
		public int heavyBoundry;
		
		public loadable script;
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
		public rangeCounter(loadable script){
			activateCount = 0;
			lightBoundry = 0; // TODO: Rename this to use "count"
			heavyBoundry = 0;
			
			activateCount_old = 0;
			lightBoundry_old = 0;
			heavyBoundry_old = 0;
			
			isNew = true;
			this.script = script;
		}
		
	}
	
	public static List<worldLoader> instances = new List<worldLoader>();
	private static bool needUpdate;
	private static Dictionary<(int,int),rangeCounter> managedTiles = new Dictionary<(int,int),rangeCounter>(); 
	// The null is so we only construct it once 
	
	public static loadable getLoader(Vector3Int pos){
		loadable loader = new loadZombietile(); // TODO: Replace this
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
	
	public static void update(){
		needUpdate = false;
		ZombieManager zmanager = ZombieManager.instance;
		
		if(managedTiles == null){
			managedTiles = new Dictionary<(int,int),rangeCounter>();
		}
		// TODO: If this is slow, it can be done in parallel
		List<(int,int)> deleteList = new List<(int,int)>();
		foreach (var item in managedTiles) {
			rangeCounter counter = item.Value;
			loadable script = counter.script;
			Debug.DrawLine(new Vector3(item.Key.Item1,item.Key.Item2,1),new Vector3(item.Key.Item1+1,item.Key.Item2,1));
			Debug.DrawLine(new Vector3(item.Key.Item1,item.Key.Item2,1),new Vector3(item.Key.Item1,item.Key.Item2+1,1));
			Debug.DrawLine(new Vector3(item.Key.Item1+1,item.Key.Item2,1),new Vector3(item.Key.Item1+1,item.Key.Item2+1,1));
			Debug.DrawLine(new Vector3(item.Key.Item1,item.Key.Item2+1,1),new Vector3(item.Key.Item1+1,item.Key.Item2+1,1));
			
			if(counter.heavyBoundry == 0){
				// We remove the instance
				if(script.modified() || zmanager.hasEntityInTile(script.getPos())){
					script.stash();
				}
				deleteList.Add(item.Key);
				continue; 
			}
			if(counter.lightBoundry_old == 0 && counter.lightBoundry > 0){
				// Check for a file, because I don't have that part written yet, we just generate for now
				script.generate(0); // TODO: Add a seed generating algorithm
				Debug.Log("Running script " +script);
				Debug.DrawLine(new Vector3(item.Key.Item1,item.Key.Item2,1),new Vector3(item.Key.Item1+1,item.Key.Item2+1,1));
			
			}else if(counter.lightBoundry_old > 0 && counter.lightBoundry == 0){
				script.deactivate();
			}
			if(counter.activateCount_old == 0 && counter.activateCount > 0){
				script.activate();
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
