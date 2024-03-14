using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerZombieAlert : MonoBehaviour
{
	public static PlayerZombieAlert instance;
	[SerializeField] public LayerMask alertBlockLayer;
	GameObject player;
	public void Start(){
		instance = this;
		// Find the player
		player = this.transform.parent.gameObject;
	}
	
	public void alertAt(ZombieAI ai, Vector2 pos,Vector2 otherPos, float amt){
		Vector2 relativeVector = otherPos-pos;
		RaycastHit2D hit = Physics2D.Raycast(pos, relativeVector,relativeVector.magnitude,alertBlockLayer);
		if(!(hit.collider == null)){
			//Debug.DrawLine((Vector3) pos,(Vector3) otherPos);
			ai.alert((Vector2) pos,amt);
		}
	}
	
	void OnTriggerStay2D(Collider2D col)
    {
		ZombieAI ai = col.gameObject.GetComponent<ZombieAI>();
		if(ai != null){
			Vector2 playerPos = player.transform.position;
			Vector2 zombiePos = col.gameObject.transform.position; 
			alertAt(ai,playerPos,zombiePos,1.0f);
		}
	}
}
