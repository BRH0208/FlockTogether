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
		Dictionary<Vector3Int, int> neighbors = rule.GetNeighbors();
		
		bool isFlipped = false;
		int dir = 0;
		int subMatches = 0;
		while (true) {
			bool matchOutcome = RuleMatchFixed(neighbors, dir, isFlipped, tilemap, position);
			
			
			// Deal with rotations
			Dictionary<Vector3Int, int> newNeighbors = new Dictionary<Vector3Int, int>();
			// cases explained here : https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@4.0/api/UnityEngine.RuleTile.TilingRuleOutput.Transform.html
			
			switch (rule.m_RuleTransform) {
				case RuleTile.TilingRuleOutput.Transform.Fixed:
					// If we are not rotating, we end with just the fixed
					return matchOutcome;
				case RuleTile.TilingRuleOutput.Transform.Rotated: 
					// End cases
					if(matchOutcome == true) {
						// If we found a rotation which works
						Quaternion rotation = Quaternion.Euler(0,0,-90f * subMatches);
						transform = Matrix4x4.Rotate(rotation);
						return true;
					} else if(subMatches >= 3) {
						// If we didn't find a rotation, give up
						return false;
					}
					dir += 1;
					
					// Rotate the neighbors
					foreach (var square in neighbors) {
						Vector3Int newVec = Vector3Int.RoundToInt(Quaternion.AngleAxis(-90, Vector3.forward) * square.Key);
						newNeighbors.Add(newVec,square.Value);
					}
					break;
				case RuleTile.TilingRuleOutput.Transform.MirrorX:
					// End cases
					if(matchOutcome == true) {
						// If we found a rotation which works
						Quaternion rotation = Quaternion.Euler(180 * subMatches,0f,0f);
						transform = Matrix4x4.Rotate(rotation);
						return true;
					} else if(subMatches >= 1) {
						// If we didn't find a rotation, give up
						return false;
					}
					isFlipped = true;
					
					// Rotate the neighbors
					foreach (var square in neighbors) {
						Vector3Int newVec = Vector3Int.RoundToInt(Quaternion.AngleAxis(180, Vector3.right) * square.Key);
						newNeighbors.Add(newVec,square.Value);
					}
					break;
				case RuleTile.TilingRuleOutput.Transform.MirrorY:
					// End cases
					if(matchOutcome == true) {
						// If we found a rotation which works
						Quaternion rotation = Quaternion.Euler(0,180 * subMatches,0f);
						transform = Matrix4x4.Rotate(rotation);
						return true;
					} else if(subMatches >= 1) {
						// If we didn't find a rotation, give up
						return false;
					}
					isFlipped = true;
					
					// Rotate the neighbors
					foreach (var square in neighbors) {
						Vector3Int newVec = Vector3Int.RoundToInt(Quaternion.AngleAxis(180, Vector3.up) * square.Key);
						newNeighbors.Add(newVec,square.Value);
					}
					break;
				case RuleTile.TilingRuleOutput.Transform.MirrorXY:
					// End cases
					if(matchOutcome == true) {
						// If we found a rotation which works
						Quaternion rotation;
						if(subMatches == 2) {
							rotation = Quaternion.Euler(0,0,0f);
						} else {
							rotation = Quaternion.Euler(0,180 * subMatches,0f);
						}
						transform = Matrix4x4.Rotate(rotation);
						return true;
					} else if(subMatches >= 2) {
						// If we didn't find a rotation, give up
						return false;
					}
					isFlipped = true;
					
					// Flip the neighbors in X
					if (subMatches == 0) {
						foreach (var square in neighbors) {
							Vector3Int newVec = Vector3Int.RoundToInt(Quaternion.AngleAxis(180, Vector3.up) * square.Key);
							newNeighbors.Add(newVec,square.Value);
						}
					}else if (subMatches == 1) {
						foreach (var square in neighbors) {
							Vector3Int newVec = Vector3Int.RoundToInt(Quaternion.AngleAxis(180, Vector3.right) * square.Key);
							newNeighbors.Add(newVec,square.Value);
						}
					}
					break;
				case RuleTile.TilingRuleOutput.Transform.RotatedMirror:
					// End cases
					if(matchOutcome == true) {
						// If we found a rotation which works
						Quaternion rotation;
						if (isFlipped) {
							rotation = Quaternion.Euler(0,180,90f * subMatches + 180);
						} else {
							rotation = Quaternion.Euler(0,0,-90f * subMatches);
						}
						transform = Matrix4x4.Rotate(rotation);
						return true;
					} else if(subMatches >= 7) {
						// If we didn't find a rotation, give up
						return false;
					}
					dir = (dir + 1) % 4;
					if(subMatches == 3){
						// Flip the neighbors
						isFlipped = true;
						foreach (var square in neighbors) {
							Vector3Int newVec = Vector3Int.RoundToInt(Quaternion.AngleAxis(180, Vector3.up) * square.Key);
							newNeighbors.Add(newVec,square.Value);
						}
						neighbors = newNeighbors;
						newNeighbors = new Dictionary<Vector3Int, int>(); 
					}
					// Rotate the neighbors
					foreach (var square in neighbors) {
						Vector3Int newVec = Vector3Int.RoundToInt(Quaternion.AngleAxis(-90, Vector3.forward) * square.Key);
						newNeighbors.Add(newVec,square.Value);
					}
					break;
			}
			// Update modified neighbors
			neighbors = newNeighbors;
			subMatches++;
		}
	}
	
	public bool RuleMatchFixed(Dictionary<Vector3Int, int> neighbors, int dir, bool isFlipped,ITilemap tilemap, Vector3Int position){
		// Look over each position
		foreach (var square in neighbors){
			// Get the absolute direction from the relative
			// note: this probally doesn't work. 
			int localDir = 0;

			if (square.Key == Vector3Int.down){
				localDir += 0;
			}else if (square.Key == Vector3Int.right){
				localDir += 1;
			}else if (square.Key == Vector3Int.up){
				localDir += 2;
			}else if (square.Key == Vector3Int.left){
				localDir += 3; 
			}
			localDir = localDir % 4;
			
			// Do rule matching
			bool result = RuleMatchDir(square.Value,position + square.Key,tilemap,localDir,isFlipped);
			if (result == false){
				return false; // A neighbor doesn't match, so the whole rule fails
			}
		} 
		return true; // If all the neighbors pass, the rule succeeds
	}
    public bool RuleMatchDir(int neighbor, Vector3Int position, ITilemap tilemap, int checkDir, bool isMirror) {
        TileBase tile = tilemap.GetTile(position);
		switch (neighbor) {
			case 1:
				return tile == this;
            case Neighbor.LargeRoad: 			return LargeRoad.Contains(tile);
            case Neighbor.LargeDriveway: 		return compareWithDir(LargeDriveway,tile,position,tilemap, checkDir, isMirror);
			case Neighbor.SmallDriveway: 		return compareWithDir(SmallDriveway,tile,position,tilemap, checkDir, isMirror);
			case Neighbor.LargeDrivewayMirror: 		return compareWithDir(LargeDriveway,tile,position,tilemap, checkDir, isMirror, true);
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
		if(checkDir < 0  || checkDir > 3){
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
		
		// Flipping is a form of 180 rotation.
		float effectiveRotation = rotation.z;
		
		// Check rotation
		if(rotation.y != 180){
			switch (checkDir) {
				case 0:
					return rotation.z == 180;
				case 1: 
					return rotation.z == 270;
				case 2: 
					return rotation.z == 0;
				case 3: 
					return rotation.z == 90;
			}
		} else {
			switch (checkDir) {
				case 0:
					return rotation.z == 180;
				case 1: 
					return rotation.z == 90;
				case 2: 
					return rotation.z == 0;
				case 3: 
					return rotation.z == 270;
			}
		}
		// theoretically unreachable
		return false;
	}
}