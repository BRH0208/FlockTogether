using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleManger : MonoBehaviour
{
	public List<GameObject> paintedObjects;
	
	void Start()
    {
        randomCarColor(); // TODO: Remove
    }

	// Randomly assigns a color from the car colors
    public void randomCarColor()
    {
		List<Color> carPaint = GP.i.carPaint;
        recolor(carPaint[Random.Range(0,carPaint.Count)]);
    }
	
	// Paints this vehicle with the chosen color
	// Updates the sprite color of every gameobject in the "paintedObjects" list
	private void recolor(Color color)
	{
		foreach (GameObject obj in paintedObjects){
			SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
			if(renderer == null){
				Debug.LogError("Vehicle painted objects includes object without a sprite");
			} else {
				renderer.color = color;
			}
		}
	}
}
