using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogCutter : MonoBehaviour
{
	public FogMesh fogScript;
	public float dist;
	public float fov;
	public float degPerVec;
	public FogMesh.FogCutterCone cone;
	public bool doCone;
	
	// TODO: Optimise by only changing what needs to be changed if it is updated
	// instead of reamaking a cone each frame
	
	void Start(){
		doCone = true;
	}
    // Start is called before the first frame update
    void Update()
    {
		if(cone != null){
			fogScript.removeCone(cone);
		}
		if(!doCone) {return;}
        cone = new FogMesh.FogCutterCone((Vector2) transform.position, (Vector2) transform.up * dist, fov, degPerVec);
		fogScript.addCone(cone);
    }
	
	public void OnSit(Seat seat){
		doCone = false;
	}
	
	public void OnDesit(Seat seat){
		doCone = true;
	}
}
