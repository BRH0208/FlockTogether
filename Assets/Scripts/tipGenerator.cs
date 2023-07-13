using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tipGenerator : MonoBehaviour
{
	TMPro.TextMeshProUGUI textElement;
	public TextAsset hints;
	string[] lines;
	public float changeDelay; // The time, in seconds, to wait between changing tips(6 seconds works okay)
	
    // Start is called before the first frame update
    void Start()
    {
		string allHints = hints.ToString();
		lines = allHints.Split("\n");
        textElement = GetComponent<TMPro.TextMeshProUGUI>();
		textElement.text = "hi :)";
		Debug.Log(lines.Length+" hints found");
		StartCoroutine(changeHint());
    }
	
	IEnumerator changeHint(){
		while(true){
			textElement.text = lines[Random.Range(0,lines.Length)];
			yield return new WaitForSeconds(changeDelay);
		}
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
