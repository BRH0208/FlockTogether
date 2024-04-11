using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleObscure : MonoBehaviour
{
	GameObject grandparent;
    VehicleAI ai;
	Collider2D[] colList;
	void Start()
    {
        grandparent = transform.parent.parent.gameObject;
		ai = grandparent.GetComponent<VehicleAI>();
		colList = GetComponents<Collider2D>();
    }

    // TODO: This does not need to be checked each frame
    void Update()
    {
		foreach (Collider2D col in colList){
			col.enabled = !ai.commandable;
		}
    }
}
