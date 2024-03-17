using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerZombieAlert : MonoBehaviour
{
	GameObject player;
	private static RaycastHit2D[] rayBuffer;
	private const int bufferSize = 1; // We only care about the first hit, so we have a cap of 1.

	public void Start(){
		// Find the player
		player = this.transform.parent.gameObject;
		if(player == null){
			Debug.LogError("Player not found for zombie alerter");
		}
		rayBuffer = new RaycastHit2D[1];
	}
	
	public static void alertAt(ZombieAI ai, Vector2 pos,Vector2 otherPos, float amt){
		Vector2 relativeVector = otherPos-pos;
		int hitCount = Physics2D.Raycast(pos, relativeVector,GP.i.alertBlockFilter, rayBuffer,relativeVector.magnitude);
		if (hitCount == 0){
			ai.alert((Vector2) pos,amt);
		}
	}
	
	void OnTriggerStay2D(Collider2D col)
    {
		ZombieAI ai = col.gameObject.GetComponent<ZombieAI>();
		if(ai != null){
			Vector2 playerPos = player.transform.position;
			Vector2 zombiePos = col.gameObject.transform.position; 
			if(ai.wouldAlert(playerPos,1.0f)){
				alertAt(ai,playerPos,zombiePos,1.0f);
			}
		}
	}
}
