using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using Random=UnityEngine.Random;

public class worldGen : MonoBehaviour
{
	// Singleton pattern;
	public static worldGen instance;
	
	// Public basic setup
	public GameObject GridObj;
	// Some things have to be scaled to the size of the map, this is done in worldgen
	public GameObject noise;
	public GameObject cameraObj;
	public GameObject minimap;
	public GameObject eventSystem;
	public GameObject oceanObj;
	 
	 // TODO: make private, replace references like this with GameObject.Find() by name;
	public TileBase roadTile;
	public TileBase landTile;
	public TileBase suburbTile;
	public TileBase parkingTile;
	public List<TileBase> houseTiles;
	public List<TileBase> houseTiles_i;
	
	public TileBase[] overlayTiles;
	
	// Configuration variables
	public int maxX = 3200; // Measured in tiles(64 feet)
	public int maxY = 3200;
	
	// Used only during land tile placement.
	// Larger is lower framerate, but faster
	private int chunkSize = 250;
	
	private float roadRatio = 0.03f;
	private float roadVariance = 0.01f;
	public int seed; 
	
	/*
	TODO: remove this, it is not accurate
		We keep track of the current span of layers. 
		0 is always the default "land" layer. 
		1 is the first story(roads, entrances etc)
		2 is the 2nd floor.. etc
		-1 would be one story underground
		-2 would be two stories underground
	*/ 
	public Dictionary<int,Tilemap> layers;
	
	// Regions
	public int[,] ownership; // Who owns which tile
	private class WorldgenRegion {
		public WorldgenRegion(int index) {
			this.index = index;
			this.size = 0;
			this.origin = new Vector2Int(-1,-1);
			this.didEdgeCheck = false;
			this.edges = new List<Vector2Int>();
			this.debugColor = new Color(
				Random.Range(0.0f,1.0f),
				Random.Range(0.0f,1.0f),
				Random.Range(0.0f,1.0f),
				1);
		}
		
		// the size of this region.
		public int size;
		
		// For debugging, we give each region a color randomly
		// Two regions might have the same(or simiar) colors but it should be distinquishable enough.
		public Color debugColor;
		
		// A simple value to make finding this region easier
		public Vector2Int origin;
		
		// The value of this region
		public int index;
		
		// The "edges" of the region
		// Edge positions are owned by this region
		// Edge positions are bordered cardinally by a position that isn't owned by this region
		// Can be none
		public List<Vector2Int> getEdges() {
			if (!didEdgeCheck){
				findRegionEdges();
			}
			return edges;
		}
		
		// We only want to do the check once
		// There may be no edges, so the count of the edges list is not a replacement for this flag
		private bool didEdgeCheck;
		
		// Used for storing the return of findRegionEdges.
		// To access the edges, use getEdges()
		private List<Vector2Int> edges;
		
		private bool findRegionEdges(){
			// Once we call this method, this should be true
			didEdgeCheck = true;
			
			if(size == 1){ // Edge case(pun intended)
				edges.Add(origin);
				return true;
			}
			
			// Get some values from our worldgen Instance
			worldGen gen = worldGen.instance;
			int[,] ownership = gen.ownership;
			int maxIterations = gen.maxX * gen.maxY + 1; //The surface area shouldn't be larger than the area
			
			// Find a valid spot for the first entrace
			// We travel along the rim, clockwise.
			List<Vector2Int> validEntrancePos = new List<Vector2Int>();
			
			// The direction we are headed
			Vector2Int currentDir = Vector2Int.up;
			Vector2Int currentPos = origin;
			
			// Get the initial direction
			if (ownership[origin.x,origin.y + 1] != index){ // Up
				currentDir = Vector2Int.right;
			} else if (ownership[origin.x - 1,origin.y] != index){ // Left
				currentDir = Vector2Int.up;
			} else if (ownership[origin.x,origin.y - 1] != index){ // Down
				currentDir = Vector2Int.left;
			} else if (ownership[origin.x + 1,origin.y] != index){ // Right
				currentDir = Vector2Int.down;
			} else {
				return false; // if we are not on an edge, we give up as this region is confusing
			}
			
			Vector2Int startDir = currentDir;
			// Walk the circle
			int iterations = 0; // We keep track of iterations so we can garuntee a finishing time.
			bool haveLeftStart = false; // We only check if we have returned to the start if we have left
			
			do {
				
				// Track iterations
				iterations++;
				if (iterations > maxIterations) { 
					Debug.Log("Caught in a loop! (Over "+maxIterations+" iterations while filling a region)");
					Debug.Log("Location: " + currentPos + "Region at: " + origin);
					Debug.Log("Facing: " + currentDir);
					return false;
				}
				
				// Add the edge
				if (!edges.Contains(currentPos)) {
					edges.Add(currentPos);
				}
				// Calculate vectors
				Vector2Int checkPos = currentPos + currentDir; // The position we are checking
				// up is anticlockwise rotation of our current direction
				// This makes it always face the interior
				Vector2Int upDir = rotateVector2Int(currentDir);
				
				if (ownership[checkPos.x,checkPos.y] != index) {
					// If the next position is not ours, we turn left
					currentDir = upDir;
				} else if (ownership[checkPos.x - upDir.x,checkPos.y - upDir.y] != index) {
					// If the next position is ours, and the tile below it isn't, we just move forward
					currentPos += currentDir;
					haveLeftStart = true; // Because we have moved, this is now true
				} else {
					// If the next position doesn't have a road tile below it, we rotate towards it
					currentPos += currentDir; // Move forward
					currentDir = -upDir; // Set ourselves down
					currentPos += currentDir; // Move down(the new forward)
					haveLeftStart = true; // Because we have moved, this is now true
				}
		} while (!(haveLeftStart && currentPos == origin));
			// We end when we return to the same position.
			return true;
		}
	}
	
	private Dictionary<int,WorldgenRegion> regionData;
	
	private int populationEstimate = 0;
	
	private HashSet<Vector2Int> roadPositions;
	
	private Tilemap primaryLayer;
	private Tilemap overdrawLayer;
	private Tilemap interiorLayer;
	public TileBase hospitalTile;
	private int minHeight = 0;
	private int maxHeight = 0;
	private bool inProgress = false;
	private float currentProgress = 0.0f; // The unweighted value of all previous stage steps
	private float stageWeight = 0.0f; // The weight of the current stage, used for calculating progress
	private float stageProgress = 0.0f; // The progress of a current substage of world generation
	private float totalWeight = 0.0f; // The total cost of worldgen
	private string stageName = "Waiting to begin";
	// Populated during road generation 
	// denotes if a spot is a full tile that can support a road
	private bool[,] emptyField;
	
	// The sprite for "empty land" found during road generation
	// Always the sprite at the center of the layer 0 tilemap
	private Sprite openSprite;
	
	public Sprite debugSprite;
	
	public (string,float) getProgress(){
		if (!inProgress) {return ("Waiting to begin",0.0f);}
		return (stageName,(currentProgress + stageWeight * stageProgress) / totalWeight);
	}
	
	void Start(){
		instance = this;
	}
	
