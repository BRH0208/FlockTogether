using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class loadEmpty : loadtile
{	
	[Serializable]
	private class JsonData
	{
	  public int seed { get; set; }
	}
	
	
	public Vector2Int pos;
	public int seed;
	
	public override void init(Vector2Int pos){
		this.pos = pos;
	}
	
	// We never generate(loadEmpty generates, but it does not use the normal system)
	public override string[] spriteList(){
		return Array.Empty<string>();
	}
	
	// We can't be modfied in any meaningful way
	public override bool modified(){return false;} 
	
	public override Vector2Int getPos(){return pos;}
	
	// We have nothing to activate
	public override void activate(){}
	
	// We have nothing to deactivate
	public override void deactivate(){}
	
	// We generate by doing nothing
	public override void generate(int seed){
		this.seed = seed;
	}

	// We load the seed, then call generate. This works for some basic tiles. 
	public override void load(string json){
		JsonData data = JsonUtility.FromJson<JsonData>(json);
		generate(data.seed);
	}

	// We store the seed. 
	public override string stash(){
		JsonData data = new JsonData();
		data.seed = seed;
		return JsonUtility.ToJson(data);
	}
}
