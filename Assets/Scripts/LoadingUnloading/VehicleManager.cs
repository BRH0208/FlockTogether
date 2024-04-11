using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleManager : MonoBehaviour, preservationSystem
{
	public int carCount {get{return GP.i.cars.Length;}}
	public GameObject spawnCar(float x, float y, int carId, float angle, Color color){
		if(carId < 0 || carId > carCount){
			carId = Random.Range(0,carCount);
		}
		Quaternion rotation = Quaternion.Euler(0f,0f,angle);
		GameObject carInstance = Instantiate(GP.i.cars[carId], new Vector3(x, y, 0), rotation,transform);
		Vector2Int position = Vector2Int.FloorToInt(new Vector2(x,y));
		VehicleAI ai = carInstance.GetComponent<VehicleAI>();
		if(ai == null){
			Debug.LogError("Car "+carId+" at ("+x+","+y+") was spawned without an AI");
		}
		ai.recolor(color);
		ai.type = carId; // Cars don't know their ID unless told
		ObjectManager.instance.trackObject(ai,position);
		return carInstance;
	}
	
	// Randomly gets a color from the car colors
    public Color randomCarColor()
    {
		List<Color> carPaint = GP.i.carPaint;
        return carPaint[Random.Range(0,carPaint.Count)];
    }
	
	public class JsonObject
	{
		// Zombies don't store anything besides their position.
		public float r; 
		public float g;
		public float b;
		
		public float x;
		public float y;
		public float rot;
		public int type;
	}
	// Singleton pattern
	public static VehicleManager instance;
	
	// Vehicles have no activation or deactivation
    public void activate(preservable obj){return;}
	public void deactivate(preservable obj){return;}
	
	public string stash(preservable obj){
		VehicleAI ai = obj.obj.GetComponent<VehicleAI>();
		if(ai.commandable){return "{}";}
		
		JsonObject data = new JsonObject();
		data.r = ai.savedColor.r;
		data.g = ai.savedColor.g;
		data.b = ai.savedColor.b;
		data.type = ai.type;
		data.x = obj.obj.transform.position.x;
		data.y = obj.obj.transform.position.y;
		data.rot = obj.obj.transform.eulerAngles.z;
		
		Destroy(obj.obj);// Destroy it
		
		return JsonUtility.ToJson(data);
	}
	
	// Do we want to store anything? We are important so yeah
	public bool wantStash(){return true;}
	
	// Load the entity. The string that was returned when it was stashed is now returned.
    public void load(string json){
		JsonObject data = JsonUtility.FromJson<JsonObject>(json);
		spawnCar(data.x,data.x,data.type,data.rot,new Color(data.r,data.g,data.b));
	}
	
	// The name for this section in the save file. Shorter names prefered for entities with high count.
	public string saveName{get{return "v";}}
	
	// The name to display for readability
	public string displayName{get{return "Vehicle";}}
	
	void Start()
    {
		VehicleManager.instance = this; 
	}
}
