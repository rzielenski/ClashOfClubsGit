using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;


public class APIHandler : MonoBehaviour
{
    private string SUPABASE_URL = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/";
    private string SUPABASE_API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVycXNyZWNzY2lvcmlnZXdhaWhyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTQxMTIwNjYsImV4cCI6MjA2OTY4ODA2Nn0.0M6QpU8h-_6zESOlyuXB3lkq7RXlOLXhKEPMCax14zU";
    public static APIHandler Instance { get; private set; }
    public GameObject courseButtonPrefab;
    public GameObject teesButtonPrefab;
    public LocationData locationData;

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

    public void GetHoles(System.Action callback)
    {
        string id = CourseManager.Instance.SelectedCourse.tees.teebox_id;
        string url = $"{SUPABASE_URL}Holes?teebox_id=eq.{id}&select=*";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<Hole> holes = JsonConvert.DeserializeObject<List<Hole>>(json);
            
            CourseManager.Instance.SelectedCourse.tees.holes = holes;
            callback?.Invoke();
        }));
    }

    public void GetTees()
    {
        string id = CourseManager.Instance.SelectedCourse.course_id;
        string url = $"{SUPABASE_URL}Teeboxes?course_id=eq.{id}&select=*";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<TeeBox> tees = JsonConvert.DeserializeObject<List<TeeBox>>(json);
            Transform scrollView = GameObject.FindWithTag("TeesView").transform;
            foreach (Transform child in scrollView)
            {
                Destroy(child.gameObject);
            }
            foreach (var tee in tees)
            {
                var temptee = tee;
                GameObject buttonObj = Instantiate(teesButtonPrefab, scrollView);
                buttonObj.transform.Find("TeeName").GetComponentInChildren<TextMeshProUGUI>().text = tee.name;
                buttonObj.transform.Find("Yardage").GetComponentInChildren<TextMeshProUGUI>().text = tee.total_yards.ToString();
                buttonObj.transform.Find("Par").GetComponentInChildren<TextMeshProUGUI>().text = tee.par.ToString();
                
                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Debug.Log("Making onclick");
                    CourseManager.Instance.SelectedCourse.tees = temptee;
                    SceneManager.LoadScene("Scorecard");
                });
            }
        }));
    }

    public void SearchCourse()
    {
        TMP_InputField input = GameObject.FindWithTag("CourseSearchInput").GetComponent<TMP_InputField>();
        string searchName = UnityWebRequest.EscapeURL(input.text);
        string url = $"{SUPABASE_URL}Courses?course_name=ilike.%25{searchName}%25";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<Course> courses = JsonConvert.DeserializeObject<List<Course>>(json);

            Transform scrollView = GameObject.FindWithTag("CoursesView").transform;
            foreach (Transform child in scrollView)
                Destroy(child.gameObject);

            foreach (var course in courses)
            {
                float _distance = 0;
                GameObject buttonObj = Instantiate(courseButtonPrefab, scrollView);
                buttonObj.transform.Find("CourseName").GetComponentInChildren<TextMeshProUGUI>().text = course.course_name;

                locationData.GetCourseDist(course.latitude, course.longitude, distance =>
                {
                    if (distance >= 0)
                    {
                        GameObject distText = buttonObj.transform.Find("Dist").gameObject;
                        distText.GetComponentInChildren<TextMeshProUGUI>().text = distance.ToString("F2");
                        _distance = distance;
                    }
                });

                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    CourseManager.Instance.SelectedCourse = course;
                    CourseManager.Instance.CourseDistance = _distance;
                    SceneManager.LoadScene("SelectTees");
                });
            }
        }));
    }


    public void GetBestMatch(int userElo, System.Action<Match> callback)
    {
        string url = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/rpc/get_best_match";
        var requestBody = new Dictionary<string, object>
        {
            { "user_elo", userElo }
        };
        string jsonData = JsonConvert.SerializeObject(requestBody);

        StartCoroutine(PostData(url, jsonData, (responseJson) =>
        {
            if (string.IsNullOrEmpty(responseJson))
            {
                Debug.LogError("No match found.");
                callback(null);
                return;
            }

            try
            {
                List<Match> matches = JsonConvert.DeserializeObject<List<Match>>(responseJson);
                if (matches.Count > 0){
                    callback(matches[0]);
                }
                else
                    callback(null);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing match response: " + e.Message);
                callback(null);
            }
        }));
        
    }



    IEnumerator GetRequest(string url, System.Action<string> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                callback?.Invoke(json);  // Pass JSON back via callback
            }
            else
            {
                Debug.LogError($"Supabase Error: {www.error}, Response: {www.downloadHandler.text}");
                callback?.Invoke(null);  // Pass null or empty on failure
            }
        }
    }

    public void CreateMatch(string match_type, string format, string name, bool is_public, bool is_practice)
    {
        string url = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/Matches?select*"; 

        var matchData = new Dictionary<string, object>
        {
            { "match_type", match_type },
            { "format", format },
            { "name", name },
            { "is_public", is_public },
            { "is_practice", is_practice }
        };

        string jsonData = JsonConvert.SerializeObject(matchData);

        StartCoroutine(PostData(url, jsonData, resjson =>
        {
            if (string.IsNullOrEmpty(resjson))
            {
                Debug.LogError("Match insert returned empty response.");
                return;
            }

            try
            {
                List<Match> matches = JsonConvert.DeserializeObject<List<Match>>(resjson);
                if (matches == null || matches.Count == 0)
                {
                    Debug.LogError("Match insert response did not contain a valid match.");
                    return;
                }

                Match match = matches[0];
                
                string newurl = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/MatchPlayers";

                var matchPlayerData = new Dictionary<string, object>
                {
                    { "match_id", match.match_id },
                    { "user_id", CourseManager.Instance.user.user_id }
                };

                string newJsonData = JsonConvert.SerializeObject(matchPlayerData);
                StartCoroutine(PostData(newurl, newJsonData, json => { }));

                CourseManager.Instance.match_id = match.match_id;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing Supabase response: " + e.Message);
            }
        }));
    }

    public void CreateMatchPlayer(string match_id)
    {
        string newurl = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/MatchPlayers";

        var matchPlayerData = new Dictionary<string, object>
        {
            { "match_id", match_id },
            { "user_id", CourseManager.Instance.user.user_id }
        };

        string newJsonData = JsonConvert.SerializeObject(matchPlayerData);
        StartCoroutine(PostData(newurl, newJsonData, json => { }));

        CourseManager.Instance.match_id = match_id;
    }
    IEnumerator PostData(string url, string jsonData, System.Action<string> callback)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Prefer", "return=representation");
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success || www.responseCode == 201)
            {
                Debug.Log("Round inserted successfully.");
                Debug.Log(www.downloadHandler.text);  // Optional: show inserted data
                string json = www.downloadHandler.text;
                callback?.Invoke(json);
            }
            else
            {
                Debug.LogError($"Failed to insert: {www.error}, Response: {www.downloadHandler.text}");
                callback?.Invoke(null);
            }
        }
    }
}


