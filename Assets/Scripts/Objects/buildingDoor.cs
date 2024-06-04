using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buildingDoor : interactable
{
	public bool locked;
	public float initialDir;
	public float offset;
	public bool inMotion;
	public int openDir; // Direction of opening, 1 for counterclockwise, -1 for clockwise
	public bool isOpen;
	public const float openTime = 0.5f;
	private bool firstInteract = false;
	public float timeCount; 
	private Transform interactObj;
	void Start(){
		interactObj = transform.Find("Collider");
		inMotion = false;
	}
	void Update(){
		if(inMotion){
			timeCount = timeCount + Time.deltaTime;
			if(timeCount > openTime){
				inMotion = false;
				timeCount = openTime;
			}
			transform.localEulerAngles = new Vector3(0.0f,0.0f,getAngle());
		}
	}
	private float getAngle(){
		if(isOpen){
			return (initialDir+openDir*90.0f * timeCount/openTime);
		}else{
			return (initialDir+openDir*90.0f * (1-timeCount/openTime));
		}
	}
    // Returns if the interaction was success
    public override bool interact(commandable entity){
		firstInteract = true;
		if(isOpen == false){
			initialDir = transform.localEulerAngles.z;
			
			Vector2 relativePos = entity.getPos() -(Vector2) interactObj.position;
			if(transform.eulerAngles.y == 180){
				relativePos = new Vector2(-relativePos.x,relativePos.y);
			}
			// TODO: Appears to not account for flipping causing doors on some tiles to always
			// open in the wrong direction
			float angle = Mathf.Atan2(relativePos.y,relativePos.x);
			if(Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad - angle) > 0){
				openDir = 1;
			}else{
				openDir = -1;
			}
		}
		isOpen = !isOpen;
		inMotion = true;
		timeCount = 0.0f;
		return true;
	}
	
	// Is the selected object capable of interacting with this object?
    public override bool canInteract(commandable entity){
		return entity.hasTag("hands") && !locked && !inMotion;
	}
	
	// JSON loading helpers
	public class JsonData
	{
		public bool isOpen;
		public bool openDir; // True for 1, false for -1
		public Vector2 pos;
		public float initialDir;
	}
	
	public void loadJson(string json_data){
		firstInteract = true;
		JsonData data = JsonUtility.FromJson<JsonData>(json_data);
		isOpen = data.isOpen;
		initialDir = data.initialDir;
		openDir = data.openDir ? 1 : -1;
		transform.localPosition = (Vector3) data.pos;
		// Place correctly
		timeCount = openTime;
		transform.localEulerAngles = new Vector3(0.0f,0.0f,getAngle());
	}
	
	public string saveJson(){
		JsonData doorJObj = new JsonData();
		if(firstInteract == false){
			initialDir = transform.localEulerAngles.z;
		}
		doorJObj.initialDir = initialDir;
		doorJObj.isOpen = isOpen;
		doorJObj.openDir = ((openDir == 1) ? true : false);
		doorJObj.pos = (Vector2) transform.localPosition;
		return JsonUtility.ToJson(doorJObj);
	}
}
