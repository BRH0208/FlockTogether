using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;
// This tile ONLY has cars. no hitboxes or anything else
// init and getPos are handled by load empty
// Intended for debug use //TODO: Remove as use changes
public class loadCartile : loadEmpty
{
	public const int spawnCount = 2;
	bool didAwake;
	bool didGenerate;
	VehicleManager vmanager;
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
		vmanager = VehicleManager.instance;
		didGenerate = true;
		Random.InitState(seed); // Set a seed.
		for (int i = 0; i < spawnCount; i++){
			GameObject car = vmanager.spawnCar(
				pos.x+Random.value,pos.y+Random.value, // pos
				Random.Range(0,vmanager.carCount), // id
				Random.value*360.0f, // rotation
				vmanager.randomCarColor()); // color
		}
		base.generate(seed);
	}
	
	// We load the seed, then call generate. This works for some basic tiles. 
	public override void load(string json){
		JsonData data = JsonUtility.FromJson<JsonData>(json);
		if(!data.didGenerate){
			generate(data.seed);
		}else {
			this.didGenerate = data.didGenerate;
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
