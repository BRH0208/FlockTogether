using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class cameraController : MonoBehaviour
{
	Camera cam;
	Camera miniCam;
	public float scrollWeight = 0.01f;
    // Start is called before the first frame update
    public void Start()
    {
		cam = GetComponent<Camera>();
		for (int i = 0; i < transform.childCount; i++){
			GameObject obj = transform.GetChild(i).gameObject;
			if(obj.tag == "MinimapCam"){
				miniCam = obj.GetComponent<Camera>();
			}
		}
    }

    // Update is called once per frame
    public void Update()
    {
		Vector2 cameraPos = (Vector2) transform.position;
        Vector2 mousePos = Mouse.current.position.ReadValue();
		Vector2 mouseDelta = Mouse.current.delta.ReadValue();
		Vector2 clickPos = (Vector2) cam.ScreenToWorldPoint(mousePos);
		Vector2 worldDelta = (Vector2) cam.ScreenToWorldPoint(mousePos + mouseDelta) - clickPos;
		
		float scroll = Mouse.current.scroll.y.ReadValue();
		if(scroll != 0.0f){
			cam.orthographicSize = cam.orthographicSize* Mathf.Exp(-scroll*scrollWeight);
			Vector2 newClickPos = (Vector2) cam.ScreenToWorldPoint(mousePos);
			Vector2 movement = newClickPos - clickPos;
			cameraPos -= movement;
		}
		// Offset by delta if scroll button is down
		if(Mouse.current.middleButton.isPressed)
		{
			cameraPos -= worldDelta * 2f;
		}
		transform.position = new Vector3(cameraPos.x,cameraPos.y,-1f);
    }
}
