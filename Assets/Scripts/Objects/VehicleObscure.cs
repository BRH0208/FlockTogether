using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleObscure : MonoBehaviour
{
	GameObject grandparent;
    VehicleAI ai;
	Collider2D col;
	void Start()
    {
        grandparent = transform.parent.parent.gameObject;
		ai = grandparent.GetComponent<VehicleAI>();
		col = GetComponent<Collider2D>();
    }

    // TODO: This does not need to be checked each frame
    void Update()
    {
        col.enabled = !ai.commandable;
    }
}
