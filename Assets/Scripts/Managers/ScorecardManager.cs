using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class ScorecardManager : MonoBehaviour
{

    private List<GameObject> holePanels = new List<GameObject>();
    public TextMeshProUGUI courseInfoText; // Display course name and rating
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI normalizedScoreText;
    public Button submitButton;
    private Course selectedCourse;
    private float courseRating;
    private float courseDistance;
    public GameObject holePrefab;
    public GameObject scorePrefab;
    public Transform panel;

    public int curHole = 1;
    void Start()
    {
        // Retrieve course data from CourseManager
        selectedCourse = CourseManager.Instance.SelectedCourse;
        courseDistance = CourseManager.Instance.CourseDistance;
        submitButton.onClick.AddListener(() => { CourseManager.Instance.FinishRound(); });
        APIHandler.Instance.GetHoles(() =>
        {
            List<Hole> sortedHoles = selectedCourse.tees.holes.OrderBy(hole => hole.hole_num).ToList();
            selectedCourse.tees.holes = sortedHoles;
            int count = 1;
            foreach (var hole in selectedCourse.tees.holes)
            {
                
                int tempCount = count;
                GameObject holeObj = Instantiate(holePrefab, panel);
                holeObj.transform.Find("HoleNum").gameObject.GetComponent<TextMeshProUGUI>().text = count.ToString();
                int par = hole?.par ?? 4;
                int yards = hole?.yardage ?? 0;
                int handicap = hole?.handicap ?? 0;
                holeObj.transform.Find("HoleInfo").GetComponent<TextMeshProUGUI>().text = $"Par: {par}\nYardage: {yards}\nHandicap: {handicap}";

                for (int i = 1; i < par + 3; i++)
                {
                    int tempi = i;
                    GameObject scoreObj = Instantiate(scorePrefab, holeObj.transform.Find("ScoreScrollView").Find("Viewport").Find("Content"));
                    scoreObj.tag = "ScoreBtn";
                    if (i == par + 2)
                    {
                        scoreObj.transform.Find("score").GetComponent<TextMeshProUGUI>().text = $"{tempi}+";
                    }
                    else
                    {
                        scoreObj.transform.Find("score").GetComponent<TextMeshProUGUI>().text = $"{tempi}";
                    }

                    scoreObj.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        CourseManager.Instance.holeScores[curHole - 1] = tempi;
                        List<GameObject> btns = FindAllChildrenWithTag(holePanels[tempCount - 1].transform, "ScoreBtn");
                        foreach (GameObject btn in btns)
                        {
                            btn.GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
                        }
                        scoreObj.GetComponent<Image>().color = new Color(.117f, .0585f, .5686f, 1f);
                    });

                }

                holeObj.transform.Find("NextHoleBtn").GetComponent<Button>().onClick.AddListener(() =>
                {
                    updateHole("next");
                });

                holeObj.transform.Find("PrevHoleBtn").GetComponent<Button>().onClick.AddListener(() =>
                {
                    updateHole("prev");
                });
                holePanels.Add(holeObj);
                CourseManager.Instance.holeScores.Add(0);
                if (count == curHole)
                {
                    holePanels[count - 1].SetActive(true);
                }
                else
                {
                    holePanels[count - 1].SetActive(false);
                }
                count++;
            }
            // Default to first male tee box rating (expand later for tee selection)
            courseRating = selectedCourse?.tees?.course_rating ?? 72f;
            string courseName = selectedCourse?.course_name ?? "Unknown Course";
            string clubName = selectedCourse?.club_name ?? "Unknown Club";

            // Update UI
            string distanceText = courseDistance >= 0 ? $"{courseDistance:F1} km (~{courseDistance * 0.621371:F1} miles)" : "Unavailable";
            courseInfoText.text = $"Course: {courseName}\nClub: {clubName}\nRating: {courseRating:F1}\nDistance: {distanceText}";

        });
        
        // Setup submit button
        //submitButton.onClick.AddListener(CalculateAndSubmit);
    }

    public void updateHole(string direction)
    {
        if (direction == "next" && curHole < holePanels.Count)
        {
            holePanels[curHole - 1].SetActive(false);
            curHole += 1;
            holePanels[curHole - 1].SetActive(true);
        }
        else if (direction == "prev" && curHole > 1)
        {
            holePanels[curHole - 1].SetActive(false);
            curHole -= 1;
            holePanels[curHole - 1].SetActive(true);
        }
    }

    public void SubmitRound()
    {
        
    }
    
    public List<GameObject> FindAllChildrenWithTag(Transform parent, string tag)
    {
        List<GameObject> foundObjects = new List<GameObject>();

        // Iterate through immediate children
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                foundObjects.Add(child.gameObject);
            }

            // Recursively call for children of the current child
            List<GameObject> grandChildren = FindAllChildrenWithTag(child, tag);
            if (grandChildren.Count > 0)
            {
                foundObjects.AddRange(grandChildren);
            }
        }
        return foundObjects;
    }
}