#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoadAttribute]
public static class DefaultSceneLoader
{
	const bool active = true;
    static DefaultSceneLoader(){
        if(active){
			EditorApplication.playModeStateChanged += LoadDefaultScene;
		}
    }

    static void LoadDefaultScene(PlayModeStateChange state){
        if (state == PlayModeStateChange.ExitingEditMode) {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
        }

        if (state == PlayModeStateChange.EnteredPlayMode) {
            EditorSceneManager.LoadScene (0);
        }
    }
}
#endif
