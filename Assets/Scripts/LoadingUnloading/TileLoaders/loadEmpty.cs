using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

// A simple class used for anything that just generates once.
// This class will still have normal calls to activate, deactivate and generate
// But after the first time it generates, it stashes that it should not generate again.
// Useful if it only spawns objects tracked by the ObjectManager
// Or for tiles in which nothing truely happends. 
public class loadEmpty : loadtile
{	
	private const string dummyJson = "{\"x\":\"x\"}"; // A bit of dummy json
	// We don't interact with whatever we save, but if we save nothing generate
	// will be called again on the assumption we didn't save because we didn't change
	
	// We never generate(loadEmpty generates, but it does not use the normal system)
	public override string[] spriteList(){
		return Array.Empty<string>();
	}
	
	// While we can't be modfied in any meaningful way,
	// We set this to true so we never generate twice
	public override bool modified(){return true;} 
	
	// We generate,activate,destroy and deactivate by doing nothing
	public override void generate(int seed){}
	public override void activate(){}
	public override void deactivate(){}
	public override void destroy(){}
	
	// We will NEVER actually load
	// Included is simple error checking
	public override void load(string json){
		if(json != dummyJson){
			Debug.LogError("Empty Tile Saved Improperly");
		}
	}
	
	public override string stash(){
		return dummyJson;
	}
}
