using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;
// This tile ONLY has zombies. no hitboxes or anything else
// init and getPos are handled by load empty
public class loadZombietile : loadEmpty
{
	private const int zombieCount = 10;
	
	// We generate on blank grass tiles, but not beach tiles
	public override string[] spriteList(){
		string[] arr = {"IslandTileset_2"};
		return arr;
	}

	// We generate by placing our zombies
	public override void generate(int seed){
		Vector2 pos = getPos();
		ZombieManager zmanager = ZombieManager.instance;
		Random.InitState(seed); // Set a seed.
		for (int i = 0; i < zombieCount; i++){
			GameObject zombie = zmanager.spawnZombie(pos.x+Random.value,pos.y+Random.value);
		}
		base.generate(seed);
	}
}
