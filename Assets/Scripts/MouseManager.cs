using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseManager : MonoBehaviour
{
	
	[SerializeField] private LayerMask rayLayer;
	
	public List<GameObject> humans; 
	public Camera cam;
	public GameObject selectedObj;
	public void Update(){
		if (Input.GetMouseButtonDown(1))
        {
			Vector2 mousePos = Mouse.current.position.ReadValue();
			Vector2 clickPos = (Vector2) cam.ScreenToWorldPoint(mousePos);
			float minDist = (clickPos-(Vector2) humans[0].transform.position).sqrMagnitude;
			GameObject minPlayer = humans[0];
			for (int i = 1; i < humans.Count; i++){
				float dis = (clickPos-(Vector2) humans[i].transform.position).sqrMagnitude;
				if(dis < minDist){
					minDist = dis;
					minPlayer = humans[i];
				}
			}
			HumanAI ai = minPlayer.GetComponent<HumanAI>();
			if(ai == null){
				Debug.Log("Human " +minPlayer+" lacks a brain");
				return;
			}
			ai.selected();
			selectedObj = minPlayer;
		}
		// This ordering is intentional to allow double mouse buttons to grab and command. 
		if (true)//Input.GetMouseButtonDown(0))
        {
			if(selectedObj != null){
				HumanAI ai = selectedObj.GetComponent<HumanAI>();
				if(ai == null){
					Debug.Log("Selected human " +selectedObj+" lacks a brain");
					return;
				}
				
				Vector3 mousePosition = Mouse.current.position.ReadValue();
				Ray ray = cam.ScreenPointToRay(mousePosition);
				if (Physics.Raycast(ray, out RaycastHit hit, 1.1f,rayLayer)) {
					
				}else{
					// If we hit nothing, the default behavior is to move
					Vector2 clickedPos = (Vector2) cam.ScreenToWorldPoint(mousePosition);
					ai.commandMove(clickedPos);
				}
			}
		} 
	}
}
