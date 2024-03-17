using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class preserveZombie : preservable
{
	private Vector2 oldPos; 
	public bool hadOldPos; 
	
	// Called at the moment of creation, called before start.
	public void creation() {
		sys = ZombieManager.instance;
	}
    void Start()
    {
		oldPos = (Vector2) transform.position; 
		hadOldPos = false; 
    }

    void Update()
    {
        Vector2 pos = transform.position;
		if(!hadOldPos || ObjectManager.instance.checkMovement(this,oldPos,pos)){
			pos = oldPos;
		} else {
			oldPos = pos;
			hadOldPos = true;
		}
    }
}
