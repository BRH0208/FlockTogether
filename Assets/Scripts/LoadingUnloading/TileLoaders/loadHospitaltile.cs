using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using Random=UnityEngine.Random;

// Load hospital structures
public class loadHospitaltile : loadtile 
{
	
	private GameObject obj; // The primary object
	private static string[] sprite_names; // TODO: Make name scheme consistent
	private List<Collider2D> colliders; // All associated colliders
	const float offsetDis = 0.3515625f; // The distance from body to center of walls, 21.5 pixels
	const float width = 0.015625f; // Width of collider, 1 pixel
	const float sideHalf = 0.0703125f; // Half of the length from the end of the collider to the end of the tile
	public loadHospitaltile(){
		if(sprite_names != null){return;}
		int min = 0;
		int max = 14; // The highest number in the sprite sheet
		string prefix = "HospitalSpritesheet_";
		sprite_names = new string[max-min+1];
		for (int i = min; i <= max; i++){
			sprite_names[i] = prefix + i;
		}
	}
	public override string[] spriteList(){
		return sprite_names;
	}
	
	public override void init(Vector2Int pos){
		colliders = new List<Collider2D>();
		base.init(pos);
	}
	public override void generate(int seed){
		// Place walls
		Tilemap primaryLayer = worldGen.instance.layers[0];
		Vector2 pos = getPos();
		
		obj = new GameObject();
		obj.name = "Obj"+pos;
		obj.layer = LayerMask.NameToLayer("OpaqueBlocker");
		obj.transform.position = ((Vector3) pos) + new Vector3(0.5f,0.5f,0.0f);
		
		TileBase selfTile = primaryLayer.GetTile(new Vector3Int((int) pos.x,(int) pos.y,0));		
		Vector2Int rotation = Vector2Int.up;
		for (int rotID = 0; rotID < 4; rotID++){
			Vector2Int rotLeft = worldGen.rotateVector2Int(rotation);
			float leftShift = 0.0f; // TODO: Edit initial shift so that there is no overlap
			float sizeShift = 0.0f;
			if(primaryLayer.GetTile(Vector3Int.FloorToInt((Vector3) (pos + rotLeft))) == selfTile){
				leftShift -= sideHalf * 0.5f;
				sizeShift += sideHalf;
			}
			if(primaryLayer.GetTile(Vector3Int.FloorToInt((Vector3) (pos - rotLeft))) == selfTile){
				leftShift += sideHalf * 0.5f;
				sizeShift += sideHalf;
			}
			if(primaryLayer.GetTile(Vector3Int.FloorToInt((Vector3) (pos + rotation))) != selfTile){
				BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
				collider.offset = new Vector2(rotation.x * offsetDis + rotLeft.x*leftShift, rotation.y * offsetDis+ rotLeft.y*leftShift);
				collider.size = new Vector2((rotation.x == 0 ? 1.0f + sizeShift : width),
											(rotation.y == 0 ? 1.0f + sizeShift : width));
				collider.enabled = false;
				colliders.Add(collider);
			}
			rotation = rotLeft; // rotation
		}
	}
	
	public override string stash(){
		return "{}";
	}
	
	public override bool modified(){
		return false;
	}
	
	public override void load(string test){
	}
	
	public override void activate(){
		foreach (Collider2D col in colliders) {
			col.enabled = true;
		}
	}
	public override void deactivate(){
		foreach (Collider2D col in colliders) {
			col.enabled = false;
		}
	}
	
	public override void destroy(){
		// Destroy the object
		if(obj != null) { // Its possible we were never generated. If so, we have nothing to destroy
			UnityEngine.Object.Destroy(obj);
		}
	}
}
