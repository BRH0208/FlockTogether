using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class loadEmpty : loadtile
{	
	private class JsonData
	{
	  public int seed { get; set; }
	}
	
	
	public Vector2Int pos;
	public int seed;
	
	public virtual void init(Vector2Int pos){
		this.pos = pos;
	}
	
	// We can't be modfied in any meaningful way
	public virtual bool modified(){return false;} 
	
	public virtual Vector2Int getPos(){return pos;}
	
	// We have nothing to activate
	public virtual void activate(){}
	
	// We have nothing to deactivate
	public virtual void deactivate(){}
	
	// We generate by doing nothing
	public virtual void generate(int seed){
		this.seed = seed;
	}

	// We load the seed, then call generate. This works for some basic tiles. 
	public virtual void load(string json){
		JsonData data = JsonUtility.FromJson<JsonData>(json);
		generate(data.seed);
	}

	// We store the seed. 
	public virtual string stash(){
		JsonData data = new JsonData();
		data.seed = seed;
		return JsonUtility.ToJson(data);
	}
}