    // Called to begin world generation
    public IEnumerator Generate()
    {	
		Debug.Log("Generation Called");
		
		// Go through all worldgesn steps, each asynchronous
		List<(string,IEnumerator,float)> worldGenSteps = new List<(string,IEnumerator,float)>();
		worldGenSteps.Add(("Preparing for World Generation",preGenerationSteps(),0.1f)); // The basic steps that have to happen first
		worldGenSteps.Add(("Making an island",createIsland(),5.0f)); // Place the island
		worldGenSteps.Add(("Adding large roads",createRoads(),1.0f)); // Place major(4 lane) roads
		worldGenSteps.Add(("Creating city blocks",assignPlots(),1.0f)); // Make building plots near roads
		worldGenSteps.Add(("Filling city blocks",createBuildings(),10.0f)); // Create buildings in the plots
		worldGenSteps.Add(("Adding other structures",createOtherTiles(),0.0f)); // Place tiles not in plots
		worldGenSteps.Add(("Finilizing static objects",bakeStatics(),0.0f)); // Finilize static objects
		worldGenSteps.Add(("Adding dynamic objects",createDyanmics(),0.0f)); // Add dynamic objects
		worldGenSteps.Add(("Creating the antagonists",createOpponent(),0.0f)); // Add zombies
		worldGenSteps.Add(("Preparing to start the game",startGame(),0.1f)); // Start the game

		int i = 1;
		totalWeight = 0.0f;
		// Count all the worldgen weights
		foreach ((string,IEnumerator,float) worldGenStage in worldGenSteps){
			totalWeight += worldGenStage.Item3;
		}
	
		// Iterate through all worldgen steps
		inProgress = true;
		currentProgress = 0.0f;
		foreach ((string,IEnumerator,float) worldGenStage in worldGenSteps){
			// Prepare for the worldgen stage
			double stepStartTime = Time.realtimeSinceStartupAsDouble; // Start the stopwatch
			stageName = worldGenStage.Item1;
			stageWeight = worldGenStage.Item3;
			Debug.Log(i+"/"+worldGenSteps.Count+") "+stageName);
			stageProgress = 0.0f; // We reset progress before each stage
			
			
			// Actually start the next worldgen stage
			yield return null; // Give it the frame, so it can update between steps
			yield return StartCoroutine(worldGenStage.Item2);
			
			// Update information and prepare for next stage
			i++;
			currentProgress+=stageWeight;
			double stepEndTime = Time.realtimeSinceStartupAsDouble; // Stop the stopwatch
			Debug.Log("Time elapsed:" + (stepEndTime - stepStartTime)); // Log stopwatch time
		}
		Debug.Log ("World Created");
		
		inProgress = false; // There are no more steps executed after this point
    }
	
	private IEnumerator preGenerationSteps(){
		layers = new Dictionary<int,Tilemap>();
		Random.InitState(seed); // Start the randomizer
		
		// Resize and resposition all elements
		noise.transform.position = new Vector3(maxX/2, maxY/2, 0);
		noise.transform.localScale = new Vector3(0.5f, 0.5f, 0);
		noise.GetComponent<SpriteRenderer>().size = new Vector3(maxX*2, maxY*2, 0);
		cameraObj.transform.position = new Vector3(maxX/2, maxY/2, -1);
		oceanObj.transform.position = new Vector3(maxX/2, maxY/2, 2);
		oceanObj.transform.localScale = new Vector3(maxX*2, maxY*2, 0);
		minimap.transform.position = new Vector3(maxX/2, maxY/2, -1);
		minimap.GetComponent<Camera>().orthographicSize = Mathf.Max(maxX,maxY)/2.0f;
		
		// Manage layer 0(Primary Layer)
        // We give this layer a special name because we use it so much
		primaryLayer = GridObj.transform.Find("LandLayer").gameObject.GetComponent<Tilemap>(); // Find it
		overdrawLayer = GridObj.transform.Find("OverdrawLayer").gameObject.GetComponent<Tilemap>();
		interiorLayer = GridObj.transform.Find("InteriorLayer").gameObject.GetComponent<Tilemap>();
		
		layers.Add(0,primaryLayer); // Store the tilemap(as reference)
		
		// Clear the primary layer. This happends just in case we run woldgen multiple times
		// Or had "leftover" tiles in the scene
		primaryLayer.ClearAllTiles();
		overdrawLayer.ClearAllTiles();
		
		// Set the number of zombies to zero
		ZombieManager.instance.zombieCount = 0;
		
		Debug.Log("Basic steps complete");
		yield return null;
	}
	
	// Places the basic land tiles in the world. 
	// This is done in large "chunks" as to allow for frames between chunk placement
	// Larger chunks are lower framerate, but take less time overall
    private IEnumerator createIsland()
    {
		
		// We count the number of tiles so they can be logged
		int count = 0;
		
		// All chunks of island will use the same perlin offset
		Vector2 perlinOffset = new Vector2(Random.Range(-65536,65536), Random.Range(-65536,65536));
		
		for (int chunkX = 0; chunkX < maxX; chunkX += chunkSize){
			for (int chunkY = 0; chunkY < maxY; chunkY += chunkSize){
				float yProgress = (float) (chunkSize*chunkY)/maxY;
				stageProgress = (((float) chunkX + yProgress)/ maxX);
				yield return null;
				Vector2Int chunkCorner = new Vector2Int (chunkX, chunkY);
				count += createIslandChunk(chunkCorner, perlinOffset);
			}
		}
		Debug.Log(count + " tiles placed, " + (count)/6806.25f + " square miles" );
		
    }

	private int createIslandChunk(Vector2Int cornerPoint, Vector2 perlinOffset) {
		// Compute practical bounds
		BoundsInt chunkBounds = new BoundsInt(cornerPoint.x,cornerPoint.y,0,chunkSize,chunkSize,1);
		
		// Check that our chunk does not go out of the max size
		Vector3Int chunkBoundry = (Vector3Int) Vector2Int.RoundToInt(cornerPoint + (Vector2.one * chunkSize));
		if(chunkBoundry.x > maxX){ chunkBoundry.x = maxX; }
		if(chunkBoundry.y > maxY){ chunkBoundry.y = maxY; }
		chunkBoundry.z = 1; // Otherwise, the volume is zero
        chunkBounds.max = chunkBoundry;
		
		// How many land tiles have we placed in this chunk?
		// Tracked as the return variable
		int chunkCount = 0;
		
		// How strong should noise be with respect to the distance weights.
		// Smaller values are less noisy
		float noiseRatio = 0.60f;
		// Affects the size of the noise. Larger values result in smaller "bumps"
		float noiseScale = 8f;
		
		// Regular World Generation
		
		TileBase[] chunkTileArray = new TileBase[chunkBounds.size.x * chunkBounds.size.y];
		int chunkArrayIndex = 0;
		
		// Loop over all tiles
		for(int y = cornerPoint.y; y < chunkBoundry.y; y++){
			for(int x = cornerPoint.x; x < chunkBoundry.x; x++){
				// We use maxX/maxY instead of chunkBoundry
				// because these are dependent on the absolute position,
				// and are independent of what chunk the position is in
				
				// Get base perlin noise(Big clumps)
				float noise = Mathf.PerlinNoise(
					((float) x) / maxX * noiseScale + perlinOffset.x,
					((float) y) / maxY * noiseScale + perlinOffset.y);
				// noise = Mathf.Clamp(noise,0f,1f); // This is approximatly true, removed for effeciency
				
				// Get weight factor for distance
				float distance = (Vector2.Distance(
					new Vector2(0.5f,0.5f), // From the center
					new Vector2((float) x / maxX, (float) y / maxY) // To the position(normalised)
					)) * 2 - 1; // We adjust the range to be [-1,0]
				// distance = Mathf.Clamp(distance,-1f,1f); // Distance is clamped to this
				
				// We measure our noise to zero, < 0 is land, > 0 is water
				if (distance + noiseRatio * noise < 0){
					// Place a land tile
					chunkTileArray[chunkArrayIndex] = landTile;
					// Add this tile to the count
					chunkCount++;
				}
				
				// What position we are in in the 1d array
				chunkArrayIndex++;
			}
		}
		// Actually place the tiles using the mass place command
		primaryLayer.SetTilesBlock(chunkBounds, chunkTileArray);
		return chunkCount;
	}
	
