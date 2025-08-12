using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void Home()
    {
        SceneManager.LoadScene("ChooseAction");
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
    public void Profile()
    {
        SceneManager.LoadScene("Profile");
    }
}
