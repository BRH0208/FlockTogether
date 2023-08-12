using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif


// Heavily based on https://docs.unity3d.com/Manual/Tilemap-ScriptableTiles-Example.html
public class Housetile : Tile
{
	public Sprite[] homeSprites;
	public TileBase roadTile;
	public Sprite grassSprite;
	// These are all the unique ways a house could be rotated/flipped
	// This would be marked as constant, but Quaternion.Euler must be computed
	// In addition, if these are changed the unity editor must be restarted for the changes
	// to take effect
	// TODO: Make constant by hardcoding the quaterneons.
	Quaternion[] rotations = {
		Quaternion.Euler(0,	0,		0.0f),
		Quaternion.Euler(0,	0,		90.0f),
		Quaternion.Euler(0,	0,		180.0f),
		Quaternion.Euler(0,	0,		270.0f),
		Quaternion.Euler(0,	180.0f,	00.0f),
		Quaternion.Euler(180.0f,	0,	90.0f),
		Quaternion.Euler(0,	180.0f,	180.0f),
		Quaternion.Euler(180.0f,	0,	270.0f),
		Quaternion.Euler(90f,0f,0.0f) // invalid, to make clear there was an issue
	};
	
    // Not all will be valid for every map location based on surrounding roads
	
	// Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
	
#if UNITY_EDITOR
	// The following is a helper that adds a menu item to create a Housetile Asset
    [MenuItem("Assets/Create/Housetile")]
    public static void CreateRoadTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save House Tile", "New House Tile", "Asset", "Save House Tile", "Assets");
        if (path == "")
            return;
		AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Housetile>(), path);
    }
#endif
	public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
		// Only we get to set our transform
		tileData.flags = TileFlags.LockTransform;
        
		// TODO: Implement collision
		tileData.colliderType = ColliderType.None;
		
        // We are never colored(it would reveal the grid to badly)
		tileData.color = Color.white;
		
		// Pick a house sprite
		// This has no change on worldgen. It is still defined by noise as to not change unexpectedly
		float spritePerlin = RuleTile.GetPerlinValue(location,0.99f,0.5f);
		spritePerlin = Mathf.Clamp(spritePerlin,0.0f,1.0f);
		
		int spriteNumber = (int) (spritePerlin * (homeSprites.Count()-1));
		tileData.sprite = homeSprites[spriteNumber];
		
		// Determine what rotations are valid
		List<int> validRotationIndexes = new List<int>();
		// There must be a road in that direction, and that road must have a conneciton in that direction(for end pieces and curved pieces)
		if(roadTile == tilemap.GetTile(location + Vector3Int.down)){
			if(roadTile == tilemap.GetTile(location + Vector3Int.down + Vector3Int.left)){
				validRotationIndexes.Add(0);
			}else if(roadTile == tilemap.GetTile(location + Vector3Int.down + Vector3Int.right)){
				validRotationIndexes.Add(4);
			}
		}
		if (roadTile == tilemap.GetTile(location + Vector3Int.up)) {
			if(roadTile == tilemap.GetTile(location + Vector3Int.up + Vector3Int.right)){
				validRotationIndexes.Add(2);
			}else if(roadTile == tilemap.GetTile(location + Vector3Int.up + Vector3Int.left)){
				validRotationIndexes.Add(6);
			}
		}
		if (roadTile == tilemap.GetTile(location + Vector3Int.right)) {
			if(roadTile == tilemap.GetTile(location + Vector3Int.right + Vector3Int.down)){
				validRotationIndexes.Add(1);
			}else if(roadTile == tilemap.GetTile(location + Vector3Int.right + Vector3Int.up)){
				validRotationIndexes.Add(5);
			}
		}
		if (roadTile == tilemap.GetTile(location + Vector3Int.left)) {
			if(roadTile == tilemap.GetTile(location + Vector3Int.left + Vector3Int.up)){
				validRotationIndexes.Add(3);
			}else if(roadTile == tilemap.GetTile(location + Vector3Int.left + Vector3Int.down)){
				validRotationIndexes.Add(7);
			}
		}
		
		if(validRotationIndexes.Count == 0){
			// If we don't know how to exist, pretend to be grass
			tileData.sprite = grassSprite;
			validRotationIndexes.Add(0);
		}
		
		// Rotate/flip
        Matrix4x4 newOrientation = tileData.transform;
		// We chose the index based on the tile's position, this is to make the driveway
		// position consistant
		float directionPerlin = RuleTile.GetPerlinValue(location,0.90f,0f);
		directionPerlin = Mathf.Clamp(directionPerlin,0.0f,1.0f);
		
		int staticNumber = (int) (directionPerlin * (validRotationIndexes.Count()-1));
		int chosenIndex = validRotationIndexes[staticNumber];
		
		newOrientation.SetTRS(Vector3.zero, rotations[chosenIndex], Vector3.one);
        tileData.transform = newOrientation;
		
	}
}