    // A test world for making sure all land tiles can be placed
	// Places every possible 5x5 grid of tiles in sequence
	// So long as there is a sprite for all the tiles placed in the test, 
	// we have enough sprites for the game
	private IEnumerator drawTest(){
		TileBase tile = hospitalTile;
		int count = 0;
		
		for(int i = 0; i < 181; i++){
			stageProgress = (float) i / 181.0f;
			yield return null;
			for(int j = 0; j < 181; j++){
				BoundsInt chunkBounds = new BoundsInt(i*7,j*7,0,5,5,1);
				TileBase[] chunkTileArray = new TileBase[chunkBounds.size.x * chunkBounds.size.y];
				int chunkArrayIndex = 0;
				
				// Place "center" position always
				count++;

				// For all other positions in a 3x3 grid around the center tile, maybe place tiles
				int subCount = 1; // The current multiple of the count used to determine placement
				for(int x = -2; x <= 2; x++){
					for(int y = -2; y <= 2; y++){
						if (x == 0 && y == 0){
							chunkTileArray[chunkArrayIndex] = hospitalTile;
						}
						
						subCount *= 2;
						if (count % subCount < subCount / 2){ // This pattern iterates through possibilities
							// If a tile should be made, place it
							chunkTileArray[chunkArrayIndex] = hospitalTile;
						}else{
							chunkTileArray[chunkArrayIndex] = landTile;
						}
						
						chunkArrayIndex++;
					}
				}
				primaryLayer.SetTilesBlock(chunkBounds, chunkTileArray);
			}
		}
		yield return null;
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
		if (layerNum < 0){
			layer.GetComponent<TilemapRenderer>().sortingOrder = layerNum--;
		} else {
			layer.GetComponent<TilemapRenderer>().sortingOrder = layerNum;			
		}
		// Place it in layers and hirearchy
		layer.transform.parent = GridObj.transform;
		layers[layerNum] = layer.GetComponent<Tilemap>();
	}
	
