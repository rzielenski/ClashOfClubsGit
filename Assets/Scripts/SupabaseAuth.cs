using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using SyE.BiometricsAuthentication;

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
#if !UNITY_EDITOR
            TryBiometricLogin();
#endif
            signIn.onClick.AddListener(OnSignInClicked);
        }
        else if (sceneName == "SignUp")
        {
            signUp.onClick.AddListener(OnSignUpClicked);
        }
    }

    public void TryBiometricLogin()
    {
        Biometrics.Authenticate(
            onSuccess: () =>
            {
                Debug.Log("Biometric authentication succeeded.");

                string email = SecureStorage.Get("email");
                string password = SecureStorage.Get("password");

                statusText.text = "Biometric confirmed";

                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
                {
                    StartCoroutine(SignInCoroutine(email, password, (success, message, token) =>
                    {
                        Debug.Log($"Sign-in status: {message}");
                    }));
                }
                else
                {
                    Debug.LogWarning("Email or password not found in secure storage.");
                    statusText.text = "Email/password not found.";
                }
            },
            onFailure: () =>
            {
                Debug.LogWarning("Biometric authentication failed.");
                statusText.text = "Biometric authentication failed.";
            }
        );
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
                SecureStorage.Set("email", email);
                SecureStorage.Set("password", password);
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
                
                SecureStorage.Set("email", email);
                SecureStorage.Set("password", password);

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
        string authUrl = $"{SUPABASE_URL}/auth/v1/token?grant_type=password";
        var credentials = new { email, password };
        string json = JsonConvert.SerializeObject(credentials);

        using (UnityWebRequest www = new UnityWebRequest(authUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = www.error;
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);
                    if (errorResponse.ContainsKey("msg"))
                        errorMessage = errorResponse["msg"];
                }
                catch { }

                callback?.Invoke(false, $"Login failed: {errorMessage}", null);
                yield break;
            }

            statusText.text = "Login successful";
            var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
            Debug.Log(tokenResponse);
            string accessToken = tokenResponse["access_token"].ToString();

            // Wait for user ID from Supabase
            string userId = null;
            yield return StartCoroutine(GetUserInfo(accessToken, id => userId = id));

            if (string.IsNullOrEmpty(userId))
            {
                callback?.Invoke(false, "Failed to fetch user ID", null);
                yield break;
            }

            Debug.Log("User ID fetched: " + userId);
            CourseManager.Instance.user.user_id = userId;
            

            //callback?.Invoke(true, "Login successful", accessToken);
        }

        // Insert User
        string userUrl = $"{SUPABASE_URL}/rest/v1/Users";
        var userData = new Dictionary<string, object>
        {
            { "user_id", CourseManager.Instance.user.user_id },
            { "email", email }
        };
        string userJson = JsonConvert.SerializeObject(userData);
        yield return PostToSupabase(userUrl, userJson, "User", true);
        
        // Insert Elo
        string eloUrl = $"{SUPABASE_URL}/rest/v1/PlayerEloRating";
        
        string user_id = CourseManager.Instance.user.user_id;
        
        var eloData = new Dictionary<string, object>
        {
            { "match_type", "solo" },
            { "user_id",  user_id},
            { "elo_rating", 1200 }
        };
        string eloJson = JsonConvert.SerializeObject(eloData);
        yield return PostToSupabase(eloUrl, eloJson, $"ELO for solo", true);
        
        SceneManager.LoadScene("ChooseAction");
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

    private IEnumerator PostToSupabase(string url, string jsonData, string label, bool ignoreDups = false)
    {
        
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");


            if (ignoreDups)
            {
                www.SetRequestHeader("Prefer", "resolution=ignore-duplicates");
            }

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success || www.responseCode == 201)
            {
                Debug.Log($"{label} inserted.");
            }
            else
            {
                Debug.LogError($"Failed to insert {label}: {www.error} | {www.downloadHandler.text}");
            }
        }
    }

    
    private IEnumerator GetUserInfo(string accessToken, Action<string> onUserIdReceived)
    {
        string url = $"{SUPABASE_URL}/auth/v1/user";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
                if (result.TryGetValue("id", out object userIdObj))
                {
                    string userId = userIdObj.ToString();
                    onUserIdReceived?.Invoke(userId);
                    yield break;
                }
            }

            Debug.LogError("Failed to get user ID: " + www.error + " | " + www.downloadHandler.text);
            onUserIdReceived?.Invoke(null);
        }
    }



}