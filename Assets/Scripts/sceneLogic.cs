using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneLogic : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
	
	IEnumerator startGame(){
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName:"OpenWorld");
		while (!asyncLoad.isDone)
        {
            yield return null;
        }
	}
	
	public void startGameButtion(){
		Debug.Log("Start button pressed");
		StartCoroutine(startGame());
	}
	public void openOptions(){
		
	}
}
