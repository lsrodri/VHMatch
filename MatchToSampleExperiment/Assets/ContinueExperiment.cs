using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ContinueExperiment : MonoBehaviour
{
    public string sceneName;
    public void ContinueExp()
    {
        string trialNumber;

        trialNumber = PlayerPrefs.GetString("trialNumber");

        int nextTrial = int.Parse(trialNumber) + 1;
        trialNumber = nextTrial.ToString();

        // Persisting it in case of scene unloading or crash
        PlayerPrefs.SetString("trialNumber", trialNumber);

        SceneManager.LoadScene(sceneName);
    }
}
