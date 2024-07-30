using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public TextMeshProUGUI participantIdText;
    public TextMeshProUGUI trialNumberText;
    public Toggle training;

    private string participantId;
    private string trialNumber;


    public void LoadScene(string sceneName)
    {
        // Remove any leading or trailing whitespace
        participantId = participantIdText.text;
        participantId = participantId.Trim();
        participantId = Regex.Replace(participantId, "[^0-9]", "");

        trialNumber = trialNumberText.text;
        trialNumber = trialNumber.Trim();
        trialNumber = Regex.Replace(trialNumber, "[^0-9]", "");

        // Save the state of the toggle as an int (1 for true, 0 for false)
        PlayerPrefs.SetInt("training", training.isOn ? 1 : 0);

        //Debug.Log(trialNumber);

        PlayerPrefs.SetString("participantId", participantId);
        PlayerPrefs.SetString("trialNumber", trialNumber);
        SceneManager.LoadScene(sceneName);
    }
}
