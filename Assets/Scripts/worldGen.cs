using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class worldGen : MonoBehaviour
{
	// Public basic setup
	public GameObject GridObj;
	public TileBase roadTile;
	// Configuration variables
	public int maxX = 340; // Measured in tiles(32 feet)
	public int maxY = 340;
	public int seed; 
	
	/*
		We keep track of the current span of layers. 
		0 is always the default "land" layer. 
		1 is the first story(roads, entrances etc)
		2 is the 2nd floor.. etc
		-1 would be one story underground
		-2 would be two stories underground
	*/ 
	int minHeight = 0;
	int maxHeight = 0;
	Dictionary<int,GameObject> layers;
	
    // Called to begin world generation
    void Start()
    {	
		layers = new Dictionary<int,GameObject>();
		Random.InitState(1); // Start the randomizer
		
		// Manage layer 0
        GameObject LandLayer = GridObj.transform.Find("LandLayer").gameObject; // Find it
		layers.Add(0,LandLayer); // Store it
		landSetup setupScript = LandLayer.GetComponent<landSetup>(); // Find the script
		setupScript.Generate(maxX,maxY); // Generate The Land(slow process)
		
		// Go through all worldgen steps
		addLayer();
		createRoads(); // Start with roads
		assignPlots(); // Make building plots near roads and assign buildings to them
		createBuildings(); // Let all buildings place themselves in their plots
		createOtherTiles(); // Add all tiles not placed on roads
		bakeStatics(); // Bake in movement and collisions
		createDyanmics(); // Place dynamic object
		createUndead(); // Populate with initial undead
    }
	
	/* 
		Add a new tilemap layer to the game. 
		Each layer is about a floor in size.
		Parameters: 
			- top: true adds the layer to the top, and false for the bottom
	*/
	public void addLayer(bool top = true)
	{
		int layerNum;
		string layerName;
		
		// Update the edges
		if (top){
			maxHeight++;
			layerNum = maxHeight;
			layerName = "Layer #" + layerNum;
		}
		else{
			minHeight--;
			layerNum = minHeight;
			layerName = "Underground #" + (layerNum * -1);
		}
		
		// Add the actual gameobject
		GameObject layer = new GameObject(layerName);
		// Components
		layer.AddComponent<Tilemap>();
		layer.AddComponent<TilemapRenderer>();
		if(layerNum < 0){
			layer.GetComponent<TilemapRenderer>().sortingOrder = layerNum--;
		} else {
			layer.GetComponent<TilemapRenderer>().sortingOrder = layerNum;			
		}
		// Place it in layers and hirearchy
		layer.transform.parent = GridObj.transform;
		layers[layerNum] = layer;
	}
	
	// Place one interconnected road system on the island
	private void createRoads(){
		GameObject targetLayer = (GameObject) layers[1];
		Tilemap targetTilemap = targetLayer.GetComponent<Tilemap>();
		
		// Place the first road
		Vector3Int center = new Vector3Int(maxX/2,maxY/2,0);
		targetTilemap.SetTile(center,roadTile);
	}
	
	// Place designated building areas next to the roads
	private void assignPlots(){
		
	}
	
	// Place 
	private void createBuildings(){
		
	}
	
	private void createOtherTiles(){
		
	}
	
	private void bakeStatics(){
		
	}
	
	private void createDyanmics(){
		
	}
	
	private void createUndead(){
		
	}
}
