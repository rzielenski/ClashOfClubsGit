using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;

public class ChooseActionManager : MonoBehaviour
{
    public Button practiceBtn;
    public GameObject matchPrefab;
    public GameObject matchPanel;
    public GameObject clanPanel;
    public TMP_InputField input;
    public string type = "solo";
    public string format = "stroke";
    public bool practice = false;
    public bool is_public = false;
    public Transform scrollView;

    void Start()
    {
        string savedData = File.ReadAllText(Path.Combine(Application.persistentDataPath, "saveData.json"));
        GameData data = JsonConvert.DeserializeObject<GameData>(savedData);
        if (data.match != null)
        {

            int total_score = 0;
            GameObject buttonObj = Instantiate(matchPrefab, scrollView.Find("Viewport/Content").transform);
            buttonObj.transform.Find("CourseName").GetComponentInChildren<TextMeshProUGUI>().text = data.course.course_name;
            for (int i = 0; i < data.scores.Count; i++)
            {
                if (data.scores[i] == 0) continue;
                total_score += data.scores[i] - data.course.tees.holes[i].par;
            }
            buttonObj.transform.Find("Score").GetComponentInChildren<TextMeshProUGUI>().text = total_score.ToString("+#;-#;E");
            Debug.Log("ran");

            float _distance = 0;
            LocationData.Instance.GetCourseDist(data.course.latitude, data.course.longitude, distance =>
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
                CourseManager.Instance.SelectedCourse = data.course;
                CourseManager.Instance.CourseDistance = _distance;
                CourseManager.Instance.updated = true;
                SceneManager.LoadScene("Scorecard");
            });
            
        }
    }
    public void togglePractice()
    {
        practice = !practice;
    }
    public void togglePublic()
    {
        is_public = !is_public;
    }
    public void Stroke(bool x)
    {
        if (x)
        {
            format = "stroke";
        }
        else
        {
            format = "scramble";
        }
    }
    public void Stroke()
    {
        format = "stroke";
    }
    public void CreateMatch(string nameOverride = "")
    {
        string name = "";
        if (practice)
        {
            type = "practice";
        }
        else { }

        if (nameOverride != "")
        {
            name = nameOverride;
        }
        else
        {
            name = input.text;
        }
        CourseManager.Instance.roundType = type;
        CourseManager.Instance.updated = false;
        APIHandler.Instance.CreateMatch(type, format, name, is_public, practice);
        SceneManager.LoadScene("SelectCourse");
    }

    public void FindMatch()
    {
        APIHandler.Instance.GetBestMatch(CourseManager.Instance.user.solo.elo_rating, match =>
        {
            if (match != null)
            {
                APIHandler.Instance.CreateMatchPlayer(match.match_id);
                CourseManager.Instance.updated = false;
                SceneManager.LoadScene("SelectCourse");
            }
            else
            {
                Debug.Log("No match found.");
            }
        });
    }
    public void OpenCloseMatch()
    {
        RectTransform rt = matchPanel.gameObject.GetComponent<RectTransform>();
        if (matchPanel.activeInHierarchy)
        {
            matchPanel.SetActive(false);
            scrollView.position = new Vector2(scrollView.position.x, scrollView.position.y + rt.sizeDelta.y);
        }
        else
        {
            matchPanel.SetActive(true);
            scrollView.position = new Vector2(scrollView.position.x, scrollView.position.y - rt.sizeDelta.y);
        }
    }

    public void OpenCloseClan()
    {
        if (clanPanel.activeInHierarchy)
        {
            clanPanel.SetActive(false);
        }
        else
        {
            clanPanel.SetActive(true);
        }
    }
}
