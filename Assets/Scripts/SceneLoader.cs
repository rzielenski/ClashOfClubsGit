using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    public void SignIn()
    {
        SceneManager.LoadScene("SignIn");
    }

    public void SignUp()
    {
        SceneManager.LoadScene("SignUp");
    }
    public void SelectCourse()
    {
        SceneManager.LoadScene("SelectCourse");
    }
}
