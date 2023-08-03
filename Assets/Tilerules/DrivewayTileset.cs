using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class DrivewayTileset : RuleTile<DrivewayTileset.Neighbor> {
	public TileBase[] LargeRoad;
	public TileBase[] LargeDriveway;
	public TileBase[] SmallDriveway;
	
    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int LargeRoad = 3;
        public const int SmallDriveway = 4;
		public const int SmallDrivewayMirror = 5;
		public const int LargeDriveway = 6;
		public const int LargeDrivewayMirror = 7;
    }

	public override bool RuleMatches(RuleTile.TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform){
		// Get information
		Vector3 rotation = transform.rotation.eulerAngles;
		Dictionary<Vector3Int, int> neighbors = rule.GetNeighbors();
		
		// Deal with rotations
		int rotatedDir = (int) (rotation.z / 90f);
		bool isFlipped = (rotation.y == 180f); 
		
		// We look at each position
		foreach (var square in neighbors){
			int dir;
			if (square.Key == Vector3Int.up){
				dir = 2;
			} else if (square.Key == Vector3Int.left){
				dir = 1;
			} else if (square.Key == Vector3Int.right){
				dir = 3;
			} else if (square.Key == Vector3Int.down){
				dir = 0;
			} else {
				dir = -1;
			}
			bool result = RuleMatchDir(square.Value,position + square.Key,tilemap,dir,isFlipped);
			if (result == false){
				return false;
			}
		}
		return true; 
	}
	
    public bool RuleMatchDir(int neighbor, Vector3Int position, ITilemap tilemap, int checkDir, bool isMirror) {
        TileBase tile = tilemap.GetTile(position);
		switch (neighbor) {
            case Neighbor.LargeRoad: 			return LargeRoad.Contains(tile);
            case Neighbor.LargeDriveway: 		return compareWithDir(LargeDriveway,tile,position,tilemap, checkDir, isMirror);
			case Neighbor.SmallDriveway: 		return compareWithDir(SmallDriveway,tile,position,tilemap, checkDir, isMirror);
			case Neighbor.LargeDrivewayMirror: 		return compareWithDir(SmallDriveway,tile,position,tilemap, checkDir, isMirror, true);
			case Neighbor.SmallDrivewayMirror: 		return compareWithDir(SmallDriveway,tile,position,tilemap, checkDir, isMirror, true);
        }
        return base.RuleMatch(neighbor, tile);
    }
	
	// This comparison is for if a specific kind of house fits needs
	private bool compareWithDir(TileBase[] allowedTiles,	// The tileset we are checking with
								TileBase tile, 				// The tile we are comparing to
								Vector3Int position,		// The position we are checking
								ITilemap tilemap,  // The tilemap we are checking on
								int checkDir,				// The absolute direction we are checking in. 0 is down, 1 is right and so on
								bool selfMirror,			// If we are checking with a mirror
								bool mirrorCheck = false) 	// If we are looking for a mirrored version of a house
	
	{
		// Diagonals we don't check because it would be meaningless
		if(checkDir == -1){
			return false;
		}
		// It must be in the tile set
		if (!allowedTiles.Contains(tile)){
			return false;
		}
		
		// Check location
		TileData dataBuffer = new TileData();
		tile.GetTileData(position,tilemap,ref dataBuffer);
		Vector3 rotation = dataBuffer.transform.rotation.eulerAngles;
		if (rotation.y == 180 ^ mirrorCheck ^ selfMirror){
			// If it is rotated, and we requite it to be rotated and we are rotated, it can not work
			return false;
		}
		switch (checkDir){
			case 0:
				return rotation.z == 180f;
			case 1:
				return rotation.z == 90f;
			case 2:
				return rotation.z == 0f;
			case 3:
				return rotation.z == 270f;
			
		}
		
		return true;
	}
}