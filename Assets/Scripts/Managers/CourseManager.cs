using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Linq;
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
    private string SUPABASE_API_KEY = "";


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
        int total_score = holeScores.Sum();
        SubmitMatchPlayerRound(curMatch.match_id, user.user_id, total_score, SelectedCourse.tees.teebox_id);

        // POST Rounds for history (unchanged)
        string newurl = $"{SUPABASE_URL}Rounds";
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

        // Optionally, refresh match history or UI after submission
    }

    public void FinishClanRound()
    {
        int total_score = holeScores.Sum();
        SubmitMatchPlayerRound(curMatch.match_id, user.user_id, total_score, SelectedCourse.tees.teebox_id);

        // POST Rounds (unchanged)
        string newurl = $"{SUPABASE_URL}Rounds";
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

        // Optionally, check if all clan members completed and refresh
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
        Debug.Log(url);
        using (UnityWebRequest www = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Prefer", "return=representation");
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success || www.responseCode == 204)
            {
                Debug.Log("MatchPlayers updated successfully.");
                Debug.Log(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Failed to update: {www.error}, Response: {www.downloadHandler.text}");
            }
        }
    }
    public void SubmitMatchPlayerRound(string match_id, string user_id, int strokes, string tee_id)
    {
        string url = $"{SUPABASE_URL}MatchPlayers?match_id=eq.{match_id}&user_id=eq.{user_id}";
        var data = new Dictionary<string, object>
        {
            { "strokes", strokes },
            { "tee_id", tee_id },
            { "completed", true }
        };
        string jsonData = JsonConvert.SerializeObject(data);
        StartCoroutine(PatchData(url, jsonData));
    }
}
