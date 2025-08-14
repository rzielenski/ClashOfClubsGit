using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class SignUpManager : MonoBehaviour
{
    public Button signUpButton;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField userInput;

    // Start is called before the first frame update
    void Start()
    {
        if (SceneManager.GetActiveScene().name == "SignUp")
        {
            signUpButton.onClick.AddListener(() =>
            {
                SupabaseAuth.Instance.OnSignUpClicked(
                    emailInput?.text?.Trim(),
                    passwordInput?.text,
                    userInput?.text?.Trim()
                );
            });
        }
        else if (SceneManager.GetActiveScene().name == "SignIn")
        {
            signUpButton.onClick.AddListener(() =>
            {
                SupabaseAuth.Instance.OnSignInClicked(
                    emailInput?.text?.Trim(),
                    passwordInput?.text
                );
            });
        }
        
    }

}
