using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
public class ZombieAI : MonoBehaviour
{
	// Zombies share a buffer. The buffer is used directly after it is called and all zombies are handles on the
	// same thread(because unity).
	public const int bufferSize = 1000;
	public static readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[bufferSize];
	
	public static float speed = 4.00f;
	public static float activeSlow = 30.0f;
	public static float passiveSlow = 0.0f;
	public static float sleepRatio = 0.1f;
	public static float rewanderTime = 10.0f;
	public static float screamRadius = 0.10f;
	public static float fervorLossRate = 0.05f;
	public static float fervorLostPerCall = 0.25f;
	public static float avgCallDelay = 0.20f;
	public static float fervorSpeed = 0.25f;
	public static float eyeDelay = 8f;
	public static float minEyeDelay = 2f; // Eye delay at max fervor
	
	public bool special;
	private Vector2 moveDir; 
	public bool tired = false; 
	private bool hasEyes = true; // Tracks if we react to alerts and collisions or if we are "stunned";
	public bool needFervorUpdate; // Is the next non-fervor call of wanderUpdate just after a fevor?
	private bool awake = false;
	private Rigidbody2D rb;
	private float zombieSeed;
	private bool hasFervor;
	private Coroutine fervorUpdate;
	private bool calcFervorFrame; // Have we already calculated fervor this frame?
	private float _fervor;
	private float lastFervorUpdate;
	private Vector2 fervorDirection;
	public float nextFervorCall;
	
	private void rotateToVec(Vector2 vec){
		float angle = Mathf.Rad2Deg * Mathf.Atan2(vec.y, vec.x);
		transform.rotation = Quaternion.Euler(0,0,angle-90.0f);
	}
	public void Update(){
		calcFervorFrame = false; 
	}
	

	// Wake this zombie, so it starts doing things
	public void wake(){
		awake = true;
		tired = false;
		if(Random.value < sleepRatio/(sleepRatio+1)){
			tired = true;
			Invoke("wanderUpdate", Random.Range(0.0f,rewanderTime*sleepRatio));
		}else{
			Invoke("wanderUpdate", Random.Range(0.0f,rewanderTime));
		}
	}
	
	public void deactivate(){
		awake = false;
		rb.velocity = Vector2.zero;// stop moving
	}
	
    void Start()
    {
		rb = GetComponent<Rigidbody2D>();
		zombieSeed = Random.Range(0.0f,2.0f*Mathf.PI);
		_fervor = 0.0f;
    }
	
	
	private void wanderUpdate(){
		_wanderUpdate(true);
	}
	// Invokes "call fervor" if appropriate
	private void recallFervor(){
		if(!awake){return;}
		float fervor = getFervor();
		float callDelay = avgCallDelay / fervor;
		float fervorLostThisCall = fervorLostPerCall * fervor;
		float recallTime = Time.time + callDelay;
		if (fervor - callDelay * fervorLossRate > fervorLostThisCall && 
		   (nextFervorCall == 0.0f || recallTime < nextFervorCall)
		){
			if(nextFervorCall != 0.0f){
				StopCoroutine(fervorUpdate);
			}
			nextFervorCall = recallTime;
			fervorUpdate = StartCoroutine(callFervor(callDelay));
		}
	}
	IEnumerator callFervor(float callDelay){
		if(!awake){yield break;}
		yield return new WaitForSeconds(callDelay);
		if(!awake){yield break;}
		nextFervorCall = 0.0f; // Assume we no longer have fervor(recall fervor called at bottom can change this)
		float fervor = getFervor();
		float fervorLostThisCall = fervorLostPerCall * fervor;
		if(hasFervor && fervor > fervorLostThisCall){
			int hitCount = Physics2D.CircleCast(rb.position,screamRadius,Vector2.zero, GP.i.alertBlockFilter, hitBuffer, 1f);
			if(hitCount >= bufferSize){
				Debug.Log("Increase Raycast Buffer for Zombies, "+hitCount+" hits occured");
			}
			for (int i = 0; i < hitCount && i < bufferSize; i++){
				RaycastHit2D hit = hitBuffer[i];
				GameObject obj = hit.transform.gameObject;
				if(obj == gameObject){
					continue; // We don't do ourselves.
				}
				ZombieAI ai = obj.GetComponent<ZombieAI>();
				if(ai == null){
					// We could hit any number of dynamic objects, we only want to interact with zombies
					continue;
				}
				PlayerZombieAlert.alertAt(ai, (Vector2) transform.position,(Vector2) obj.transform.position,fervor - fervorLostThisCall);
			}
			recallFervor();
		}
	}
	private void gainFervor(float fervor){
		getFervor(); // We have to update fervor first
		if(fervor < _fervor){return;}
		_fervor = fervor;
		hasFervor = true;
		needFervorUpdate = true;
		if(_fervor < 0){
			fervor = 0;
			hasFervor = false;
		}else{
			Invoke("getFervor",_fervor/fervorLossRate); // This helps check to make sure we don't run out of fervor
			// Because get fervor is quick, we can have extra invocations running
		}
	}
	private float getFervor(){
		if(calcFervorFrame){
			return _fervor;
		}
		float currentTime = Time.time;
		_fervor = _fervor - (currentTime - lastFervorUpdate) * fervorLossRate;
		if(_fervor <= 0){
			_fervor = 0;
			hasFervor = false;
		}
		calcFervorFrame = true;
		lastFervorUpdate = currentTime;
		return _fervor;
	}
	private float getSpeed(){
		if(hasFervor){
			return (speed + getFervor() * fervorSpeed);
		}else{
			return speed;
		}
	}
	
