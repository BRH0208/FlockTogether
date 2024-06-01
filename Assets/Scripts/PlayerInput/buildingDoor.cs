using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buildingDoor : interactable
{
	public bool locked;
	public float initialDir;
	public float offset;
	public bool inMotion;
	public bool openDir; 
	public bool isOpen;
	public const float openTime = 0.5f;
	public float timeCount; 
	
	void Start(){
		inMotion = false;
	}
	void Update(){
		if(inMotion){
			timeCount = timeCount + Time.deltaTime;
			if(timeCount > openTime){
				inMotion = false;
				timeCount = openTime;
			}
			Quaternion newRotation = Quaternion.identity;
			if(isOpen){
				newRotation.eulerAngles = new Vector3(0,0,initialDir+90.0f * timeCount/openTime);
			}else{
				newRotation.eulerAngles = new Vector3(0,0,initialDir+90.0f * (1-timeCount/openTime));
			}
			transform.rotation = newRotation;
		}
	}
    // Returns if the interaction was success
    public override bool interact(commandable entity){
		if(isOpen == false){
			initialDir = transform.rotation.eulerAngles.z;
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
}
