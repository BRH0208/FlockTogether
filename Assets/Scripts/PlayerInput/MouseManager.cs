using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseManager : MonoBehaviour
{
	
	[SerializeField] private LayerMask rayLayer;
	private RaycastHit[] rayHitBuffer;
	private const int bufferSize = 100; // Its unlikely there will be more than 100 objects in a selection
	public static List<commandable> commandedObjects; 
	public Camera cam;
	public commandable selectedObj;
	
	public static void track(commandable comm){
		if(commandedObjects == null){
			commandedObjects = new List<commandable>();
		}
		commandedObjects.Add(comm);
	}
	public static void untrack(commandable comm){
		if(commandedObjects == null){return;} // its okay, if it doesn't exist this was probally removing itself as a precaution
		commandedObjects.Remove(comm);
	}
	
	
	public void Start(){
		rayHitBuffer = new RaycastHit[bufferSize];
	}
	
	private bool isValid(PlayerInteract interact){
		return interact != null
			&& interact.inter != null
			&& interact.inter.canInteract(selectedObj);
	}
	private PlayerInteract nearestInteract(Vector3 pos){
		// Raycast
		Ray ray = cam.ScreenPointToRay(pos);
		int hits = Physics.RaycastNonAlloc(ray, rayHitBuffer, 1.1f,rayLayer);
		if (hits >= bufferSize){
			Debug.LogError("Player interact buffer exceeded with "+hits+"/"+bufferSize+" hits");
			return null; // Exit due to buffer
		}
		if (hits == 0) {
			return null; // Exit due to hiting nothing
		}
		
		// We first find a single valid entity
		int offset;
		GameObject interactedObj = null;
		PlayerInteract interact = null;
		for(offset = 0; !isValid(interact); offset++){
			if(offset >= bufferSize || offset >= hits){ // If we run out of objects. 
				return null; // There is not a single valid entity
			}
			interactedObj = rayHitBuffer[offset].transform.gameObject;
			interact = interactedObj.GetComponent<PlayerInteract>();
		}
		
		// We loop over the remaning objects and select only the closest valid object.
		
		// These are squared distances
		float minDist = (rayHitBuffer[0].point - rayHitBuffer[0].transform.position).sqrMagnitude;
		
		for (int i = offset + 1; i < hits && i < bufferSize; i++){
			GameObject obj = rayHitBuffer[i].transform.gameObject;
			float objDist = (rayHitBuffer[0].point - rayHitBuffer[0].transform.position).sqrMagnitude;
			PlayerInteract i_interact = obj.GetComponent<PlayerInteract>();
			if(objDist < minDist && isValid(i_interact)){
				minDist = objDist;
				interactedObj = obj;
				interact = i_interact;
			}
		}
		return interact;
	}
	
	public void OnSneak(){
		makeCommand(commandable.Mode.Sneak);
	}
	public void OnSprint(){
		makeCommand(commandable.Mode.Sprint);
	}
	public void OnCommand(){
		makeCommand(commandable.Mode.Normal);
	}
	public void OnSelect(){
		if (commandedObjects.Count == 0){return;} // We do nothing if we aren't tracking anything
		Vector2 mousePos = Mouse.current.position.ReadValue();
		Vector2 clickPos = (Vector2) cam.ScreenToWorldPoint(mousePos);
		float minDist = (clickPos-(Vector2) commandedObjects[0].obj.transform.position).sqrMagnitude;
		commandable minObj = commandedObjects[0];
		for (int i = 1; i < commandedObjects.Count; i++){
			float dis = (clickPos-(Vector2) commandedObjects[i].obj.transform.position).sqrMagnitude;
			if(dis < minDist){
				minDist = dis;
				minObj = commandedObjects[i];
			}
		}
		
		if(selectedObj != null){
			selectedObj.deselected();
		}
		if(minObj == selectedObj){
			selectedObj = null;
		} else {
			selectedObj = minObj;
			selectedObj.selected();
		}
	}
	public void makeCommand(commandable.Mode mode){
		if(selectedObj != null){
			Vector3 mousePosition = Mouse.current.position.ReadValue();
			PlayerInteract interact = nearestInteract(mousePosition);
			if(interact == null){
				// If we hit nothing, the default behavior is to move
				Vector2 clickedPos = (Vector2) cam.ScreenToWorldPoint(mousePosition);
				selectedObj.commandEmpty(clickedPos,mode);
			} else {
				selectedObj.commandInteractable(interact.inter,mode);
			}
		}
	}
}
