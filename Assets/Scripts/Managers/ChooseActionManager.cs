using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseActionManager : MonoBehaviour
{
    public Button practiceBtn;
    // Start is called before the first frame update
    void Start()
    {
        practiceBtn.onClick.AddListener(() => { CourseManager.Instance.SetRoundType("practice"); });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
