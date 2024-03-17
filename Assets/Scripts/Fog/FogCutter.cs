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
	
    // Start is called before the first frame update
    void Update()
    {
		if(cone != null){
			fogScript.removeCone(cone);
		}
        cone = new FogMesh.FogCutterCone((Vector2) transform.position, (Vector2) transform.up * dist, fov, degPerVec);
		fogScript.addCone(cone);
    }
}
