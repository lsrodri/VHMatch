using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Valve.VR;

public class Trial : MonoBehaviour
{

    public int phase = 1;

    // Variables to be read from Player Preferences, set by previous scenes
    public string trialNumber;
    public string participantId;
    public bool training;

    // Possibility of setting manual variables for dev and debugging
    public string adhocTrialNumber;
    public string adhocParticipantId;


    // Variables needed for capturing an answer and saving it in a csv
    private bool leftKeyPressed = false;
    private bool rightKeyPressed = false;
    private bool isAnswered = false;

    // Time limit, imported from csv
    private float timeLimit;
    public float adhocTimeLimit = 0f;
    public TextMeshProUGUI timerText;
    private bool timeIsUp = false;
    public bool loadScene;
    private float currentTime;
    public string sceneName;

    // Objects that prevent the probe from going under stimuli level
    public GameObject plane;
    public GameObject sampleBarrier;
    public GameObject matchBarrier;

    // Objects that create a mask for stimuli
    private GameObject maskCubeLeft;
    private GameObject maskCubeRight;
    private GameObject maskCubeSample;

    // Timestamps
    private string startTimestamp;
    private string answerTimestamp;

    // Canvas references for requesting answers on countdown end
    public Canvas countdownCanvas;
    public Canvas promptCanvas;
    public Canvas pauseCanvas;
    public TextMeshProUGUI pauseCountdownText;
    public TextMeshProUGUI feedbackText;

    public GameObject magicSphere;
    private MagneticSphere magneticSphere;

    public float pauseLength;

    public Material mat;

    public bool showFeedback = true;
    public bool showCountdown = false;

    string sampleNumber;
    string sampleOrder;
    string sampleTime;
    string comparisonTime;
    string condition;

    private GameObject sampleObjectPath;
    private GameObject sampleObjectBlocks;
    private GameObject foilObjectPath;
    private GameObject foilObjectBlocks;

    private CsvReader csvReader;
    private Dictionary<string, string> rowData;

    private bool pauseBool = false;

    // TrackProbe script uses this bool to check when to start recording position
    public static bool shouldRecord = false;

    public SteamVR_Action_Boolean snapLeftAction = SteamVR_Input.GetBooleanAction("SnapTurnLeft");
    public SteamVR_Action_Boolean snapRightAction = SteamVR_Input.GetBooleanAction("SnapTurnRight");

    // Plane "Elevator" to move the probe up
    private PlaneElevator planeElevator;

    // Declaring manual positions that are used to 
    private static readonly Vector3 sampleResetPosition = new Vector3(-8.56f, -12f, -0.50999999f);
    private static readonly Vector3 foilResetPosition = new Vector3(8.05f, -12f, -0.50999999f);

    public bool runTimer = true;

    private Renderer thumbsUp;
    private Renderer thumbsDown;
    private Renderer questionMark;

    private int rowCount;

    public int trialsBeforePause;

