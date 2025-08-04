using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SupabaseAuth : MonoBehaviour
{
    private string SUPABASE_URL = "https://erqsrecsciorigewaihr.supabase.co";
    private string SUPABASE_API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVycXNyZWNzY2lvcmlnZXdhaWhyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTQxMTIwNjYsImV4cCI6MjA2OTY4ODA2Nn0.0M6QpU8h-_6zESOlyuXB3lkq7RXlOLXhKEPMCax14zU";
    public TMP_InputField emailInput; 
    public TMP_InputField passwordInput; 
    public TMP_InputField userInput;
    public Button signIn; // Assign in Inspector
    public Button signUp; // Assign in Inspector
    public TMP_Text statusText; // Assign in Inspector for feedback

    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        if (sceneName == "SignIn")
        {
            signIn.onClick.AddListener(OnSignInClicked);
        }
        else if (sceneName == "SignUp")
        {
           signUp.onClick.AddListener(OnSignUpClicked); 
        } 
    }

    void OnSignInClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter email and password.";
            return;
        }

        SignIn(email, password, (success, message, accessToken) =>
        {
            statusText.text = message;
            if (success)
            {
                PlayerPrefs.SetString("AuthToken", accessToken);
                PlayerPrefs.SetString("UserId", email); // Using email as user ID for simplicity
                SceneManager.LoadScene("ChooseAction");
            }
        });
    }

    void OnSignUpClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string username = userInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter email and password.";
            return;
        }

        SignUp(email, password, username, (success, message) =>
        {
            statusText.text = message; // Show feedback
            if (success)
            {
                // Optionally clear inputs or stay on login screen
                emailInput.text = "";
                passwordInput.text = "";
            }
        });
    }

    public void SignUp(string email, string password, string username, System.Action<bool, string> callback)
    {
        StartCoroutine(SignUpCoroutine(email, password, username, callback));
    }

    private IEnumerator SignUpCoroutine(string email, string password, string username, System.Action<bool, string> callback)
    {
        string userId = "";
        string url = $"{SUPABASE_URL}/auth/v1/signup";
        var data = new { email, password, username };
        string json = JsonConvert.SerializeObject(data);

        using (UnityWebRequest www = UnityWebRequest.Post(url, json, "application/json"))
        {
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                //var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
                var response = JObject.Parse(www.downloadHandler.text);
                userId = response["user"]?["id"]?.ToString();
                callback?.Invoke(true, "Sign-up successful. Check your email for confirmation.");

            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler.text != null)
                {
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);
                        errorMessage = errorResponse.ContainsKey("msg") ? errorResponse["msg"] : www.error;
                    }
                    catch
                    {
                        // Fallback to raw error
                    }
                }
                callback?.Invoke(false, $"Sign-up failed: {errorMessage}");
            }
        }

        

    }

    public void SignIn(string email, string password, System.Action<bool, string, string> callback)
    {
        StartCoroutine(SignInCoroutine(email, password, callback));
    }

    private IEnumerator SignInCoroutine(string email, string password, System.Action<bool, string, string> callback)
    {
        string url = $"{SUPABASE_URL}/auth/v1/token?grant_type=password";
        var data = new { email, password };
        string json = JsonConvert.SerializeObject(data);

        using (UnityWebRequest www = UnityWebRequest.Post(url, json, "application/json"))
        {
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                //var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
                var response = JObject.Parse(www.downloadHandler.text);
                string accessToken = response["access_token"].ToString();
                CourseManager.Instance.user_id = response["user"]?["id"]?.ToString();
                Debug.Log(response["user"]?["id"]?.ToString());
                callback?.Invoke(true, "Login successful", accessToken);
            }
            else
            {
                string errorMessage = www.error;
                if (www.downloadHandler.text != null)
                {
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);
                        errorMessage = errorResponse.ContainsKey("msg") ? errorResponse["msg"] : www.error;
                    }
                    catch
                    {
                        // Fallback to raw error
                    }
                }
                callback?.Invoke(false, $"Login failed: {errorMessage}", null);
            }
        }

        string newurl = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/Users";

        var userData = new Dictionary<string, object>
        {
            { "user_id", CourseManager.Instance.user_id },
            { "email", email }
        };

        string jsonData = JsonConvert.SerializeObject(userData);

        using (UnityWebRequest www = new UnityWebRequest(newurl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success || www.responseCode == 201)
            {
                Debug.Log("User Added successfully.");
                Debug.Log(www.downloadHandler.text);  // Optional: show inserted data
            }
            else
            {
                Debug.LogError($"Failed to Add User: {www.error}, Response: {www.downloadHandler.text}");
            }
        }
    }

    public void RequestPasswordReset(string email, System.Action<bool, string> callback)
    {
        StartCoroutine(RequestPasswordResetCoroutine(email, callback));
    }

    private IEnumerator RequestPasswordResetCoroutine(string email, System.Action<bool, string> callback)
    {
        string url = $"{SUPABASE_URL}/auth/v1/recover";
        var data = new { email };
        string json = JsonConvert.SerializeObject(data);

        using (UnityWebRequest www = UnityWebRequest.Post(url, json, "application/json"))
        {
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, "Password reset email sent.");
            }
            else
            {
                callback?.Invoke(false, $"Reset failed: {www.error}");
            }
        }
    }
}