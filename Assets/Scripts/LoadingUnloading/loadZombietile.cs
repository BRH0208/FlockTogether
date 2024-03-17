using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;
// This tile ONLY has zombies. no hitboxes or anything else
// init and getPos are handled by load empty
public class loadZombietile : loadEmpty
{
	private const int zombieCount = 50;
	bool didAwake;
	bool didGenerate;
	ZombieManager zmanager;
	[Serializable]
	public class JsonData
	{
	  public int seed;
	  public bool didGenerate;
	}
	
	public override void init(Vector2Int pos) {
		didAwake = false;
		didGenerate = false;
		base.init(pos);
	}
	
	public override bool modified(){
		return didAwake;
	}
	
	public override void activate(){
		base.activate();
	}
	// We generate by placing our zombies
	public override void generate(int seed){
		if(didGenerate){ return; }
		zmanager = ZombieManager.instance;
		didGenerate = true;
		Random.InitState(seed); // Set a seed.
		for (int i = 0; i < zombieCount; i++){
			GameObject zombie = zmanager.spawnZombie(pos.x+Random.value,pos.y+Random.value);
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
