using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	public int roadMin = 500;
	public int roadMax = 700;
	public int seed; 
	
	/*
		We keep track of the current span of layers. 
		0 is always the default "land" layer. 
		1 is the first story(roads, entrances etc)
		2 is the 2nd floor.. etc
		-1 would be one story underground
		-2 would be two stories underground
	*/ 
	Dictionary<int,Tilemap> layers;
	
	int minHeight = 0;
	int maxHeight = 0;
	
	// Populated during road generation 
	// denotes if a spot is a full tile that can support a road
	bool[,] emptyField;
	
	// The sprite for "empty land" found during road generation
	// Always the sprite at the center of the layer 0 tilemap
	Sprite openSprite;
	
    // Called to begin world generation
    void Start()
    {	
		layers = new Dictionary<int,Tilemap>();
		Random.InitState(1); // Start the randomizer
		
		// Manage layer 0
        GameObject LandLayer = GridObj.transform.Find("LandLayer").gameObject; // Find it
		layers.Add(0,LandLayer.GetComponent<Tilemap>()); // Store the tilemap(as reference)
		landSetup setupScript = LandLayer.GetComponent<landSetup>(); // Find the script
		setupScript.Generate(maxX,maxY); // Generate The Land(slow process)
		
		// We must create layer #1 for the roads and first floors manually. 
		// The rest are added by buildings themselves as needed
		addLayer(); 
		
		// Go through all worldgen steps
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
		layers[layerNum] = layer.GetComponent<Tilemap>();
	}
	
	// Place one interconnected road system on the island
	private void createRoads(){
		// Get the tilemaps
		Tilemap landLayer = (Tilemap) layers[0];
		Tilemap primaryLayer = (Tilemap) layers[1];
		
		// Place the first road
		Vector3Int center = new Vector3Int(maxX/2,maxY/2,0);
		primaryLayer.SetTile(center,roadTile);
		
		// We assume the sprite the road is placed on is the sprite that roads go on
		openSprite = landLayer.GetSprite(center);
		
		// The set of all tile positions we could place the next road in. Set is for speed an uniqueness
		HashSet<Vector2Int> regularExpansion = new HashSet<Vector2Int>();
		
		// Add first adjacent cells
		regularExpansion.Add(new Vector2Int(maxX/2,maxY/2 + 1));
		regularExpansion.Add(new Vector2Int(maxX/2,maxY/2 - 1));
		regularExpansion.Add(new Vector2Int(maxX/2 - 1,maxY/2));
		regularExpansion.Add(new Vector2Int(maxX/2 + 1,maxY/2));
		
		// A "priority" set that is drawn from more often than regularExpansion
		// Used to make more "straight" roads. 
		// No roads are prioritised at the start
		HashSet<Vector2Int> priorityExpansion = new HashSet<Vector2Int>();
		
		int roadCount = 0;
		int desiredRoads = Random.Range(roadMin,roadMax);
 		float priorityBias = 0.9f; // percent chance of using priority set
		
		// We loop until we hit our number of roads
		// or we run out of positions
		while(roadCount < desiredRoads && regularExpansion.Count != 0){
			// Check if we do priority, or normal
			HashSet<Vector2Int> possiblePositions;
			if(priorityExpansion.Count != 0 && Random.Range(0.0f,1.0f) < priorityBias){
				 possiblePositions = priorityExpansion;
			} else {
				 possiblePositions = regularExpansion;
			}
			 
			// Find a valid position to place in.
			bool foundPos = false;
			Vector2Int pos = new Vector2Int(-1,-1); // used as default, if a tile is placed here we have issues
			Vector3Int posVec3;
			while(!foundPos) {
				// If we don't have positions, if so we just stop
				if(possiblePositions.Count == 0){
					 break;
				}
				// Pick a position to try
				pos = possiblePositions.ElementAt(Random.Range(0,possiblePositions.Count));
				foundPos = false;
				posVec3 = new Vector3Int(pos.x,pos.y,0); // Tilemaps want 3d vectors
				 
				// We don't place roads that are already roads
				if(primaryLayer.GetTile(posVec3) == roadTile){
					possiblePositions.Remove(pos);
					continue; // try again immedietly
				}
				
				// Ignore positions that are not placeable sprites
				if(landLayer.GetSprite(posVec3) != openSprite){
					possiblePositions.Remove(pos);
					continue;
				}
				 
				// Ignore positions that create invalid tiles(2x2 grids)
				// Up and Left
				if(primaryLayer.GetTile(posVec3+Vector3Int.up) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.left) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.up+Vector3Int.left) == roadTile
				){
					possiblePositions.Remove(pos);
					continue; 
				}
				// Up and Right
				if(primaryLayer.GetTile(posVec3+Vector3Int.up) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.right) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.up+Vector3Int.right) == roadTile
				){
					possiblePositions.Remove(pos);
					continue; 
				}
				// Down and Left
				if(primaryLayer.GetTile(posVec3+Vector3Int.down) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.left) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.down+Vector3Int.left) == roadTile
				){
					possiblePositions.Remove(pos);
					continue; 
				}
				// Down and Right
				if(primaryLayer.GetTile(posVec3+Vector3Int.down) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.right) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.down+Vector3Int.right) == roadTile
				){
					possiblePositions.Remove(pos);
					continue; 
				}
				
				// If there is no problem with this position
				foundPos = true;	 
			}
			// If we could not find a valid position, we give up for now
			if(foundPos == false){
				 continue;
				 // We only continue here. We may not have a position just because we chose
				 // priority queue and the queue was full of invalid positions
			}
			
			// Place the tile
			posVec3 = new Vector3Int(pos.x,pos.y,0);
			primaryLayer.SetTile(posVec3,roadTile);
			roadCount++;
			
			// add the next tiles
			regularExpansion.Add(new Vector2Int(pos.x,pos.y + 1));
			regularExpansion.Add(new Vector2Int(pos.x,pos.y - 1));
			regularExpansion.Add(new Vector2Int(pos.x - 1,pos.y));
			regularExpansion.Add(new Vector2Int(pos.x + 1,pos.y));
			
			// Make them a priority if they are across from a road(prioritise lines)
			// Up/Down connecion
			if (primaryLayer.GetTile(posVec3 + Vector3Int.up) == roadTile) {// If we have Up
				priorityExpansion.Add(new Vector2Int(pos.x,pos.y - 1)); // We try down
			} else if(primaryLayer.GetTile(posVec3 + Vector3Int.down) == roadTile) { // If we have Down
				priorityExpansion.Add(new Vector2Int(pos.x,pos.y + 1)); // We try Up
			}
			
			// Make them a priority if they are across from a road(prioritise lines)
			// Left/Right connecion
			if (primaryLayer.GetTile(posVec3 + Vector3Int.right) == roadTile) {// If we have Right
				priorityExpansion.Add(new Vector2Int(pos.x - 1,pos.y)); // We try Left
			} else if(primaryLayer.GetTile(posVec3 + Vector3Int.left) == roadTile) { // If we have Left
				priorityExpansion.Add(new Vector2Int(pos.x + 1,pos.y)); // We try Right
			}
			
		}
		Debug.Log(roadCount + "/" + desiredRoads + " roads");
		
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
