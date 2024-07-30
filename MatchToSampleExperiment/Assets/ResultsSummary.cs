using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class ResultsSummary : MonoBehaviour
{
    // Assuming you have a TextMeshProUGUI component on the same GameObject
    public TextMeshProUGUI csvOutputText;

    public void LoadCsv()
    {
        // File path
        string filePath = Path.Combine(Application.persistentDataPath, "Results", PlayerPrefs.GetString("participantId")+".csv");

        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found at " + filePath);
            return;
        }

        // Read all lines of the CSV file
        string[] lines = File.ReadAllLines(filePath);

        // Get the headers (Participant ID, Trial Number, Response, Correctness, Start Timestamp, End Timestamp)
        string[] headers = lines[0].Split(',');

        // Create a table to hold CSV data
        List<string> csvTable = new List<string>();


        // Showing the last 15 results, Math.Max ensures that the result doesn't go negative in case there are less than 15 in total
        int start = Math.Max(1, lines.Length - 15);

        // Loop through each row in the CSV file
        for (int i = start; i < lines.Length; i++)
        {
            // Split the row into its values
            string[] values = lines[i].Split(',');

            // Transform the "Correctness" and "Timestamp" values and construct the row string
            string correctness = values[3] == "true" ? "Correct" : "Incorrect";

            DateTime startTimestamp = DateTime.Parse(values[4]);
            DateTime endTimestamp = DateTime.Parse(values[5]);
            // Currently hidden after decision to not show reaction time as feedback
            TimeSpan reactionTime = endTimestamp - startTimestamp;

            string row = string.Format("{0}, {1}, {2}",
                values[0], // Participant ID
                values[1], // Trial Number
                correctness
            );

            // Replace commas with tabulations
            row = row.Replace(",", "\t");

            // Add the row string to the table
            csvTable.Add(row);
        }

        // Output the table to the TextMeshProUGUI component, separating rows by newlines
        csvOutputText.text = string.Join("\n", csvTable);
    }

    // Call LoadCsv when the script starts (or call it whenever you want to load the CSV)
    private void Start()
    {
        LoadCsv();
    }
}
