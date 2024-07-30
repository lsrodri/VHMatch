using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class LoadStart : MonoBehaviour
{

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
