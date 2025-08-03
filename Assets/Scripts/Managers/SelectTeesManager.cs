using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectTeesManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        APIHandler.Instance.GetTees();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
