using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Must be placed or updated after the ground tile it is placing over
// base sprites and replace sprites cannot be updated dynamically
public class replacerTile : Tile
{
	public Color offsetcolor; // This tile's color
	public Sprite[] baseTiles;
	public Sprite[] replacingTiles;
	public string baseLayerName;
	private Tilemap referenceMap; // The tilemap to look for the base sprite
    private TileData parentTile; // IMPORTANT: No recursive relationships!
	
	// Start is called before the first frame update
    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
		referenceMap = GameObject.Find(baseLayerName).GetComponent<Tilemap>();
		// Only we get to set our transform
		tileData.flags = TileFlags.LockTransform;
        
		// Collision is not done here
		tileData.colliderType = ColliderType.None;
		
		tileData.color = offsetcolor;
		
		bool foundSprite = false;
		// update sprite
		Sprite baseSprite = referenceMap.GetSprite(location);
		for (int spriteId = 0; spriteId < baseTiles.Length; spriteId++) {
			if (baseTiles[spriteId] == baseSprite){
				foundSprite = true;
				tileData.sprite = replacingTiles[spriteId % replacingTiles.Length];
			}
		}
		if(!foundSprite){
			Debug.LogError("Sprite "+baseSprite.name+" has no associated replaced sprite");
		}
		tileData.transform = referenceMap.GetTransformMatrix(location);
		
	}
	
	#if UNITY_EDITOR
	// The following is a helper that adds a menu item to create this Asset
    [MenuItem("Assets/Create/ReplacerTile")]
    public static void CreateReplacerTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Replacer Tile", "New Replacer Tile", "Asset", "Save Replacer Tile", "Assets");
        if (path == "")
            return;
		AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<replacerTile>(), path);
    }
	#endif
}
