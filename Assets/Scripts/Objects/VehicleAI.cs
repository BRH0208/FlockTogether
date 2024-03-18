using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAI : MonoBehaviour, preservable
{
	public Seat driverSeat;
	public List<GameObject> paintedObjects;
	void Start()
    {
        randomCarColor();
    }
	
	// The system we use to preserve ourselves
	public preservationSystem sys{get{return VehicleManager.instance;}}
	public GameObject obj{get{return gameObject;}} // Our vehicle
	
	// Returns true if the current car is capable of movement
	public bool canMove(){
		return driverSeat.occupied; 
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
				color.a = 1.0f; // no invisible paint
				renderer.color = color;
			}
		}
	}
}
