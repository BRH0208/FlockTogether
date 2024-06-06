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
	VehicleManager vmanager;
	// We generate by placing our zombies
	public override void generate(int seed){
		vmanager = VehicleManager.instance;
		Random.InitState(seed); // Set a seed.
		for (int i = 0; i < spawnCount; i++){
			Vector2 pos = getPos();
			GameObject car = vmanager.spawnCar(
				pos.x+Random.value,pos.y+Random.value, // pos
				Random.Range(0,vmanager.carCount), // id
				Random.value*360.0f, // rotation
				vmanager.randomCarColor()); // color
		}
		base.generate(seed);
	}
}
