using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitButton : MonoBehaviour
{
    public void OnQuitButtonPressed()
    {
#if UNITY_EDITOR
        // If running in the Unity Editor, stop play mode.
        EditorApplication.isPlaying = false;
#else
        // If running in a built application, quit the application.
        Application.Quit();
#endif
    }
}