    // Start is called before the first frame update
    void Awake()
    {

        promptCanvas.enabled = false;
        pauseCanvas.enabled = false;

        if (adhocTrialNumber != "")
        {
            trialNumber = adhocTrialNumber;
        }
        else
        {
            trialNumber = PlayerPrefs.HasKey("trialNumber") ? PlayerPrefs.GetString("trialNumber") : adhocTrialNumber;
        }

        if (adhocParticipantId != "")
        {
            participantId = adhocParticipantId;
        }
        else
        {
            participantId = PlayerPrefs.HasKey("participantId") ? PlayerPrefs.GetString("participantId") : adhocParticipantId;
        }

        // Checking whether this is training, in which case timer doesn't run and data is not saved
        training = PlayerPrefs.HasKey("training") ? Convert.ToBoolean(PlayerPrefs.GetInt("training")) : false;

        csvReader = FindObjectOfType<CsvReader>();

        rowCount = csvReader.GetRowCount(participantId);

        maskCubeLeft = GameObject.Find("MaskCubeLeft");
        maskCubeRight = GameObject.Find("MaskCubeRight");
        maskCubeSample = GameObject.Find("MaskCubeSample");

        thumbsUp = GameObject.Find("TURenderer").GetComponent<Renderer>();
        thumbsDown = GameObject.Find("TDRenderer").GetComponent<Renderer>();
        questionMark = GameObject.Find("QuestionMark").GetComponent<Renderer>();

        planeElevator = GetComponent<PlaneElevator>();

        magneticSphere = magicSphere.GetComponent<MagneticSphere>();

        if (magneticSphere != null)
        {
            magneticSphere.toggleConstraint(1); // Enable HL_CONSTRAINT
        }

        pauseCountdownText.enabled = showCountdown;
        timerText.enabled = showCountdown;

        startTrial("sample");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(phase);
        if (phase == 1)
        {
            // To help development, a public bool controls whether the timer actually runs
            // Also checking if this is a training trial, in which case time does not run
            if (runTimer && !training)
            {
                currentTime -= Time.deltaTime;
            }

            if (pauseBool)
            {
                if (showCountdown)
                {
                    pauseCountdownText.text = currentTime.ToString("0");
                }
            }
            else
            {
                timerText.text = currentTime.ToString("0");
            }

            if (currentTime <= 0)
            {
                // If time is up during pause, this means that the first phase is over and the second phase should be loaded
                if (pauseBool)
                {
                    // Preparing currentTime for the second phase, match, which uses comparisonTime
                    currentTime = float.Parse(comparisonTime) / 1000f;
                    pauseCanvas.enabled = false;
                    pauseBool = false;
                    startTrial("match");

                }
                else
                {
                    startPause(2);

                }
            }
        }
        else if (phase == 2)
        {
            if (!timeIsUp)
            {
                // Managing and rendering the countdown
                // To help development, a public bool controls whether the timer actually runs
                // Also checking if this is a training trial, in which case time does not run
                if (runTimer && !training)
                {
                    currentTime -= Time.deltaTime;
                }

                if (pauseBool)
                {
                    if (showCountdown)
                    {
                        pauseCountdownText.text = currentTime.ToString("0");
                    }
                }
                else
                {
                    timerText.text = currentTime.ToString("0");
                }

            }

            // If time is over and the answer prompt is invisible, show it
            if (currentTime <= 0 && promptCanvas.enabled == false)
            {
                timeIsUp = true;
                PromptAnswer();
            }

            // If time is up from the answer prompt, start a new trial from the first phase
            if (currentTime <= 0 && pauseBool == true)
            {

                startTrial("sample");

            }

            // Capture arrow key input
            if (Input.GetKey(KeyCode.LeftArrow) || snapLeftAction.GetStateDown(SteamVR_Input_Sources.LeftHand))
            {
                leftKeyPressed = true;
                Debug.Log("left");
            }
            else if (Input.GetKey(KeyCode.RightArrow) || snapRightAction.GetStateDown(SteamVR_Input_Sources.LeftHand))
            {
                rightKeyPressed = true;
                Debug.Log("right");
            }

            // Save the answer to the CSV file
            if (leftKeyPressed && !isAnswered)
            {
                SaveAnswerToCsv("left");
                isAnswered = true;
                timeIsUp = false;
                shouldRecord = false;
            }
            else if (rightKeyPressed && !isAnswered)
            {
                SaveAnswerToCsv("right");
                isAnswered = true;
                timeIsUp = false;
                shouldRecord = false;
            }
        }
    }