	// Place one interconnected road system on the island
	private IEnumerator createRoads(){
		// We assume the sprite the road is placed on is the sprite that roads go on
		Vector3Int center = new Vector3Int(maxX/2,maxY/2,0);
		openSprite = primaryLayer.GetSprite(center);
		
		// Place the first "seed" road from which all roads will grow
		primaryLayer.SetTile(center,roadTile);
		
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
		float tileCount = (float) (maxX * maxY);
		int desiredRoads = (int) Random.Range((int) (tileCount * roadRatio * (1.0f - roadVariance)),(int) (tileCount * roadRatio * (1.0f + roadVariance)));
 		float priorityBias = 0.95f; // percent chance of using priority set
		
		// We loop until we hit our number of roads
		// or we run out of positions
		while(roadCount < desiredRoads && regularExpansion.Count != 0){
			// Allow for an asynch break at quarter incriments
			if (roadCount == desiredRoads/4 || roadCount == desiredRoads/2 || roadCount == (desiredRoads * 3) / 4){
				stageProgress = (float) roadCount / desiredRoads;
				yield return null;
			}
			// Check if we do priority, or normal
			HashSet<Vector2Int> possiblePositions;
			if (priorityExpansion.Count != 0 && Random.Range(0.0f,1.0f) < priorityBias){
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
				if (possiblePositions.Count == 0){
					 break;
				}
				// Pick a position to try
				pos = possiblePositions.ElementAt(Random.Range(0,possiblePositions.Count));
				foundPos = false;
				posVec3 = new Vector3Int(pos.x,pos.y,0); // Tilemaps want 3d vectors
				 
				// Ignore positions that are not placeable sprites
				if (primaryLayer.GetSprite(posVec3) != openSprite){
					possiblePositions.Remove(pos);
					continue;
				}
				 
				// Ignore positions that create invalid tiles(2x2 grids)
				// Up and Left
				if (primaryLayer.GetTile(posVec3+Vector3Int.up) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.left) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.up+Vector3Int.left) == roadTile
				){
					possiblePositions.Remove(pos);
					continue; 
				}
				// Up and Right
				if (primaryLayer.GetTile(posVec3+Vector3Int.up) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.right) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.up+Vector3Int.right) == roadTile
				){
					possiblePositions.Remove(pos);
					continue; 
				}
				// Down and Left
				if (primaryLayer.GetTile(posVec3+Vector3Int.down) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.left) == roadTile &&
					primaryLayer.GetTile(posVec3+Vector3Int.down+Vector3Int.left) == roadTile
				){
					possiblePositions.Remove(pos);
					continue; 
				}
				// Down and Right
				if (primaryLayer.GetTile(posVec3+Vector3Int.down) == roadTile &&
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
			if (foundPos == false){
				 continue;
				 // We only continue here. We may not have a position just because we chose
				 // priority queue and the queue was full of invalid positions
			}
			
			// Place the tile
			posVec3 = new Vector3Int(pos.x,pos.y,0);
			primaryLayer.SetTile(posVec3,roadTile); // TODO: Batch tile placement
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
			} else if (primaryLayer.GetTile(posVec3 + Vector3Int.down) == roadTile) { // If we have Down
				priorityExpansion.Add(new Vector2Int(pos.x,pos.y + 1)); // We try Up
			}
			
			// Make them a priority if they are across from a road(prioritise lines)
			// Left/Right connecion
			if (primaryLayer.GetTile(posVec3 + Vector3Int.right) == roadTile) {// If we have Right
				priorityExpansion.Add(new Vector2Int(pos.x - 1,pos.y)); // We try Left
			} else if (primaryLayer.GetTile(posVec3 + Vector3Int.left) == roadTile) { // If we have Left
				priorityExpansion.Add(new Vector2Int(pos.x + 1,pos.y)); // We try Right
			}
			
		}
		
		
		roadPositions = regularExpansion; // We save the road positions for later
		Debug.Log(roadCount + "/" + desiredRoads + " roads");
	}
	
	// Divide the map into navicable blocks
	private IEnumerator assignPlots(){
		// This designates which plot owns each tile(by index)
		ownership = new int[maxX,maxY];
		
		// Keep track of each region
		regionData = new Dictionary<int,WorldgenRegion>();
		
		int currentIndex = 1; // What is the current highest ownership value 
		regionData.Add(0,new WorldgenRegion(0)); // Make the 0th region, this is always empty
		regionData.Add(currentIndex,new WorldgenRegion(1)); // Make the first region
		Vector3Int posVec3;
		
		// Region creation by flood filling
		// This works well on wierd, but closed shapes
		
		for (int x = 0; x < maxX; x++){
			// We take a break halfway through
			if (x == maxX/2){
				stageProgress = 0.25f;
				yield return null;
			}
			
			// check all y positions
			for (int y = 0; y < maxY; y++){
				// We try to "fill on each tile"
				posVec3 = new Vector3Int(x,y,0); // Tilemaps want 3d vectors
				
				if (!fillPlot(x,y,currentIndex)){
					// If the fill returned false, then we know a region was made. 
					
					// Deal with current region
					regionData[currentIndex].origin = new Vector2Int(x,y); // The origin is just somewhere in the region.
					
					// Move onto new region
					currentIndex++; // The next region will have an index 1 higher
					regionData.Add(currentIndex,new WorldgenRegion(currentIndex)); // Make a new region
				}else{
					regionData[currentIndex] = new WorldgenRegion(currentIndex);
				}
			}
		}
		
		// We let the operation take a break
		stageProgress = 0.5f;
		yield return null;
		
		// Region creation by road following
		// This works best for open areas without strange obstacles
		int expandDisMax = 75;
		int roadsEvaluated = 0;
		foreach (Vector2Int pos in roadPositions) {
			roadsEvaluated++;
			if (roadsEvaluated == roadPositions.Count / 2){
				stageProgress = 0.70f; // We are halfway complete with the second part of region filling, so 75%
				yield return null;
			}
			posVec3 = new Vector3Int(pos.x,pos.y,0);
			
			if (!validOpen(pos)) {
				continue;
			}
			// We try to find out if we are a straight road.
			// If we are in a corner between roads we do nothing
			bool foundDirection = true; // We default to true, but it is set to false in the else blcok
			Vector3Int anchorDir = Vector3Int.zero; // The direction of the road we are "hooked" to
			Vector3Int leftDir = Vector3Int.zero; // A direction perpendicular to the road direction, either is valid
			
			// If we are down anchored
			if (primaryLayer.GetTile(posVec3 + Vector3Int.down) == roadTile 
				&& primaryLayer.GetTile(posVec3 + Vector3Int.left) != roadTile
				&& primaryLayer.GetTile(posVec3 + Vector3Int.right) != roadTile)
			{
				anchorDir = Vector3Int.down;
				leftDir = Vector3Int.left;
				
				// Sometimes we are in a tunnel, this case is handled seperatly
				if (primaryLayer.GetTile(posVec3 + Vector3Int.up) == roadTile && Random.Range(0.0f,1.0f) < 0.5f){
					anchorDir = Vector3Int.up;
				}
			} 
			// If we are left anchored
			else if (primaryLayer.GetTile(posVec3 + Vector3Int.left) == roadTile 
				&& primaryLayer.GetTile(posVec3 + Vector3Int.up) != roadTile
				&& primaryLayer.GetTile(posVec3 + Vector3Int.down) != roadTile)
			{
				anchorDir = Vector3Int.left;
				leftDir = Vector3Int.up;
				
				// Case for vertical tunnels
				if (primaryLayer.GetTile(posVec3 + Vector3Int.right) == roadTile && Random.Range(0.0f,1.0f) < 0.5f){
					anchorDir = Vector3Int.right;
				}
			}
			// If we are right anchored
			else if (primaryLayer.GetTile(posVec3 + Vector3Int.right) == roadTile 
				&& primaryLayer.GetTile(posVec3 + Vector3Int.up) != roadTile
				&& primaryLayer.GetTile(posVec3 + Vector3Int.down) != roadTile)
			{
				anchorDir = Vector3Int.right;
				leftDir = Vector3Int.up;
			}
			// If we are up anchored
			else if (primaryLayer.GetTile(posVec3 + Vector3Int.up) == roadTile 
				&& primaryLayer.GetTile(posVec3 + Vector3Int.left) != roadTile
				&& primaryLayer.GetTile(posVec3 + Vector3Int.right) != roadTile)
			{
				anchorDir = Vector3Int.up;
				leftDir = Vector3Int.right;
			} else {
				foundDirection = false;
			}
			
			// If we did not find our direction, continue. 
			if (foundDirection == false) {
				continue;
			}
			
			// Now we expand from our chosen position.
			// Left/right expands along the "anchoring" road.
			// In the anchor direction, we expand up to expandDisMax tiles or until an obstical is reached
			
			// Pre-loop behavior
			int roadOffset = 0; // How far offset are we leftwards from the center?
			int dir = 1;  // 1 or -1. +1 means we next move leftwards, -1 means rightwards.
			// We loop through roadoffset. This is a while loop for readability
			while (true) {
				// We fill from where we are plus to the left times the offset
				Vector3Int fillPos = posVec3 + leftDir * roadOffset;
				
				// Exit check for the while loop
				if (primaryLayer.GetTile(fillPos + anchorDir) != roadTile // Exit Case #1: If we do not have an anchoring road
					|| !validOpen(fillPos)) // Exit Case #2: If we don't have at least 1 open tile in front
				{
					// If we are moving left, turn around
					if (dir == 1){
						roadOffset = -1; // The center is covered initially, so we move 1 rightward
						dir = -1; // We change direction
						continue;
					}
					// If we are moving right we are done
					break;
				}
				
				// For all positions along the road, we want to expend upto 50 units up(where allowed)
				int expandOffset = 0; // Distance(tiles) opposite the anchoring direction during filling.
				while (true) {
					// Exit cases
					if (expandOffset >= expandDisMax){ // We would go to far
						break;
					}
					
					// Get a position offset away from the anchor
					Vector3Int outwardsFillPos = fillPos - expandOffset * anchorDir;
					// If the position is not valid, we stop for now.
					if (!validOpen(outwardsFillPos)){
						break;
					}
					ownership[outwardsFillPos.x,outwardsFillPos.y] = currentIndex;
					regionData[currentIndex].size = regionData[currentIndex].size + 1;
					expandOffset++; // Move further out
				}
				
				// We move leftward or rightward at the end of the loop
				roadOffset+=dir;
			}
			regionData[currentIndex].origin = pos;
			currentIndex++;
			regionData.Add(currentIndex,new WorldgenRegion(currentIndex));
		}
		
		stageProgress = 0.90f; // We are halfway complete with the second part of region filling, so 75%
		yield return null;
		// Remove nonsense roads
		foreach (Vector2Int pos in new HashSet<Vector2Int>(roadPositions)) {
			Vector2Int dir = Vector2Int.up;
			bool doRemove = true; 
			for (int dirCount = 0; dirCount < 4; dirCount++){
				dir = rotateVector2Int(dir);
				if(primaryLayer.GetTile(new Vector3Int(pos.x + dir.x,pos.y + dir.y, 0)) == roadTile){
					int checkIndex = ownership[pos.x,pos.y];
					if (checkIndex > 0 && ownership[pos.x + dir.x*2,pos.y + dir.y*2] == checkIndex){
						primaryLayer.SetTile(new Vector3Int(pos.x + dir.x,pos.y + dir.y, 0),landTile); // Delete the invalid tile
						ownership[pos.x + dir.x,pos.y + dir.y] = checkIndex; // Change ownership of now empty tile
						regionData[checkIndex].size = regionData[checkIndex].size + 1; // Count that added tile
					}else {
						doRemove = false;
					}
				}
			}
			if(doRemove){
				roadPositions.Remove(pos);
			}
		}

		Debug.Log(currentIndex + " regions created");
	}
	
	List<Vector2Int> trackedELem;
	// Starting at a position claim all available connected positions
	// A recursive helper for assignPlots
	// TODO: This might be better without recursion.
	private bool fillPlot(int x, int y, int ownervalue, int depth = 0){
		if (depth == 0){
			trackedELem = new List<Vector2Int>();
		}
		
		// Base case
		Vector3Int posVec3 = new Vector3Int(x,y,0);
		if (depth > 750 || ownership[x,y] == -1){
			return true;
		}
		// We stop on roads, claimed tiles and non-open land tiles
		if (!validOpen(x,y,0)){
			return depth == 0;
		}
		
		// Actually claim the tile
		ownership[x,y] = ownervalue;
		regionData[ownervalue].size = regionData[ownervalue].size + 1;
		trackedELem.Add(new Vector2Int(x,y));
		
		
		// Recurse up
		if (fillPlot(x, y+1, ownervalue, depth + 1)){
			while (trackedELem.Count != 0){
				ownership[trackedELem[0].x,trackedELem[0].y] = -1;
				trackedELem.RemoveAt(0);
			}
			return true;
		}
		
		// Recurse left
		if (fillPlot(x-1, y, ownervalue, depth + 1)){
			while (trackedELem.Count != 0){
				ownership[trackedELem[0].x,trackedELem[0].y] = -1;
				trackedELem.RemoveAt(0);
			}
			return true;
		}
		
		// Recurse down
		if (fillPlot(x, y-1, ownervalue, depth + 1)){
			while (trackedELem.Count != 0){
				ownership[trackedELem[0].x,trackedELem[0].y] = -1;
				trackedELem.RemoveAt(0);
			}
			return true;
		}
		
		// Recurse right
		if (fillPlot(x+1, y, ownervalue, depth + 1)){
			while (trackedELem.Count != 0){
				ownership[trackedELem[0].x,trackedELem[0].y] = -1;
				trackedELem.RemoveAt(0);
			}
			return true;
		}
		return false;
	}
	
	// Overload for validOpen
	private bool validOpen(int x, int y, int ownershipValue = -1) {
		Vector3Int posVec3 = new Vector3Int(x,y,0);
		return validOpen(posVec3,ownershipValue);
	}
	// Overload for validOpen
	private bool validOpen(Vector2Int vec, int ownershipValue = -1) {
		Vector3Int posVec3 = (Vector3Int) (vec);
		return validOpen(posVec3,ownershipValue);
	}
	// Is a specific area claimed with a specific region, not a road and not coast/water?
	private bool validOpen(Vector3Int vec, int ownershipValue = -1){
		return ownership[vec.x,vec.y] == ownershipValue && // Only for specific region flags
			primaryLayer.GetSprite(vec) == openSprite; // open grass
	}
	
	// Place 
	private IEnumerator createBuildings(){
		int regionCount = 0;
		foreach (var regionPair in regionData){
			WorldgenRegion region = regionPair.Value;
			// Basic checks
			if (region.size <= 0){
				continue;
			}
			if (region.origin == new Vector2Int(-1,-1)){
				continue;
			}
			// TODO: Make a system for regions to "fight" for priority to place
			
			// Attempt generation
			if(!attemptSuburb(region)){; // A winding suburb
				cityParking(region); // literally concrete desert. 
			}
			// because region generation can be slow
			// We yield control out of the couroutine briefly between regions
			regionCount++;
			stageProgress = (float) regionCount / regionData.Count();
			if(regionCount % 1 == 0){ 
			// Stopping every region can be unneccesarily slow, but sometimes one region will take a long while
			// This gives occasional breaks. Will need to be fine tuned as part of the worldgen timings
				yield return null; 
			}
		}
		Debug.Log("City populated with " + populationEstimate + " people");
	}
	
	// TODO: headers
	private void fillRegion(WorldgenRegion region, TileBase tile){
		// Pick a random edge to start on
		List<Vector2Int> edges = region.getEdges(); 
		int currentIndex = Random.Range(0,edges.Count());
		Vector2Int currentPos = edges[currentIndex];
		
		// Track where we want to place
		List<Vector2Int> placePositions = new List<Vector2Int>();
		
		// Points we need to fill. This keeps 
		Queue<Vector2Int> expandPositions = new Queue<Vector2Int>();
		do { // While we have places left to expand into
			// Add valid adjacent spaces
			Vector2Int checkDir = Vector2Int.up;
			for (int directionCount = 0; directionCount < 4; directionCount++){
				checkDir = rotateVector2Int(checkDir);
				Vector2Int checkPos = checkDir + currentPos;
				if(validOpen(checkPos,region.index) && !placePositions.Contains(checkPos)){ // Is this an open position for our region
					expandPositions.Enqueue(checkPos);
				}
			}
			
			// Add the element
			placePositions.Add(currentPos);
			
			// If we are out of elements, we are done
			if(expandPositions.Count() == 0){
				break;
			}
			// Grab the top element
			currentPos = expandPositions.Dequeue();
		}while(true);
		customMassTilePlace(placePositions,tile);
	}
	
	
	private bool cityParking(WorldgenRegion region) {
		// TODO: Add an entrance and make it not double add sidewalks.
		bool havePlacedBlocks = false;
		if (region.size > 10) {
			return havePlacedBlocks;
		}
		
		if (region.getEdges().Count() > 12) {
			return havePlacedBlocks;
		}
		fillRegion(region,parkingTile);
		havePlacedBlocks = true;
		return havePlacedBlocks;
	}
	private bool attemptSuburb (WorldgenRegion region) {
		// This value is returned at all exit points
		// If we have not placed blocks, we can give this region to a different generator
		// Once we place tiles, we are commited to this being a suburb
		bool havePlacedBlocks = false;
		
		// Reasons to not build a suburb
		if (region.size < 16) {
			return havePlacedBlocks;
		}
		if (region.size > 700){
			return havePlacedBlocks;
		}
		if (region.getEdges().Count() < 12) {
			return havePlacedBlocks;
		}
		bool isGrid = (Random.Range(0.0f,1.0f) < 0.5f);
		
		// We keep track of the amount of area "covered" to see if we need to grow more.
		int coverage = 0;
		int entrances = 0;
		int minEntrances = 1 + (int)(region.size / 250);
		if(isGrid){
			minEntrances = 1; // Grid suburbs make entrances, so as long as we meet coverage we have enough suburb
		}
		// A constant we use in attempting to create the suburbs
		// When we are at this percent of our area as roads, we are maybe okay to stop
		float minCoveragePercent = 0.20f;
		int minCoverage = (int) (minCoveragePercent * region.size);
		
		List<Vector2Int> placedTilePosition = new List<Vector2Int>();
		
		// The edges left to attempt, starts as a copy of region.getEdges()
		List<Vector2Int> attemptedEdges = new List<Vector2Int> (region.getEdges());
		while (coverage < minCoverage || entrances < minEntrances) {
			// Alternate exit condition, if we run out of places to expand from.
			if(attemptedEdges.Count() == 0){
				return havePlacedBlocks; 
			}
			
			// Pick a spot to try
			int entracePointIndex = Random.Range(0,attemptedEdges.Count());
			Vector2Int entrancePoint = attemptedEdges[entracePointIndex];
			
			// Check the validity of the entrance point
			Vector2Int entranceDir = getValidSuburbEntrance(entrancePoint,region);
			if (entranceDir != Vector2Int.zero){
				// If the entrance is valid
				coverage += 1;
				if(isGrid) {
					// Grid suburbs look uniform and work best in dense areas.
					coverage += gridSuburbBuilder(region, entrancePoint, entranceDir, ref placedTilePosition);
				} else {
					// "Naturalistic" suburbs are high variety, but on sizes above about 600 they don't work as well
					// We build using a maze generation algoritm similar to Prim's algorithm, but weighted for better suburbs
					coverage += suburbBuilder(region, entrancePoint, entranceDir, ref placedTilePosition);
				}
				entrances += 1;
				havePlacedBlocks = true;
			} else {
				attemptedEdges.RemoveAt(entracePointIndex);
			}
			
		}
		
		// We only try to place houses if there is a chance of them doing something
		foreach (Vector2Int roadPos in placedTilePosition) {
			Vector2Int checkDir = Vector2Int.up;
			for (int directionCount = 0; directionCount < 4; directionCount++) {	
				checkDir = rotateVector2Int(checkDir);
				Vector2Int perpDir = rotateVector2Int(checkDir);
				Vector2Int checkPos = roadPos + checkDir;
				
				// This check prevents placing houses at the end of the road, because it can't connect
				if (primaryLayer.GetTile((Vector3Int) (roadPos + perpDir)) == suburbTile ||
					primaryLayer.GetTile((Vector3Int) (roadPos - perpDir)) == suburbTile
				) {
					if (validOpen(roadPos + checkDir,region.index)){
						if (Random.Range(0,2) == 0){
							// Small House
							primaryLayer.SetTile((Vector3Int) (roadPos + checkDir), houseTiles[0]);
							interiorLayer.SetTile((Vector3Int) (roadPos + checkDir), houseTiles_i[0]);
							populationEstimate += 2;
						} else {
							// Big House
							primaryLayer.SetTile((Vector3Int) (roadPos + checkDir), houseTiles[1]);
							interiorLayer.SetTile((Vector3Int) (roadPos + checkDir), houseTiles_i[1]);
							populationEstimate += 3;// The average is 2.5 per home so this works out
						}
					}
				}
			}
		}
		return havePlacedBlocks;
	}
	
	private class suburbPosition {
		private const int maxDensity = 10;
		private const int maxLocalDensity = 4;
		public float weight;
		public Vector2Int position;
		private bool deactive;
		private bool checkStatic; // Some tiles are invalid as a result of static details. For speed, we only check these once. 
		
		private int density; // 5x5 density
		public int localDensity; // 3x3 density
		private int neighbors; // the number of DIRECT neighbors. 
		bool hasUp;
		bool hasDown;
		bool hasLeft;
		bool hasRight;
		
		
		public suburbPosition (Vector2Int pos){
			position = pos;
			weight = 0.0f;
			deactive = false;
			checkStatic = true;
			density = 0;
			localDensity = 0;
			neighbors = 0;
			hasUp = false;
			hasDown = false;
			hasLeft = false;
			hasRight = false;
			
		}
		
		private void deactivate(){
			weight = 0.0f;
			deactive = true;
		}
		private void updatePoint(Vector2Int point){
			int addDis = (position - point).sqrMagnitude;
			if(addDis == 1){
				neighbors++;
				if((position - point) == Vector2Int.up){
					hasUp = true;
				}else if((position - point) == Vector2Int.left){
					hasLeft = true;
				}else if((position - point) == Vector2Int.right){
					hasRight = true;
				}else if((position - point) == Vector2Int.down){
					hasDown = true;
				}
				
			}
			if(addDis <= 2){
				localDensity++;
			}
			density++;
		}
		public void updateWeight(List<suburbPosition> placePositions, suburbPosition newPosition, WorldgenRegion region){
			if (deactive){ // Some tiles are marked to have no weight(generally due to already being placed)
				return;
			}
			if (checkStatic){// We only do this once. 
				checkStatic = false;
				if (!worldGen.instance.validOpen(position,region.index)) {
					deactivate(); 
					return;
				}
				// Check direct adjacency rules
				Vector2Int checkDir = new Vector2Int(0,1);
				for (int i = 0; i < 4; i++){
					checkDir = worldGen.rotateVector2Int(checkDir); // TODO: rotating vectors should not be a world function.
					if (worldGen.instance.primaryLayer.GetTile((Vector3Int) (position + checkDir)) == worldGen.instance.roadTile){
						deactivate();
						return;
					}
				}
				// We have to check for pre-placed suburban roads for density,neighbors and localDensity
				BoundsInt checkRegion = new BoundsInt((Vector3Int) position - new Vector3Int(2, 2, 0), new Vector3Int(5, 5, 1));
				foreach (Vector3Int point3 in checkRegion.allPositionsWithin) {
					if(worldGen.instance.primaryLayer.GetTile(point3) == worldGen.instance.suburbTile){
						Vector2Int point = (Vector2Int) point3;
						updatePoint(point);
					}
				}
				
			}
			// No placing twice!
			if (newPosition == this){
				deactivate();
				return;
			}
			updatePoint(newPosition.position);
			if (neighbors == 0){ // We must build from existing roads. 
				weight = 0.0f;
				return;
			}
			
			int effMaxDensity = maxDensity;
			int effMaxLocalDensity = maxLocalDensity;
			if((hasUp && hasDown && !hasLeft && !hasRight) || (!hasUp && !hasDown && hasLeft && hasRight)){
				effMaxDensity += 2;
				effMaxLocalDensity += 1;
			}
			if(localDensity <= 2){
				effMaxDensity += 3;
			}
			if (localDensity >= effMaxLocalDensity){
				weight = 0.0f;
				return;
			}
			
			if (density >= effMaxDensity){
				weight = 0.0f;
				return;
			}
			
			// If we get to this point, the weight is nonzero. 
			
			weight = 1.0f*(1 - (float) density/effMaxDensity)*(1 - (float) localDensity/effMaxLocalDensity); // More dense areas are worth less to expand into
			if(neighbors == 1){
				weight = weight/2;
			}
		}
	}
	
	// TODO: This method is ineffecient. It does constant Contains checks on lists.
	private int gridSuburbBuilder(WorldgenRegion region, Vector2Int startPosition, Vector2Int startDir, ref List<Vector2Int> positionList){
		
		List<Vector2Int> placePositions = new List<Vector2Int>();
		
		// These are the possible locations we could try to grow from. 
		List<Vector2Int> expandLocations = new List<Vector2Int>();
		List<Vector2Int> failedPositions = new List<Vector2Int>();
		expandLocations.Add(startPosition);
		Vector2Int currentPos;
		Vector2Int leftDir = rotateVector2Int(startDir);
		while (expandLocations.Count != 0) {
			
			int posIndex = Random.Range(0,expandLocations.Count);
			currentPos = expandLocations[posIndex];
			expandLocations.RemoveAt(posIndex);
			
			// Expand in the direction
			Vector2Int travelPosTop;
			Vector2Int travelPosBottom; // "Bottom" is not cardinal south. In this context, "Bottom" is inverse of the initial direction
			
			for (travelPosTop = currentPos + startDir; // Start the travel pointer
				 validOpen(travelPosTop,region.index) && validOpen(travelPosTop+leftDir,region.index) && validOpen(travelPosTop-leftDir,region.index);
				 travelPosTop += startDir) { // Move the travel pointer
				// Mark the tile to be placed
				if(!placePositions.Contains(travelPosTop)) {
					placePositions.Add(travelPosTop);
				}
			}
			travelPosTop = travelPosTop - startDir; // We overshoot by 1.
			// TODO: Moving backwards by 1 is inelegant
			// Expand in the other direction
			for (travelPosBottom = currentPos; // Start the travel pointer
				 validOpen(travelPosBottom,region.index) && validOpen(travelPosBottom+leftDir,region.index) && validOpen(travelPosBottom-leftDir,region.index);
				 travelPosBottom -= startDir) { // Move the travel pointer
				// Mark the tile to be placed
				if(!placePositions.Contains(travelPosBottom)) {
					placePositions.Add(travelPosBottom);
				}
			}
			travelPosBottom = travelPosBottom + startDir; // We overshoot by 1.
			
			// Edge Case: We were blocked from expanding enough to form a suburb(Need atleast 2 roads)
			if(travelPosBottom == travelPosTop){
				placePositions.Remove(travelPosTop); // We could't place here, but we might be able to expand still.
				failedPositions.Add(travelPosTop);
			}
			
			
			// Add new positions to the expandLocations
			// TODO: This approach limits the ability for 1 spaced suburbs to be placed.
			List<Vector2Int> potentialNewLoc = new List<Vector2Int>();
			potentialNewLoc.Add(travelPosBottom + leftDir * 3);
			potentialNewLoc.Add(travelPosBottom - leftDir * 3);
			potentialNewLoc.Add(travelPosTop + leftDir * 3);
			potentialNewLoc.Add(travelPosTop - leftDir * 3);
			
			foreach (Vector2Int loc in potentialNewLoc) {
				if ((primaryLayer.GetTile((Vector3Int) (loc + startDir)) == roadTile || primaryLayer.GetTile((Vector3Int) (loc - startDir)) == roadTile) && // We must neighbor a road
					!expandLocations.Contains(loc) && // Prevent double growth
					!placePositions.Contains(loc) && // Prevent expanding where we have already gone(but other entrances would be acceptable
					!failedPositions.Contains(loc) &&
					validOpen(loc,region.index) && // The center must be available
					validOpen(loc+leftDir,region.index) && // The "left" must be available
					validOpen(loc-leftDir,region.index)) { // The "right" must be available
						expandLocations.Add(loc);
				}
			}
			
		}
		foreach (Vector2Int position in placePositions){
			if (!positionList.Contains(position))
				positionList.Add(position);
		}
		// Actually place the tiles
		return customMassTilePlace(placePositions,suburbTile);
	}
	
	private int suburbBuilder(WorldgenRegion region, Vector2Int startPosition, Vector2Int startDir, ref List<Vector2Int> positionList){
		// Place the first suburb tiles, this is garunteed
		primaryLayer.SetTile((Vector3Int) startPosition,suburbTile);
		
		// We store startdir for parity with gridSuburbBuilder, but we only use it here.
		startPosition = startPosition + startDir;
		
		// We keep track of where we try to add roads, we will actually fill them in later
		List<suburbPosition> evaluatedPositions = new List<suburbPosition>();
		List<Vector2Int> placePositions = new List<Vector2Int>();
		// A weighted list
		Dictionary<Vector2Int, suburbPosition> expandPositions = new Dictionary<Vector2Int, suburbPosition>();
		suburbPosition currentTile = new suburbPosition(startPosition);
		while (true) { // Breaks on zero total weight
			// We place a tile at this position
			// Checks are done when elements are added to the expandPositions list
			evaluatedPositions.Add(currentTile);
			placePositions.Add(currentTile.position);
			// Update the weights
			BoundsInt checkRegion = new BoundsInt((Vector3Int) currentTile.position - new Vector3Int(2, 2, 0), new Vector3Int(5, 5, 1));
			foreach (Vector3Int point3 in checkRegion.allPositionsWithin) {
				Vector2Int point = (Vector2Int) point3;
				if(!expandPositions.ContainsKey(point)){
					expandPositions.Add(point,new suburbPosition(point));
				}
				expandPositions[point].updateWeight(evaluatedPositions, currentTile,region);
			}
			
			List<suburbPosition> maxWeightPos = new List<suburbPosition>();
			float maxWeight = 0.0f;
			foreach (suburbPosition tile in expandPositions.Values) {
				if(tile.weight > maxWeight){
					maxWeightPos.Clear();
					maxWeight = tile.weight;
					maxWeightPos.Add(tile);
				}else if(tile.weight == maxWeight){
					maxWeightPos.Add(tile);
				}
			}
			if (maxWeight == 0.0f){
				break;
			}
			currentTile = maxWeightPos[Random.Range(0,maxWeightPos.Count())];
			/*
			// We sum all the current weights
			weightTotal = 0.0f;
			foreach (suburbPosition tile in expandPositions.Values) {
				weightTotal += tile.weight;
			}
			
			if(weightTotal == 0.0f){
				break; // We have nothing else that wants to be placed
			}
			
			// We then do a weighted random draw of the expandPositions
			// We generate a random value, then choose the position at that value
			float randomValue = Random.Range(0.0f,weightTotal);
			foreach (suburbPosition tile in expandPositions.Values) {
				randomValue -= tile.weight; // we count down the weight
				if(randomValue <= 0.0f) {
					currentTile = tile; // If we found our value, move to its position
					// The tile removes itself from the list in effect by reducing its weight to zero. 
					break; // We found our value, so we can stop
				}
			}
			*/
		}
		foreach (Vector2Int position in placePositions){
			if (!positionList.Contains(position))
				positionList.Add(position);
		}
		// Actually place the tiles
		return customMassTilePlace(placePositions,suburbTile);
		
	}
	
	private int customMassTilePlace(List<Vector2Int> positions, TileBase tile){
		if(positions.Count == 0){
			return 0; // If we did nothing, do nothing.
		}
		
		// Find the max and min positions. These are used to create our list for the mass place command
		BoundsInt placementBoxArea = new BoundsInt();
		placementBoxArea.SetMinMax((Vector3Int) positions[0],(Vector3Int) positions[0] + Vector3Int.one);
		
		foreach (Vector2Int position in positions) {
			// We expand the box to fit the position, in the X axis
			if (position.x >= placementBoxArea.xMax) {
				placementBoxArea.xMax = position.x + 1;
			}
			// We cannot do else ifs here, it is not garunteed that xMax > xMin
			if(position.x < placementBoxArea.xMin){
				placementBoxArea.xMin = position.x;
			}
			
			// We fit the box in the Y axis
			if (position.y >= placementBoxArea.yMax) {
				placementBoxArea.yMax = position.y + 1;
			}
			if(position.y < placementBoxArea.yMin){
				placementBoxArea.yMin = position.y;
			}
		}
		
		// we store the tiles to be placed in an array that has been squashed to 1d
		TileBase[] fastTilePlacementArray = new TileBase[placementBoxArea.size.x * placementBoxArea.size.y];
		
		// Copy in pre-exisiting tiles so we can replace them.
		foreach (var arrayPos in placementBoxArea.allPositionsWithin)
        {
			Vector2Int offsetPos = (Vector2Int) (arrayPos - placementBoxArea.min);
			int arrayIndex = offsetPos.x + placementBoxArea.size.x * offsetPos.y;
			fastTilePlacementArray[arrayIndex] = primaryLayer.GetTile(arrayPos);
        }
		
        foreach (Vector2Int position in positions) {
			Vector2Int offsetPos = position - (Vector2Int) placementBoxArea.min;
			int arrayIndex = offsetPos.x + placementBoxArea.size.x * offsetPos.y;
			if (arrayIndex >= fastTilePlacementArray.Length) {
				Debug.Log("Mismatched size of tile array");
				Debug.Log(placementBoxArea.min.x + " =< " + position.x + " < " + placementBoxArea.max.x);
				Debug.Log(placementBoxArea.min.y + " =< " + position.y + " < " + placementBoxArea.max.y);
				Debug.Log(arrayIndex + " / " + fastTilePlacementArray.Length);
				
			} else {
				fastTilePlacementArray[arrayIndex] = tile;
			}
		}
		
		// Tell unity to place the tiles in bulk.
		primaryLayer.SetTilesBlock(placementBoxArea, fastTilePlacementArray);
		return positions.Count();
	}
	/*
		@Desc
			Given a point to check and a region, determine if its valid
			An entrance area must look like this
			(Or any 90 degree rotation thereof)
			###
			***
			*@*
			===
			Where "=" is road, "*" is owned by the given region(and for the first row, not suburb road), "#" is not road and "@" is the given position
			The given position must also be owned by the given region
		@Params:
			Vector2Int entracePoint: A 2d position in the world tilemap
			WorldgenRegion region: A region object that the suburb is generating in
		@Returns:
			Vector2Int.zero: If the suburb entrance is not valid
			nonzero Vector2Int: The direction away from the road(valid entrances only)
	*/
	private Vector2Int getValidSuburbEntrance(Vector2Int entrancePoint, WorldgenRegion region){
			// The direction pointing away from the road we are branching off of
			Vector2Int horDir = Vector2Int.left;	
			// The direction parallel to the road we are branching off from
			Vector2Int vertDir = Vector2Int.up;
		
			// Try 4 rotations
			for(int rotateCount = 0; rotateCount < 4; rotateCount++){
				bool fitsTemplate = true;
				
				// To get all the 'columns' in the above diagram
				// We shift in the X axis in inclusive range [-1,1]
				// We also stop early if we don't fit template
				for (int shiftX = -1; shiftX <= 1 && fitsTemplate; shiftX++) {
					// - Calculate some positions
					
					Vector2Int shiftVector = shiftX * horDir; // The relative position of this column
					Vector2Int columnPos = entrancePoint + shiftVector; // The world position of this column
					
					// We move one tile towards the road one from our check pos
					// This is the road position(for this column)
					Vector2Int roadPos = columnPos - vertDir;
					
					// - Check the positions 
					
					// Check the lowest position
					if (primaryLayer.GetTile((Vector3Int) roadPos) != roadTile){
						fitsTemplate = false;
						continue;
					}
					
					// We check the "middle" is owned by us
					for (int verticalOffset = 0; fitsTemplate && verticalOffset < 2; verticalOffset++){
						Vector2Int checkPos = columnPos + vertDir * verticalOffset;
						// If we don't own this position
						if (ownership[checkPos.x,checkPos.y] != region.index) {
							fitsTemplate = false;
							continue;
						}
					}
					// 1 Away can't be suburb road
					if (primaryLayer.GetTile((Vector3Int) columnPos) == suburbTile){
						fitsTemplate = false;
						continue;
					}
					
					// We then check 2 away for it not being road
					if (primaryLayer.GetTile((Vector3Int) (columnPos + vertDir * 2)) == roadTile){
						fitsTemplate = false;
						continue;
					}
				}
				
				// If we found a rotation that fit, we know we are valid
				if (fitsTemplate){
					// We don't have to worry about rotation bias
					// Because only one rotation is valid for a specific entracePoint
					return vertDir;
				}
				
				// Rotate our vectors
				// Rotation direction is arbitrary
				horDir = rotateVector2Int(horDir);
				vertDir = rotateVector2Int(vertDir);
			}
			// If we failed to find a valid rotation, this is not a valid entrance
			return Vector2Int.zero;
	}
	
	private IEnumerator createOtherTiles(){
		yield return null;
	}
	
	private IEnumerator bakeStatics(){
		// First: We must add overlay roads(sidewalks) to the overlay tilemap
		BoundsInt worldBounds = new BoundsInt(new Vector3Int(0, 0, 0), new Vector3Int(maxX, maxY, 1));
		foreach (Vector3Int point in worldBounds.allPositionsWithin)
        {
			TileBase tile = primaryLayer.GetTile(point);
			if(tile == roadTile){
				String name = primaryLayer.GetSprite(point).name; 
				Matrix4x4 rot = primaryLayer.GetTransformMatrix(point);
				TileChangeData newData = new TileChangeData( point, null, Color.white, rot);
				bool needChange = true;
				switch(name) {
					// TODO: Long switch statements are a sign of bad code
					// Consider alternate design
					case "RoadTileset_(A)1":
						newData.tile = overlayTiles[2];
					break;
					
					case "RoadTileset_(A)2":
						// In this case we need to rotate
						newData.transform = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, rot.rotation.eulerAngles.z+180f));
						newData.tile = overlayTiles[2];
					break;
					
					case "RoadTileset_(A)3":
						newData.tile = overlayTiles[2];
					break;
					
					case "RoadTileset_(N)End":
						newData.tile = overlayTiles[3];
					break;
					
					case "RoadTileset_(N)Intersection":
						newData.tile = overlayTiles[2];
					break;
					
					case "RoadTileset_(N)Corner":
						newData.tile = overlayTiles[1];
					break;
					
					case "RoadTileset_(N)Straight":
						newData.tile = overlayTiles[0];
					break;
					
					default: // If we need not add sidewalk, or the name is unknown to us
						needChange = false;
					break;
				}
				if(needChange) {
					overdrawLayer.SetTile(newData,false);
				}

				// TODO: Optimise placement with a mass tile placement
			}
        }
		yield return null;
	}
	
	private IEnumerator createDyanmics(){
		yield return null;
	}
	
	private IEnumerator createOpponent(){
		yield return null;
	}
	
	private IEnumerator startGame(){
		// Scene management
		// Activate out screen
		SceneManager.SetActiveScene(SceneManager.GetSceneByName("OpenWorld"));
		// Unload the main screen
		AsyncOperation loading = SceneManager.UnloadSceneAsync("MainScreen");
		while(!loading.isDone){
			yield return null;
		}
		
		// WARNING: This section includes automated deletion of files
		// We want to scan through the active saved file and remove anything irrelevant. 
		string[] files = Directory.GetFiles(worldLoader.saveLoc);
		foreach(string file in files){
			if (Path.GetExtension(file) == ".sv"){ // ONLY deletes files that end in .sv
				string deleteFile = Path.GetFullPath(file);
				Debug.Log("Deleting: "+deleteFile);
				File.Delete(deleteFile); // Limit damage with nonrecursive delete.
			}
		}
		
		// Once the other scene is unloaded, we can activate audio and event controller
		AudioListener listener = cameraObj.GetComponent<AudioListener>();
		listener.enabled = true;
		eventSystem.SetActive(true);
		
		// We then load the local enviorment
		foreach (var instance in worldLoader.instances){
			instance.activate();
		}
		
	}

	// General helpful methods
	private static Quaternion rotateVector = Quaternion.AngleAxis(90, Vector3.forward); 
	private static Quaternion rotateVectorInv = Quaternion.AngleAxis(-90, Vector3.forward); 
	
		
	public static Vector2Int rotateVector2Int(Vector2Int vec, bool clockwise = false){
		if (clockwise) {
		// It has to be casted around from Vector2Int to Vector3 and back
			return (Vector2Int) Vector3Int.RoundToInt(rotateVector * (Vector3) (Vector3Int) vec);
		} else {
			return (Vector2Int) Vector3Int.RoundToInt(rotateVectorInv * (Vector3) (Vector3Int) vec);
		}
	}	
}