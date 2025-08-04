using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseActionManager : MonoBehaviour
{
    public Button practiceBtn;
    public GameObject matchPanel;
    public GameObject clanPanel;
    // Start is called before the first frame update
    void Start()
    {
        practiceBtn.onClick.AddListener(() => { CourseManager.Instance.SetRoundType("practice"); });
    }

    
    public void OpenCloseMatch()
    {
        if (matchPanel.activeInHierarchy)
        {
            matchPanel.SetActive(false);
        }
        else
        {
            matchPanel.SetActive(true);
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
