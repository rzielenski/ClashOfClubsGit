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
    public TMP_InputField matchNameInput;
    public TMP_InputField clanNameInput;
    public Transform scrollView;
    public Transform clanScrollView;

    public string matchFormat;
    public bool practiceMatch = false;
    public bool publicMatch = true;
    public bool publicClan = true;

    public int clanMaxPlayers;

    void Awake()
    {
        string savedData = "";
        GameData data = new GameData();
        if (File.Exists(Path.Combine(Application.persistentDataPath, "saveData.json")))
        {
            savedData = File.ReadAllText(Path.Combine(Application.persistentDataPath, "saveData.json"));
            data = JsonConvert.DeserializeObject<GameData>(savedData);
        }
        else
        {
            Debug.Log("No saved data found.");
        }
        
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
        APIHandler.Instance.GetUserClans();
        APIHandler.Instance.GetLeaders();
    }
    public void toggleMatchPractice()
    {
        practiceMatch = !practiceMatch;
    }

    public void toggleMatchPublic()
    {
        publicMatch = !publicMatch;
    }
    public void toggleClanPublic()
    {
        publicClan = !publicClan;
    }
    public void Stroke(bool x)
    {
        if (x)
        {
            matchFormat = "stroke";
        }
        else
        {
            matchFormat = "scramble";
        }
    }

    public void ClanSize(int size)
    {
        clanMaxPlayers = size;
    }
    public void CreateMatch(string nameOverride = "")
    {
        string type = "solo";
        string name = "";
        if (practiceMatch)
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
            name = matchNameInput.text;
        }
        CourseManager.Instance.roundType = type;
        CourseManager.Instance.updated = false;
        APIHandler.Instance.CreateMatch(type, matchFormat, name, publicMatch, practiceMatch);
        SceneManager.LoadScene("SelectCourse");
    }

    public void FindMatch()
    {
        APIHandler.Instance.GetBestMatch(CourseManager.Instance.user.user_id, match =>
        {
            if (match != null)
            {
                APIHandler.Instance.CreateMatchPlayer(match);
                CourseManager.Instance.updated = false;
                SceneManager.LoadScene("SelectCourse");
            }
            else
            {
                Debug.Log("No match found.");
            }
        });
    }

    public void CreateClan(string nameOverride = "")
    {
        string name = "";
        string type = "";
        if (clanMaxPlayers == 2) { type = "duo"; }
        else if (clanMaxPlayers == 4) { type = "squad"; }
        else { type = "squad"; }

        if (nameOverride != "")
        {
            name = nameOverride;
        }
        else
        {
            name = clanNameInput.text;
        }
        
        APIHandler.Instance.CreateClan(type, name, publicClan);
        SceneManager.LoadScene("Clans");
    }

    public void FindClan()
    {
        
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
        RectTransform rt = clanPanel.gameObject.GetComponent<RectTransform>();
        if (clanPanel.activeInHierarchy)
        {
            clanPanel.SetActive(false);
            clanScrollView.position = new Vector2(clanScrollView.position.x, clanScrollView.position.y + rt.sizeDelta.y);
        }
        else
        {
            clanPanel.SetActive(true);
            clanScrollView.position = new Vector2(clanScrollView.position.x, clanScrollView.position.y - rt.sizeDelta.y);
        }
    }
}
