using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ClanManager : MonoBehaviour
{
    public TextMeshProUGUI clanNameText;
    public TextMeshProUGUI clanElo;

    void Start()
    {
        clanElo.text = CourseManager.Instance.SelectedClan.elo.ToString();
        clanNameText.text = CourseManager.Instance.SelectedClan.name;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
