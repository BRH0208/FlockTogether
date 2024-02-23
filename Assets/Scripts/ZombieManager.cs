using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieManager : MonoBehaviour
{
	// Singleton pattern;
	public static ZombieManager instance;
	
	public GameObject zombieObj; 
	
	private ulong zombieCount; // the fact this is a long makes me happy. 
	
	public GameObject spawnZombie(float x, float y){
		zombieCount++;
		Quaternion rotation = Quaternion.Euler(0f,0f,Random.Range(0.0f,360.0f));
		return Instantiate(zombieObj, new Vector3(x, y, 0), rotation,transform);
	}
	
	public bool hasEntityInTile(Vector2 pos){
		return false; // TODO: Change this
	}
	
    // Start is called before the first frame update
    void Start()
    {
		ZombieManager.instance = this; 
        zombieCount = 0;
    }
}
