using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class sceneLogic : MonoBehaviour
{
	public GameObject loadingScreen;
	public GameObject mainScreen;
	public Slider progressbar;
	public TMPro.TextMeshProUGUI description;
	bool checkProgress = false;
	
	void Start(){
		checkProgress = false;
	}
	void Update(){
		worldGen worldGenerator = worldGen.instance;
		if(checkProgress && worldGenerator != null){
			// If we are loading, we get our progress
			(string,float) progress = worldGenerator.getProgress();
			// Update the UI with the game's progress
			description.text = progress.Item1;
			progressbar.value = progress.Item2;
		}
	}
	public IEnumerator startGame(){
		// Start the loading screen
		loadingScreen.SetActive(true);
		mainScreen.SetActive(false);
		
		// load the main game
		AsyncOperation loading = SceneManager.LoadSceneAsync("OpenWorld",LoadSceneMode.Additive);
		while(!loading.isDone){
			yield return null;
		}
		
		// Start world generation
		worldGen worldGenerator = worldGen.instance;
		Coroutine generation = StartCoroutine(worldGenerator.Generate());		
		checkProgress = true; // we will be updating the loading bar
	}
	
	public void startGameButtion(){
		 StartCoroutine(startGame());
	}
	
	public void openOptions(){
		
	}
}