	private void _wanderUpdate(bool propogate = true){
		rb.drag = passiveSlow;
		if(!hasFervor){
			if(needFervorUpdate){
				needFervorUpdate = false;
				tired = false; // We are not tired just after hunting
			}
			if(tired){
				rb.drag = activeSlow;
				tired = false;
				moveDir = Vector2.zero; // This stops us from adding more force
				if(propogate){
					Invoke("wanderUpdate", rewanderTime *sleepRatio);
				}
				return;
			}
			float frameSeed = GP.i.wang_hash(Time.time)/(200.0f*Mathf.PI);
			float personalSeed = frameSeed + zombieSeed; // Note: This is an angle
			float xSpeed = Mathf.Cos(personalSeed);
			float ySpeed = Mathf.Sin(personalSeed);
			transform.rotation = Quaternion.Euler(0,0,Mathf.Rad2Deg * personalSeed - 90.0f);
			rb.velocity = new Vector2(0, 0); // No infinitly gaining speed
			rb.AddForce(new Vector2(xSpeed,ySpeed)*getSpeed());
			tired = true;
		}else{
			tired = false;
			moveDir = fervorDirection;
		}
		if(propogate){
			Invoke("wanderUpdate", rewanderTime);
		}
	}
	// Importantly, this behavior is VERY slow. Zombie collisions should be limited if possible
	public void OnCollisionEnter2D(Collision2D c){
		if(!awake){return;}
		if(getFervor() > 0.2f){return;} // We don't want to bump during fervor.
		Invoke("turnEyesOn", eyeDelay*Random.value);
		tired = false;
		rb.drag = passiveSlow;
		moveDir = Vector2.Reflect(rb.velocity,c.GetContact(0).normal).normalized; // We set our direction based on a reflection.
		rb.velocity = new Vector2(0, 0); // No infinitly gaining speed
		rb.AddForce(moveDir * getSpeed());
		rotateToVec(moveDir);
	}
	
	// After we alert we are "stunned" from similar changes for a few seconds. This disables that. 
	// Intended to be used as a coroutine
	public void turnEyesOn(){
		hasEyes = true;
	}
	
	public bool wouldAlert(Vector2 pos, float fervor = 1.0f){
		if(!awake){return false;}
		if(!hasEyes) {return false;}
		if(fervor > 1.0f){
			fervor = 1.0f;
		}
		if(fervor < getFervor()){
			return false; // We have something better we are chasing.
		}
		return true;
	}
	public bool alert(Vector2 pos,float fervor = 1.0f){
		if(!wouldAlert(pos,fervor)){
			return false;
		}
		hasEyes = false;
		Invoke("turnEyesOn", (eyeDelay*(1-fervor) + minEyeDelay)*Random.value);
		tired = false;
		rb.drag = passiveSlow;
		gainFervor(fervor);
		Vector2 relativePos = (pos - (Vector2) transform.position);
		float dis = relativePos.magnitude;
		moveDir = relativePos/dis;
		fervorDirection = moveDir;
		rb.velocity = new Vector2(0, 0); // No infinitly gaining speed
		rb.AddForce(moveDir*getSpeed());
		rotateToVec(moveDir);
		// Delay Calling randomly so as to avoid too many calls on the same frame
		recallFervor();
		return true;
	}
}