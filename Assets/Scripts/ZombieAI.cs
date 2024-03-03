using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
public class ZombieAI : MonoBehaviour
{
	// Zombies share a buffer. The buffer is used directly after it is called and all zombies are handles on the
	// same thread(because unity).
	public static readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[ 1000 ];
	[SerializeField] private LayerMask zombieLayer;
	
	public static float speed = 0.05f;
	public static float activeSlow = 30.0f;
	public static float passiveSlow = 10f;
	public static float sleepRatio = 10f;
	public static float rewanderTime = 10.0f;
	public static float screamRadius = 0.5f;
	public static float fervorLossRate = 0.1f;
	public static float fervorLostPerCall = 0.2f;
	public static float avgCallDelay = 0.5f;
	public static float fervorSpeed = 0.5f;
	public bool special;
	private Vector2 moveDir; 
	public bool tired = false; 
	public bool needFervorUpdate; // Is the next non-fervor call of wanderUpdate just after a fevor?
	private bool awake = false;
	private Rigidbody2D rb;
	private float zombieSeed;
	private bool hasFervor;
	private Coroutine fervorUpdate;
	private bool calcFervorFrame; // Have we already calculated fervor this frame?
	private float _fervor;
	private float lastFervorUpdate;
	private Vector2 oldPos;
	public bool hadOldPos;
	private Vector2 fervorDirection;
	public float nextFervorCall;
	
	public void Update(){
		Vector2 pos = transform.position;
		if(!hadOldPos || ZombieManager.instance.checkMovement(gameObject,oldPos,pos)){
			pos = oldPos;
		} else {
			oldPos = pos;
			hadOldPos = true;
		}
		if(!awake){return;} // We only act if we are awake.
		calcFervorFrame = false;
		rb.AddForce(moveDir * getSpeed());
		float angle = Mathf.Rad2Deg * Mathf.Atan2(rb.velocity.y, rb.velocity.x);
		transform.rotation = Quaternion.Slerp(transform.rotation,Quaternion.Euler(0, 0, angle-90.0f),0.5f);
	}
	
	private uint wang_hash(float time)
	{
		unsafe {
			float* timePtr = &time;
			uint* timeIntPtr = (uint*) (timePtr); // I am sorry. 
			uint seed = *timeIntPtr;
			seed = (seed ^ 61) ^ (seed >> 16);
			seed *= 9;
			seed = seed ^ (seed >> 4);
			seed *= 0x27d4eb2d;
			seed = seed ^ (seed >> 15);
			return seed;
		}
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
		oldPos = (Vector2) transform.position;
		hadOldPos = false;
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
			int hitCount = Physics2D.CircleCastNonAlloc(rb.position,screamRadius,Vector2.zero, hitBuffer, 1f,zombieLayer);
			if(hitCount >= 1000){
				Debug.Log("Increase Raycast Buffer for Zombies, "+hitCount+" hits occured");
			}
			for (int i = 0; i < hitCount && i < 1000; i++){
				RaycastHit2D hit = hitBuffer[i];
				GameObject obj = hit.transform.gameObject;
				if(obj == gameObject){
					continue; // We don't do ourselves.
				}
				ZombieAI ai = obj.GetComponent<ZombieAI>();
				if(ai == null){
					Debug.Log("Hit non-zombie with zombie hitbox: " + obj);
					continue;
				}
				PlayerZombieAlert alerter = PlayerZombieAlert.instance; 
				alerter.alertAt(ai, (Vector2) transform.position,(Vector2) obj.transform.position,fervor - fervorLostThisCall);
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
			float frameSeed = wang_hash(Time.time)/(200.0f*Mathf.PI);
			float personalSeed = frameSeed + zombieSeed;
			float xSpeed = Mathf.Cos(personalSeed);
			float ySpeed = Mathf.Sin(personalSeed);
			moveDir = new Vector2(xSpeed,ySpeed);
			tired = true;
		}else{
			tired = false;
			moveDir = fervorDirection;
		}
		if(propogate){
			Invoke("wanderUpdate", rewanderTime);
		}
	}
	public void OnCollisionEnter2D(Collision2D c){
		if(!awake){return;}
		tired = false;
		rb.drag = passiveSlow;
		moveDir = (c.GetContact(0).normal+moveDir).normalized;
	}
	public bool alert(Vector2 pos,float fervor = 1.0f){
		if(!awake){return false;}
		if(fervor > 1.0f){
			fervor = 1.0f;
		}
		if(fervor < getFervor()){
			return false; // We have something better we are chasing.
		}
		tired = false;
		rb.drag = passiveSlow;
		gainFervor(fervor);
		Vector2 relativePos = (pos - (Vector2) transform.position);
		float dis = relativePos.magnitude;
		moveDir = relativePos/dis;
		fervorDirection = moveDir;
		// Delay Calling randomly so as to avoid too many calls on the same frame
		recallFervor();
		return true;
	}
}