using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogCutter : MonoBehaviour
{
	public float dist;
	public float fov;
	public float degPerVec;
	public FogMesh.FogCutterCone cone;
	public bool doCone = false; // Set as fast-default
	public Vector2 oldUp;
	
	// TODO: Optimise by only changing what needs to be changed if it is updated
	// instead of reamaking a cone each frame
	
	void Start(){
		oldUp = transform.up;
	}
    // Start is called before the first frame update
    void Update()
    {
		if(!doCone && cone == null){return;} // If we should do nothing, do nothing
		FogMesh fogScript = FogMesh.instance;
		if(!doCone && cone != null){ // If we should do nothing but have a cone still, destroy it
			fogScript.removeCone(cone);
			cone = null;
			return;
		}
		// Decide if we need to remake our cone
		if(cone != null && (cone.fov != fov || cone.degPerVec != degPerVec || (fov != 180 && (Vector2) transform.up != oldUp))){ // TODO: Make helper function
			fogScript.removeCone(cone);
			cone = null; // Throw out our cone, it is out of date
		}
		// If we should have a cone, but don't have it yet
		if(cone == null){ 
			cone = new FogMesh.FogCutterCone((Vector2) transform.position, (Vector2) transform.up * dist, fov, degPerVec);
			fogScript.addCone(cone);
			oldUp = transform.up;
			return;
		}
		// Update our cone
		cone.center = (Vector2) transform.position;
		cone.range = dist;
		
		
    }
	// Handle sit calls with appropriate enable/disable
	public void OnSit(Seat seat){disable();}
	public void OnDesit(Seat seat){enable();}
	
	public void enable(){doCone = true;}
	
	public void disable(){doCone = false;}
	
	void OnDestroy()
    {
		if(cone != null && FogMesh.instance != null){
			FogMesh.instance.removeCone(cone);
		}
    }
}
