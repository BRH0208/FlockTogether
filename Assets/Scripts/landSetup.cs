using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/*
	A simple script that places all of the "island" tiles.  
	These tiles are on their own tile because rule tiles are used for rendering. 
	If a tile is not placed, that is assumed to be only water
	If a tile is placed, it is assumed to be atleast partially land(though it may have water portions)
	The generation is done through Unity's perlin noise.
*/
public class landSetup : MonoBehaviour
{
	public TileBase landTile;
	
    // Generate tells the world to generate to a specific size
    public void Generate(int maxX, int maxY)
    {
		// How strong should noise be with respect to the distance weights.
		// Smaller values are less noisy
		float noiseRatio = 1f;
		// Affects the size of the noise. Larger values result in smaller "bumps"
		float noiseScale = 8f;
		
		// We count the number of tiles so they can be logged
		int count = 0;
		gameObject.GetComponent<Tilemap>().ClearAllTiles();
		
		// Regular World Generation
		float perlinOffsetX = Random.Range(-65536,65536);
		float perlinOffsetY = Random.Range(-65536,65536);
		
		// Loop over all tiles
		for(int x = 0; x < maxX; x++){
			for(int y = 0; y < maxY; y++){
				
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
					gameObject.GetComponent<Tilemap>().SetTile(pos,landTile);
					
					// Add this tile to the count
					count++;
				}
			}
		}
		
		Debug.Log(count + " tiles placed, " + (count)/6806.25f + " square miles" );
		
    }

    // A test world for making sure all land tiles can be placed
	// Places every possible 3x3 grid of tiles in sequence
	// So long as there is a sprite for all the tiles placed in the test, 
	// we have enough sprites for the game
	void drawTest(){
		int count = 0;
		
		// There are 256 possibilites, we represent them in a grid
		for(int i = 0; i < 16; i++){
			for(int j = 0; j < 16; j++){
				// Place "center" position always
				count++;
				Vector3Int pos = new Vector3Int(i*4,j*4,0);
				gameObject.GetComponent<Tilemap>().SetTile(pos,landTile);
				
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
							gameObject.GetComponent<Tilemap>().SetTile(pos,landTile);
						}
					}
				}
			}
		}
	}
}
