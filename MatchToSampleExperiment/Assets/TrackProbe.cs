using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;

public class TrackProbe : MonoBehaviour
{
    private float timeSinceLastRecord = 0f;
    private const float recordInterval = 0.1f; // every 1/10th of a second
    private string filePath;

    public Trial trial;
    public GameObject probe;

    private bool training;

    // Using the start method so that it can run after Trial's Awake as we need participantId to even create the csv
    private void Start()
    {

        // Checking whether this is training, in which case timer doesn't run and data is not saved
        training = PlayerPrefs.HasKey("training") ? Convert.ToBoolean(PlayerPrefs.GetInt("training")) : false;

        if (training)
        {
            return;
        }

        trial = GetComponent<Trial>();

        //string filePath = Path.Combine(Application.dataPath, "Results", trial.participantId + "_probedata.csv");
        //Debug.Log(filePath);

        string dataFolder = Path.Combine(Application.persistentDataPath, "Results");
        string fileName = $"{trial.participantId}_probedata.csv";
        filePath = Path.Combine(dataFolder, fileName);

        // File might already exist in case we're restarting the participant session
        if (!File.Exists(filePath))
        {
            // Create the file with the header
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("Trial Number,Trial Phase,Time,Position X,Position Y,Position Z");
            }
        }
    }

    void Update()
    {
        // Set by Trial as true when Trials start and as false when Pauses start
        if (Trial.shouldRecord && !training)
        {
            timeSinceLastRecord += Time.deltaTime;

            if (timeSinceLastRecord >= recordInterval)
            {
                RecordPosition();
                timeSinceLastRecord = 0f;
            }
        }
    }

    void RecordPosition()
    {
        Vector3 position = probe.transform.position;
        string dataLine = $"{trial.trialNumber},{trial.phase},{Time.time},{position.x},{position.y},{position.z}";

        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine(dataLine);
        }
    }
}
