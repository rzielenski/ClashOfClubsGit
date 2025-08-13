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
    public Course SelectedCourse { get; set; }
    public float CourseDistance { get; set; }
    public Match curMatch = new Match();
    public Clan SelectedClan { get; set; }
    public List<int> holeScores = new List<int>();
    public string roundType = "";

    public User user = new User();
    public int user_elo;

    public bool updated = false;


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
        int total_score = 0;
        foreach (int score in holeScores)
        {
            total_score += score;
        }
        
        string url = $"https://erqsrecsciorigewaihr.supabase.co/rest/v1/MatchPlayers?user_id=eq.{user.user_id}&match_id=eq.{curMatch.match_id}";

        Debug.Log(url);
        var matchData = new Dictionary<string, object>
        {
            { "tee_id", SelectedCourse.tees.teebox_id },
            { "strokes", total_score },
            { "completed", true }
        };

        string jsonData = JsonConvert.SerializeObject(matchData);
        StartCoroutine(PatchData(url, jsonData));
	
	string newurl = $"https://erqsrecsciorigewaihr.supabase.co/rest/v1/Rounds";

        var roundData = new Dictionary<string, object>
        {
	    { "user_id", user.user_id },
	    { "course_id", SelectedCourse.course_id },
            { "tee_id", SelectedCourse.tees.teebox_id },
            { "total_score", total_score },
            { "round_type", roundType },
	    { "hole_scores", holeScores },
	    { "match_id", curMatch.match_id }
        };

        string newjsonData = JsonConvert.SerializeObject(roundData);
        StartCoroutine(PostData(newurl, newjsonData));

    }




    IEnumerator PostData(string url, string jsonData)
    {
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
                Debug.Log(www.downloadHandler.text);  // Optional: show inserted data
            }
            else
            {
                Debug.LogError($"Failed to insert: {www.error}, Response: {www.downloadHandler.text}");
            }
        }
    }
    
    IEnumerator PatchData(string url, string jsonData)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "PATCH"))
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
                Debug.Log("Round updated successfully.");
                Debug.Log(www.downloadHandler.text);  // Optional: show inserted data
            }
            else
            {
                Debug.LogError($"Failed to update: {www.error}, Response: {www.downloadHandler.text}");
            }
        }
    }
}