    private void startTrial(string trialPhase)
    {

        timeIsUp = false;

        pauseBool = false;

        isAnswered = false;

        // Resetting answer keys
        leftKeyPressed = false;
        rightKeyPressed = false;

        pauseCanvas.enabled = false;
        promptCanvas.enabled = false;

        thumbsUp.enabled = false;
        thumbsDown.enabled = false;
        questionMark.enabled = false;

        countdownCanvas.enabled = true;

        rowData = csvReader.ReadCsvRow(participantId, trialNumber);

        if (rowData != null)
        {
            // Access the data for the columns you're interested in
            sampleNumber = rowData["Sample Number"];
            sampleOrder = rowData["Sample Order"];
            sampleTime = rowData["Sample Time"];
            comparisonTime = rowData["Comparison Time"];
            condition = rowData["Condition"];
        }
        else
        {
            // Manual data for OSX csv issues
            sampleNumber = "30";
            sampleOrder = "right";
            sampleTime = "10000";
            comparisonTime = "20000";
            condition = "V";

            Debug.LogError($"Could not find row with Participant ID {participantId} and Trial Number {trialNumber}");
        }

        sampleObjectPath = GameObject.Find(sampleNumber + "s_path");
        sampleObjectBlocks = GameObject.Find(sampleNumber + "s_block");
        //sampleObject.transform.Find("default").GetComponent<Renderer>().material = mat;

        // Checking for existing sample instances left from the V condition
        if (GameObject.Find("VConditionSamplePath"))
        {
            Destroy(GameObject.Find("VConditionSamplePath"));
        }

        if (GameObject.Find("VConditionSampleBlocks"))
        {
            Destroy(GameObject.Find("VConditionSampleBlocks"));
        }



        if (trialPhase == "sample")
        {
            // to-do: find position for single stimulus, duplicate barriers to display here, hide other barrier set

            phase = 1;

            // Central position for sample stimulus
            sampleObjectPath.transform.localPosition = new Vector3(-1.76999998f, 0.5500000007f, -0.50999999f);
            sampleObjectBlocks.transform.localPosition = new Vector3(-1.76999998f, 0.5500000007f, -0.50999999f);

            // Surface Sample Barrier and hide Match Barrier
            sampleBarrier.transform.localPosition = new Vector3(1.70600009f, -0.271763206f, 0.100000001f);
            matchBarrier.transform.localPosition = new Vector3(1.70600009f, -2.271763206f, 0.100000001f);

            // Surfacing the sample mask and hiding the others
            maskCubeSample.transform.localPosition = new Vector3(0.0199999996f, -0.514999986f, -0.0400003791f);
            maskCubeLeft.transform.localPosition = new Vector3(-1.58000028f, -2.448236823f, -0.0400003791f);
            maskCubeRight.transform.localPosition = new Vector3(1.66999972f, -2.448236823f, -0.0400003791f);

            // As far as our understanding goes, conditions only apply to the first phase because the second needs to resemble real-world conditions, in this case, visuohaptic.
            // If this changes, we would only need to remove this conditional statament below
            if (condition == "V")
            {
                // Moving the background plane to stop device for dropping and to allow proble to glide through stimuli
                plane.transform.SetPositionAndRotation(new Vector3(0.27f, -0.9f, 0.508f), new Quaternion(0f, 0f, 0f, 0f));
                //plane.transform.position = planeElevator.targetPosition;
                plane.GetComponent<Renderer>().enabled = false;

                // Reinstantiating the sample and foil game objects so that it does not get mapped by OpenHaptics, just removing the tag does not solve it
                // Creating a temporary object as removing the tag from the original (unreliably) disables its haptics
                GameObject tempSampleObjectPath = Instantiate(sampleObjectPath, sampleObjectPath.transform.position, Quaternion.identity);
                GameObject tempSampleObjectBlocks = Instantiate(sampleObjectBlocks, sampleObjectBlocks.transform.position, Quaternion.identity);

                // Removing the tag, else the new instance comes tagged and gets mapped
                tempSampleObjectPath.transform.Find("default").tag = "Untagged";
                tempSampleObjectBlocks.transform.Find("default").tag = "Untagged";

                GameObject newSampleObjectPath = Instantiate(tempSampleObjectPath, sampleObjectPath.transform.position, Quaternion.identity);
                GameObject newSampleObjectBlocks = Instantiate(tempSampleObjectBlocks, sampleObjectBlocks.transform.position, Quaternion.identity);

                // Deleting the temporary sample object
                Destroy(tempSampleObjectPath);
                Destroy(tempSampleObjectBlocks);

                newSampleObjectPath.name = "VConditionSamplePath";
                newSampleObjectBlocks.name = "VConditionSampleBlocks";
                // Set the parent of the new instance to the parent of the original object, else it's out of position
                newSampleObjectPath.transform.SetParent(sampleObjectPath.transform.parent);
                newSampleObjectBlocks.transform.SetParent(sampleObjectBlocks.transform.parent);

                // Moving the original aside
                //sampleObject.transform.localPosition = new Vector3(-8.56f, 0.5500000007f, -0.50999999f);
                sampleObjectPath.transform.localPosition = sampleResetPosition;
                sampleObjectBlocks.transform.localPosition = sampleResetPosition;

                // Replicating for foil
                //foilObject.transform.Find("default").tag = "Untagged";
                //GameObject newFoilObject = Instantiate(foilObject, foilObject.transform.position, Quaternion.identity);
                //newFoilObject.transform.SetParent(foilObject.transform.parent);
                //Destroy(foilObject);
            }
            else if (condition == "H")
            {
                //sampleObject.transform.Find("default").GetComponent<MeshRenderer>().enabled = false;
                //foilObject.transform.Find("default").GetComponent<MeshRenderer>().enabled = false;
                GameObject.Find("HPlane").GetComponent<MeshRenderer>().enabled = true;
            }

            // For development purposes, I am only reading the csv comparisonTime if I haven't set it on the controller game object
            if (adhocTimeLimit == 0f)
            {
                timeLimit = float.Parse(sampleTime) / 1000f;
            }
            else
            {
                timeLimit = adhocTimeLimit;
            }

        }
        else if (trialPhase == "match")
        {

            phase = 2;

            foilObjectPath = GameObject.Find(sampleNumber + "f_path");
            foilObjectBlocks = GameObject.Find(sampleNumber + "f_block");

            if (sampleOrder == "left")
            {
                sampleObjectPath.transform.localPosition = new Vector3(-3.34f, 0.5500000007f, -0.50999999f);
                sampleObjectBlocks.transform.localPosition = new Vector3(-3.34f, 0.5500000007f, -0.50999999f);

                foilObjectPath.transform.localPosition = new Vector3(3.21f, 0.5500000007f, -0.50999999f);
                foilObjectBlocks.transform.localPosition = new Vector3(3.21f, 0.5500000007f, -0.50999999f);
            }
            else if (sampleOrder == "right")
            {
                sampleObjectPath.transform.localPosition = new Vector3(-0.1499996f, 0.5500000007f, -0.50999999f);
                sampleObjectBlocks.transform.localPosition = new Vector3(-0.1499996f, 0.5500000007f, -0.50999999f);

                foilObjectPath.transform.localPosition = new Vector3(0.0199995f, 0.5500000007f, -0.50999999f);
                foilObjectBlocks.transform.localPosition = new Vector3(0.0199995f, 0.5500000007f, -0.50999999f);
            }

            //foilObject.transform.Find("default").GetComponent<Renderer>().material = mat;

            // Surface Match Barrier and hide Sample Barrier
            matchBarrier.transform.localPosition = new Vector3(1.70600009f, -0.271763206f, 0.100000001f);
            sampleBarrier.transform.localPosition = new Vector3(1.70600009f, -2.271763206f, 0.100000001f);

            // Restoring visibility for the V condition
            sampleObjectPath.transform.Find("default").GetComponent<MeshRenderer>().enabled = true;
            sampleObjectBlocks.transform.Find("default").GetComponent<MeshRenderer>().enabled = true;

            // Hiding a possible H Condition occlusion plane
            GameObject.Find("HPlane").GetComponent<MeshRenderer>().enabled = false;

            // Surfacing the sample mask and hiding the others
            maskCubeSample.transform.localPosition = new Vector3(0.0399999991f, -2.448236823f, -0.0400003791f);
            //maskCubeSample.transform.localPosition = new Vector3(0.0199999996f, -0.514999986f, -0.0400003791f);
            maskCubeLeft.transform.localPosition = new Vector3(-1.52f, -0.545f, -0.02000038f);
            maskCubeRight.transform.localPosition = new Vector3(1.61f, -0.545f, -0.02000038f);

            if (adhocTimeLimit == 0f)
            {
                timeLimit = float.Parse(comparisonTime) / 1000f;
            }
            else
            {
                timeLimit = adhocTimeLimit;
            }



            // Only saved for the match phase
            startTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

        }


        // Initializing the timer
        currentTime = timeLimit;

        // Signaling to TrackProbe script recording should start
        shouldRecord = true;

        // Lower the "elevator" to start the trial
        // Only do it for these cases as the plane is manipulated differently for V
        if (condition != "V" || trialPhase == "match")
        {
            plane.transform.position = planeElevator.originalPosition;
        }

        // Disabling magnetic sphere
        magneticSphere.toggleConstraint(0);

        //Debug.Log("down");
        //StartCoroutine(moveHapticObjects("down"));

        Debug.Log("participantId: " + participantId + " trialNumber: " + trialNumber + " phase: " + phase + " condition: " + condition);
    }

