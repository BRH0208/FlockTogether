using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// This tile ONLY has zombies. no hitboxes or anything else
// init and getPos are handled by load empty
public class loadZombietile : loadEmpty
{
	private const int zombieCount = 10;
	private List<ZombieAI> zombies;
	bool didAwake;
	bool didGenerate;
	ZombieManager zmanager;
	private class JsonData
	{
	  public int seed { get; set; }
	  public bool didGenerate { get; set; }
	}
	
	public override void init(Vector2Int pos) {
		didAwake = false;
		didGenerate = false;
		zombies = new List<ZombieAI>();
		base.init(pos);
	}
	
	public override bool modified(){
		return didAwake;
	}
	
    // We activate our bound zombies
	public override void activate(){
		// Safety checks
		if(didAwake) {return;}
		if(!didGenerate){ return; }
		didAwake = true;
		foreach (ZombieAI zombie in zombies) {
			zombie.wake();
		}
		base.activate();
	}
	// We generate by placing our zombies
	public override void generate(int seed){
		if(didGenerate){ return; }
		zmanager = ZombieManager.instance;
		didGenerate = true;
		for (int i = 0; i < zombieCount; i++){
			GameObject zombie = zmanager.spawnZombie(pos.x+Random.value,pos.y+Random.value);
			zombies.Add(zombie.GetComponent<ZombieAI>());
			Debug.Log(zombies.Count);
		}
		base.generate(seed);
	}
	
	// We load the seed, then call generate. This works for some basic tiles. 
	public override void load(string json){
		JsonData data = JsonUtility.FromJson<JsonData>(json);
		if(!data.didGenerate){
			generate(data.seed);
		}
	}

	// We store the seed. 
	public override string stash(){
		JsonData data = new JsonData();
		data.seed = this.seed;
		data.didGenerate = this.didGenerate;
		return JsonUtility.ToJson(data);
	}
}
