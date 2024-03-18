using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class preserveZombie : MonoBehaviour, preservable
{
	public preservationSystem sys{get{return ZombieManager.instance;}}
	public GameObject obj{get{return gameObject;}}
	
	private Vector2 oldPos; 
	public bool hadOldPos; 
	preservationSystem loadingSystem;
	// Called at the moment of creation, called before start.
	
    void Start()
    {
		oldPos = (Vector2) transform.position; 
		hadOldPos = false; 
    }
	// Position only updates on fixed update.
    void FixedUpdate()
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
