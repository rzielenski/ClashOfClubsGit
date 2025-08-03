using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectCourseManager : MonoBehaviour
{
    public TMP_InputField input;
    // Start is called before the first frame update
    void Start()
    {
        input.onEndEdit.AddListener((text) => { APIHandler.Instance.SearchCourse(); });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