    private void SaveAnswerToCsv(string answer)
    {
        // Time since the scene was loaded, saved as participant reaction time
        string elapsedTime = Time.timeSinceLevelLoad.ToString();

        answerTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

        string correctness = answer == sampleOrder ? "true" : "false";

        if (showFeedback)
        {
            // Show correctness on screen
            //string feedbackString = (correctness == "true") ? "Correct" : "Wrong";
            //feedbackText.SetText(feedbackString);

            if (correctness == "true")
            {
                thumbsUp.enabled = true;
                Debug.Log("Correct");
            }
            else
            {
                thumbsDown.enabled = true;
                Debug.Log("Incorrect");
            }
        }

        // Not saving the data if this is a training trial
        if (!training)
        {
            // Create a new row for the CSV file
            string[] rowData = new string[] { participantId, trialNumber, answer, correctness, startTimestamp, answerTimestamp };
            //// Check if the file exists
            //string filePath = Path.Combine(Application.dataPath, "Results", participantId + ".csv");


            string filePath = Path.Combine(Application.persistentDataPath, "Results", participantId + ".csv");

            //string resultsDirPath = Path.Combine(Application.persistentDataPath, "Results");
            //Directory.CreateDirectory(resultsDirPath); // Ensures that the directory exists
            //string filePath = Path.Combine(resultsDirPath, participantId + ".csv");

            bool fileExists = File.Exists(filePath);

            // Write the row to the CSV file
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                if (!fileExists)
                {
                    // Add the header row if the file did not exist previously
                    sw.WriteLine("Participant ID,Trial Number,Response,Correctness, Start Timestamp, End Timestamp");
                }

                sw.WriteLine(string.Join(",", rowData));
                Debug.Log("Data saved in: " + Application.persistentDataPath);
            }
        }

