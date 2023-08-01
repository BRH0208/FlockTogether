using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
	public GameObject ocean;
	public GameObject camera;
	public GameObject minimap;
	
	public TileBase roadTile;
	public TileBase landTile;
	
	// Configuration variables
	public int maxX = 3200; // Measured in tiles(64 feet)
	public int maxY = 3200;
	public float roadRatio = 0.06f;
	public float roadVariance = 0.2f;
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
	Dictionary<int,Vector2Int> regionOrigins;
	HashSet<Vector2Int> roadPositions;
	
	int minHeight = 0;
	int maxHeight = 0;
	Tilemap landLayer;
	Tilemap primaryLayer;
	bool inProgress = false;
	float currentProgress = 0.0f; // The unweighted value of all previous stage steps
	float stageWeight = 0.0f; // The weight of the current stage, used for calculating progress
	float stageProgress = 0.0f; // The progress of a current substage of world generation
	float totalWeight = 0.0f; // The total cost of worldgen
	string stageName = "Waiting to begin";
	// Populated during road generation 
	// denotes if a spot is a full tile that can support a road
	bool[,] emptyField;
	
	// The sprite for "empty land" found during road generation
	// Always the sprite at the center of the layer 0 tilemap
	Sprite openSprite;
	
	public Sprite debugSprite;
	
	public (string,float) getProgress(){
		if(!inProgress) {return ("Waiting to begin",0.0f);}
		return (stageName,(currentProgress + stageWeight * stageProgress) / totalWeight);
	}
	
	void Start(){
		instance = this;
	}
	
    // Called to begin world generation
    public IEnumerator Generate()
    {	
		Debug.Log("Generation Called");
		inProgress = true;
		
		// Go through all worldgesn steps, each asynchronous
		List<(string,IEnumerator,float)> worldGenSteps = new List<(string,IEnumerator,float)>();
		worldGenSteps.Add(("Preparing for World Generation",preGenerationSteps(),0.1f)); // The basic steps that have to happen first
		worldGenSteps.Add(("Making an island",createIsland(),5.0f)); // Place the island
		worldGenSteps.Add(("Adding large roads",createRoads(),1.0f)); // Place major(4 lane) roads
		worldGenSteps.Add(("Creating city blocks",assignPlots(),1.0f)); // Make building plots near roads
		worldGenSteps.Add(("Filling city blocks",createBuildings(),0.0f)); // Create buildings in the plots
		worldGenSteps.Add(("Adding other structures",createOtherTiles(),0.0f)); // Place tiles not in plots
		worldGenSteps.Add(("Finilizing static objects",bakeStatics(),0.0f)); // Finilize static objects
		worldGenSteps.Add(("Adding dynamic objects",createDyanmics(),0.0f)); // Add dynamic objects
		worldGenSteps.Add(("Adding too many zombies",createUndead(),0.0f)); // Add zombies
		worldGenSteps.Add(("Preparing to start the game",startGame(),0.1f)); // Start the game
		
		int i = 1;
		totalWeight = 0.0f;
		// Count all the worldgen weights
		foreach ((string,IEnumerator,float) worldGenStage in worldGenSteps){
			totalWeight += worldGenStage.Item3;
		}
		
		// Iterate through all worldgen steps
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
		
		inProgress = false; // There are no more steps executed after this point
    }
	
	private IEnumerator preGenerationSteps(){
		layers = new Dictionary<int,Tilemap>();
		Random.InitState(seed); // Start the randomizer
		
		// Resize and resposition all elements
		ocean.transform.position = new Vector3(maxX/2, maxY/2, 0);
		ocean.transform.localScale = new Vector3(maxX, maxY, 0);
		noise.transform.position = new Vector3(maxX/2, maxY/2, 0);
		noise.GetComponent<SpriteRenderer>().size = new Vector3(maxX, maxY, 0);
		camera.transform.position = new Vector3(maxX/2, maxY/2, -1);
		minimap.transform.position = new Vector3(maxX/2, maxY/2, -1);
		minimap.GetComponent<Camera>().orthographicSize = Mathf.Max(maxX,maxY)/2.0f;
		// Manage layer 0
        GameObject LandLayer = GridObj.transform.Find("LandLayer").gameObject; // Find it
		layers.Add(0,LandLayer.GetComponent<Tilemap>()); // Store the tilemap(as reference)
		
		// We must create layer #1 for the roads and first floors manually. 
		// The rest are added by buildings themselves as needed
		addLayer(); 
		
		// We give some layers special names because we use them so much
		landLayer = (Tilemap) layers[0];
		primaryLayer = (Tilemap) layers[1];
		
		Debug.Log("Basic steps complete");
		yield return null;
	}
	// Tells the world to generate to a specific size
    private IEnumerator createIsland()
    {
		// How strong should noise be with respect to the distance weights.
		// Smaller values are less noisy
		float noiseRatio = 1f;
		// Affects the size of the noise. Larger values result in smaller "bumps"
		float noiseScale = 8f;
		
		// We count the number of tiles so they can be logged
		int count = 0;
		landLayer.ClearAllTiles();
		
		// Regular World Generation
		float perlinOffsetX = Random.Range(-65536,65536);
		float perlinOffsetY = Random.Range(-65536,65536);
		
		TileBase[] islandTileArray = new TileBase[maxX * maxY];
		int tileArrayIndex = 0;
		
		// Loop over all tiles
		for(int y = 0; y < maxY; y++){
			if(y % 100 == 0){
				stageProgress = (((float) y) / maxY)/2; // Update progress
				yield return null;  // We give control back to unity, for a bit
			}
			for(int x = 0; x < maxX; x++){
				
				// Get base perlin noise(Big clumps)
				float noise = Mathf.PerlinNoise(
					((float) x) / maxX * noiseScale + perlinOffsetX,
					((float) y) / maxY * noiseScale + perlinOffsetY);
				// noise = Mathf.Clamp(noise,0f,1f); // This is approximatly true, removed for effeciency
				
				// Get weight factor for distance
				float distance = (Vector2.Distance(
					new Vector2(0.5f,0.5f), // From the center
					new Vector2((float) x / maxX, (float) y / maxY) // To the position(normalised)
					)) * 2 - 1; // We adjust the range to be [-1,0]
				// distance = Mathf.Clamp(distance,-1f,1f); // Distance is clamped to this
				
				// We measure our noise to zero, < 0 is land, > 0 is water
				if(distance + noiseRatio * noise < 0){
					// Place a land tile
					Vector3Int pos = new Vector3Int(x,y,0);
					islandTileArray[tileArrayIndex] = landTile;
					
					// Add this tile to the count
					count++;
				}
				
				// What position we are in in the 1d array
				tileArrayIndex++;
			}
		}
		
		stageProgress = (float) 0.5f; // Update progress
		yield return null;  // We give control back to unity, for a bit
		
		// Actually place the tiles using the mass place command
        landLayer.SetTilesBlock(new BoundsInt(0,0,0,maxX,maxY,1), islandTileArray);
		
		Debug.Log(count + " tiles placed, " + (count)/6806.25f + " square miles" );
		
    }

    // A test world for making sure all land tiles can be placed
	// Places every possible 3x3 grid of tiles in sequence
	// So long as there is a sprite for all the tiles placed in the test, 
	// we have enough sprites for the game
	private void drawTest(){
		int count = 0;
		
		// There are 256 possibilites, we represent them in a grid
		for(int i = 0; i < 16; i++){
			for(int j = 0; j < 16; j++){
				// Place "center" position always
				count++;
				Vector3Int pos = new Vector3Int(i*4,j*4,0);
				landLayer.SetTile(pos,landTile);
				
				// For all other positions in a 3x3 grid around the center tile, maybe place tiles
				int subCount = 1; // The current multiple of the count used to determine placement
				for(int x = -1; x <= 1; x++){
					for(int y = -1; y <= 1; y++){
						if(x == 0 && y == 0){
							continue; // we ignore the center tile(it was already placed)
						}
						
						subCount *= 2;
						if(count % subCount < subCount / 2){ // This pattern iterates through possibilities
							// If a tile should be made, place it
							pos = new Vector3Int(i*4 + x,j*4 + y,0);
							landLayer.SetTile(pos,landTile);
						}
					}
				}
			}
		}
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
	private IEnumerator createRoads(){
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
		float tileCount = (float) (maxX * maxY);
		int desiredRoads = (int) Random.Range((int) (tileCount * roadRatio * (1.0f - roadVariance)),(int) (tileCount * roadRatio * (1.0f + roadVariance)));
 		float priorityBias = 0.95f; // percent chance of using priority set
		
		// We loop until we hit our number of roads
		// or we run out of positions
		while(roadCount < desiredRoads && regularExpansion.Count != 0){
			// Allow for an asynch break at quarter incriments
			if(roadCount == desiredRoads/4 || roadCount == desiredRoads/2 || roadCount == (desiredRoads * 3) / 4){
				stageProgress = (float) roadCount / desiredRoads;
				yield return null;
			}
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
		roadPositions = regularExpansion; // We save the road positions for later
		
		Debug.Log(roadCount + "/" + desiredRoads + " roads");
		
	}
	
	// Divide the map into navicable blocks
	private IEnumerator assignPlots(){
		// This designates which plot owns each tile(by index)
		int[,] ownership = new int[maxX,maxY];
		
		// We keep track of one tile in each region so we can find the regions later
		regionOrigins = new Dictionary<int,Vector2Int>();
		
		int currentIndex = 1; // What is the current highest ownership value 
		Vector3Int posVec3;
		
		// Region creation by flood filling
		// This works well on wierd, but closed shapes
		
		for (int x = 0; x < maxX; x++){
			// We take a break halfway through
			if(x == maxX/2){
				stageProgress = 0.25f;
				yield return null;
			}
			
			// check all y positions
			for (int y = 0; y < maxY; y++){
				// We try to "fill on each tile"
				posVec3 = new Vector3Int(x,y,0); // Tilemaps want 3d vectors
				
				if(!fillPlot(ownership,x,y,currentIndex)){
					// If the fill returned false, then we know a region was made. 
					regionOrigins.Add(currentIndex,new Vector2Int(x,y)); // Keep track of the new region
					currentIndex++; // The next region will have an index 1 higher
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
			if(roadsEvaluated == roadPositions.Count / 2){
				stageProgress = 0.75f; // We are halfway complete with the second part of region filling, so 75%
				yield return null;
			}
			posVec3 = new Vector3Int(pos.x,pos.y,0);
			
			if (!validOpen(ownership,pos.x,pos.y)) {
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
				if(primaryLayer.GetTile(posVec3 + Vector3Int.up) == roadTile && Random.Range(0.0f,1.0f) < 0.5f){
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
				if(primaryLayer.GetTile(posVec3 + Vector3Int.right) == roadTile && Random.Range(0.0f,1.0f) < 0.5f){
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
					|| !validOpen(ownership, fillPos.x,fillPos.y)) // Exit Case #2: If we don't have at least 1 open tile in front
				{
					// If we are moving left, turn around
					if(dir == 1){
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
					if(expandOffset >= expandDisMax){ // We would go to far
						break;
					}
					
					// Get a position offset away from the anchor
					Vector3Int outwardsFillPos = fillPos - expandOffset * anchorDir;
					// If the position is not valid, we stop for now.
					if(!validOpen(ownership,outwardsFillPos.x,outwardsFillPos.y)){
						break;
					}
					ownership[outwardsFillPos.x,outwardsFillPos.y] = currentIndex;
					expandOffset++; // Move further out
				}
				
				// We move leftward or rightward at the end of the loop
				roadOffset+=dir;
			}
			currentIndex++;
		}
		
		
		// For debugging, we add colors to clumps. This is slow for large sizes
		// Only use on small map sizes(300x300 or smaller)
		/*
		List<Color> colors = new List<Color>();
		for(int i = 0; i <= currentIndex; i++){
			colors.Add(new Color(
			Random.Range(0.0f,1.0f),
			Random.Range(0.0f,1.0f),
			Random.Range(0.0f,1.0f),
			1.0f));
		}
		

		List<GameObject> tempobjects = new List<GameObject>();
		for(int x = 0; x < maxX; x++){
			for(int y = 0; y < maxY; y++){
				if(ownership[x,y] > 0){
					Vector3 pos = new Vector3(x+0.5f,y+0.5f,0);
					//spawn object
					GameObject objToSpawn = new GameObject("tempcolor:"+ownership[x,y]);
					//Add Components
					objToSpawn.AddComponent<SpriteRenderer>();
					objToSpawn.transform.position = pos;
					objToSpawn.GetComponent<SpriteRenderer>().sprite = debugSprite;
					objToSpawn.GetComponent<SpriteRenderer>().color = colors[ownership[x,y]];
					objToSpawn.transform.parent = GridObj.transform;
					tempobjects.Add(objToSpawn);
				}
			}
		}
		*/
		Debug.Log(currentIndex + " regions created");
	}
	
	List<Vector2Int> trackedELem;
	// Starting at a position claim all available connected positions
	// A recursive helper for assignPlots
	private bool fillPlot(int[,] ownership,int x, int y, int ownervalue, int depth = 0){
		if(depth == 0){
			trackedELem = new List<Vector2Int>();
		}
		
		// Base case
		Vector3Int posVec3 = new Vector3Int(x,y,0);
		if(depth > 750 || ownership[x,y] == -1){
			return true;
		}
		// We stop on roads, claimed tiles and non-open land tiles
		if(!validOpen(ownership,x,y,0)){
			return depth == 0;
		}
		
		// Actually claim the tile
		ownership[x,y] = ownervalue;
		trackedELem.Add(new Vector2Int(x,y));
		
		
		// Recurse up
		if(fillPlot(ownership, x, y+1, ownervalue, depth + 1)){
			while (trackedELem.Count != 0){
				ownership[trackedELem[0].x,trackedELem[0].y] = -1;
				trackedELem.RemoveAt(0);
			}
			return true;
		}
		
		// Recurse left
		if(fillPlot(ownership, x-1, y, ownervalue, depth + 1)){
			while (trackedELem.Count != 0){
				ownership[trackedELem[0].x,trackedELem[0].y] = -1;
				trackedELem.RemoveAt(0);
			}
			return true;
		}
		
		// Recurse down
		if(fillPlot(ownership, x, y-1, ownervalue, depth + 1)){
			while (trackedELem.Count != 0){
				ownership[trackedELem[0].x,trackedELem[0].y] = -1;
				trackedELem.RemoveAt(0);
			}
			return true;
		}
		
		// Recurse right
		if(fillPlot(ownership, x+1, y, ownervalue, depth + 1)){
			while (trackedELem.Count != 0){
				ownership[trackedELem[0].x,trackedELem[0].y] = -1;
				trackedELem.RemoveAt(0);
			}
			return true;
		}
		return false;
	}
	
	// Is a specific area claimed with a specific region, not a road and not coast/water?
	private bool validOpen(int[,] ownership,int x, int y, int ownershipValue = -1) {
		Vector3Int posVec3 = new Vector3Int(x,y,0);
		return ownership[x,y] == ownershipValue && // Only for specific region flags
			primaryLayer.GetTile(posVec3) != roadTile && // Not a road
			landLayer.GetSprite(posVec3) == openSprite; // open grass
	}
	
	// Place 
	private IEnumerator createBuildings(){
		yield return null;
	}
	
	private IEnumerator createOtherTiles(){
		yield return null;
	}
	
	private IEnumerator bakeStatics(){
		yield return null;
	}
	
	private IEnumerator createDyanmics(){
		yield return null;
	}
	
	private IEnumerator createUndead(){
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
	}
}