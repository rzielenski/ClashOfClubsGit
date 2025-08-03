using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        

    
}