        // If this is the last trial for this participant or if it's a multiple of the number set for pauses, load the results scene instead of attempting to load the next trial
        if (trialNumber == rowCount.ToString() || Convert.ToInt16(trialNumber) % trialsBeforePause == 0)
        {
            SceneManager.LoadScene("Results");
        }
        else
        {
            // After writing current trial data, setting the next trial
            nextTrial();
        }
        
    }

    private void nextTrial()
    {
        // to-do: check count for csv participant id to check if this is the last trial
        // to-do: move current trial object's back to original position.
        // Setting the next trial
        int nextTrial = int.Parse(trialNumber) + 1;
        trialNumber = nextTrial.ToString();
        
        // Persisting it in case of scene unloading or crash
        PlayerPrefs.SetString("trialNumber", trialNumber);

        //Debug.Log("Next trial: " + nextTrial);

        countdownCanvas.enabled = true;
        promptCanvas.enabled = false;

        sampleObjectPath.transform.localPosition = sampleResetPosition;
        sampleObjectBlocks.transform.localPosition = sampleResetPosition;

        foilObjectPath.transform.localPosition = foilResetPosition;
        foilObjectBlocks.transform.localPosition = foilResetPosition;

        currentTime = float.Parse(sampleTime) / 1000f;

        startPause(phase);
    }

    private void startPause(int nextPhase, bool afterPrompt = false)
    {

        pauseBool = true;
        countdownCanvas.enabled = false;
        questionMark.enabled = false;

        // moved to delayedPauseSetup() as it needs to wait for the elevator
        //pauseCanvas.enabled = true;
        currentTime = pauseLength;

        Invoke("delayedPauseSetup", planeElevator.duration);

        if (phase == 2 && showFeedback)
        {
            feedbackText.enabled = true;
        }
        else
        {
            feedbackText.enabled = false;
        }

        // Signaling to TrackProbe that it should stop recording
        shouldRecord = false;


        // Removed in favor of a target object where participants should bring the probe
        StartCoroutine(moveHapticObjects("up"));
    }

    IEnumerator moveHapticObjects(string direction)
    {

        // Restablishing the plane as V condition manipulates it to present stimuli without allowing the probe to touch it
        plane.GetComponent<Renderer>().enabled = true;

        // Arranging objects in case the stimuli need to be "hidden" by the plane
        if (direction == "up")
        {
            if (plane.transform.position != planeElevator.targetPosition)
            {
                yield return planeElevator.MoveElevator(direction);
            }

            sampleObjectPath.transform.localPosition = sampleResetPosition;
            sampleObjectBlocks.transform.localPosition = sampleResetPosition;

            if (foilObjectPath)
            {
                foilObjectPath.transform.localPosition = foilResetPosition;
            }
            
            if (foilObjectBlocks)
            {
                foilObjectBlocks.transform.localPosition = foilResetPosition;
            }
        }
        
    }

    // Needed as Invoke does not take parameters
    void delayedPauseSetup()
    {
        magneticSphere.toggleConstraint(1);
        pauseCanvas.enabled = true;
    }

    void delayedAnswerSetup()
    {
        magneticSphere.toggleConstraint(1);
        countdownCanvas.enabled = false;
        promptCanvas.enabled = true;
    }

    private void PromptAnswer()
    {


        // Removed in favor of a target object where participants should bring the probe
        StartCoroutine(moveHapticObjects("up"));
        // Commented as it was somehow showing up during the phase 1, must check how to prevent PromptAnswer to be triggered after answering
        //Invoke("delayedAnswerSetup", planeElevator.duration);
        magneticSphere.toggleConstraint(1);
        countdownCanvas.enabled = false;
        promptCanvas.enabled = true;
        questionMark.enabled = true;

    }

    public void skipPhase()
    {
        currentTime = 0;
    }
}
