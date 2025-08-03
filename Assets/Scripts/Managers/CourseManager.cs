using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CourseManager : MonoBehaviour
{
    public static CourseManager Instance { get; private set; }
    public Course SelectedCourse { get; set; } // Store the selected Course object
    public float CourseDistance { get; set; } // Store the distance from LocationData
    public List<int> holeScores = new List<int>();
    public string roundType = "";
    public string user_id = "";
    public string match_id = "";


    private string SUPABASE_URL = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/";
    private string SUPABASE_API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVycXNyZWNzY2lvcmlnZXdhaWhyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTQxMTIwNjYsImV4cCI6MjA2OTY4ODA2Nn0.0M6QpU8h-_6zESOlyuXB3lkq7RXlOLXhKEPMCax14zU";
    

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    public void FinishRound()
    {
        StartCoroutine(AddRound());
    }
    IEnumerator AddRound()
    {
        int total_score = 0;
        foreach (int score in holeScores)
        {
            total_score += score;
        }
        string url = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/Rounds";

        var roundData = new Dictionary<string, object>
        {
            { "user_id", user_id },
            { "course_id", SelectedCourse.course_id },
            { "total_score", total_score },
            { "hole_scores", holeScores },
            { "round_type", roundType }
        };

        if (!string.IsNullOrEmpty(match_id))
            roundData.Add("match_id", match_id);

        string jsonData = JsonConvert.SerializeObject(roundData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success || www.responseCode == 201)
            {
                Debug.Log("Round inserted successfully.");
                Debug.Log(roundData);
                Debug.Log(www.downloadHandler.text);  // Optional: show inserted data
            }
            else
            {
                Debug.LogError($"Failed to insert round: {www.error}, Response: {www.downloadHandler.text}");
            }
        }
    }


    public void SetRoundType(string type)
    {
        roundType = type;
    }
}