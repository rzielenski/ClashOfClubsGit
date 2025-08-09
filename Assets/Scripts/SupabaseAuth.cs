using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using SyE.BiometricsAuthentication;

public class SupabaseAuth : MonoBehaviour
{
    [Header("Supabase")]
    [SerializeField] private string SUPABASE_URL = "https://erqsrecsciorigewaihr.supabase.co";
    [SerializeField] private string SUPABASE_API_KEY = "<YOUR ANON KEY>"; // fill in Inspector

    [Header("UI")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField userInput;
    public Button signIn;
    public Button signUp;
    public TMP_Text statusText;

    private void Start()
    {
        var scene = SceneManager.GetActiveScene().name;

        if (scene == "SignIn")
        {
#if UNITY_IOS && !UNITY_EDITOR
            TryBiometricLogin();
#endif
            if (signIn != null) signIn.onClick.AddListener(OnSignInClicked);
        }
        else if (scene == "SignUp")
        {
            if (signUp != null) signUp.onClick.AddListener(OnSignUpClicked);
        }
    }

    // =========================
    // Biometric -> Refresh flow
    // =========================
    public void TryBiometricLogin()
    {
        Biometrics.Authenticate(
            onSuccess: () =>
            {
                statusText.text = "Biometric confirmed";
                var refresh = SecureStorage.Get("refresh_token");
                if (string.IsNullOrEmpty(refresh))
                {
                    statusText.text = "No saved session on this device. Sign in once.";
                    return;
                }

                StartCoroutine(RefreshSupabaseSession(refresh, (ok, access, refreshOut, expiresUtc, msg) =>
                {
                    if (!ok)
                    {
                        statusText.text = $"Session refresh failed: {msg}";
                        return;
                    }

                    if (!string.IsNullOrEmpty(refreshOut))
                        SecureStorage.Set("refresh_token", refreshOut);

                    // Fetch user and go
                    StartCoroutine(GetUserInfo(access, userId =>
                    {
                        if (string.IsNullOrEmpty(userId))
                        {
                            statusText.text = "Failed to fetch user.";
                            return;
                        }

                        // Set your app's user state
                        CourseManager.Instance.user.user_id = userId;
                        // Optionally fetch/set username here

                        SceneManager.LoadScene("ChooseAction");
                    }));
                }));
            },
            onFailure: () =>
            {
                statusText.text = "Biometric authentication failed.";
            }
        );
    }

    // =========================
    // Manual Sign In (password)
    // =========================
    private void OnSignInClicked()
    {
        string email = emailInput?.text?.Trim();
        string password = passwordInput?.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter email and password.";
            return;
        }

        StartCoroutine(PasswordGrantSignIn(email, password, (ok, message, access, refresh, expiresUtc) =>
        {
            statusText.text = message;
            if (!ok) return;

            if (!string.IsNullOrEmpty(refresh))
                SecureStorage.Set("refresh_token", refresh);

            // Fetch user id and do initial upserts
            StartCoroutine(GetUserInfo(access, userId =>
            {
                if (string.IsNullOrEmpty(userId))
                {
                    statusText.text = "Failed to fetch user ID";
                    return;
                }

                CourseManager.Instance.user.user_id = userId;

                // Upsert minimal profile & Elo using USER token (RLS-friendly)
                StartCoroutine(UpsertsAfterLogin(access, userId, email, () =>
                {
                    SceneManager.LoadScene("ChooseAction");
                }));
            }));
        }));
    }

    // ==========
    // Sign Up
    // ==========
    private void OnSignUpClicked()
    {
        string email = emailInput?.text?.Trim();
        string password = passwordInput?.text;
        string username = userInput?.text?.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter email and password.";
            return;
        }

        StartCoroutine(SignUpCoroutine(email, password, username, (success, message) =>
        {
            statusText.text = message;
            if (success)
            {
                // Usually requires email confirmation; no session/token saved here.
                emailInput.text = "";
                passwordInput.text = "";
            }
        }));
    }

    // =========================
    // HTTP helpers
    // =========================

    private IEnumerator PasswordGrantSignIn(
        string email,
        string password,
        Action<bool, string, string, string, DateTime?> cb)
    {
        string url = $"{SUPABASE_URL}/auth/v1/token?grant_type=password";
        var body = JsonConvert.SerializeObject(new { email, password });

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", SUPABASE_API_KEY);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            cb(false, $"Login failed: {ExtractMsg(req)}", null, null, null);
            yield break;
        }

        var resp = JObject.Parse(req.downloadHandler.text);
        string access = resp["access_token"]?.ToString();
        string refresh = resp["refresh_token"]?.ToString();
        int expiresIn = resp["expires_in"]?.ToObject<int>() ?? 3600;
        var expiresUtc = DateTime.UtcNow.AddSeconds(expiresIn);

        cb(true, "Login successful", access, refresh, expiresUtc);
    }

    private IEnumerator RefreshSupabaseSession(
        string refreshToken,
        Action<bool, string, string, DateTime?, string> cb)
    {
        string url = $"{SUPABASE_URL}/auth/v1/token?grant_type=refresh_token";
        var body = JsonConvert.SerializeObject(new { refresh_token = refreshToken });

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", SUPABASE_API_KEY);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            cb(false, null, null, null, ExtractMsg(req));
            yield break;
        }

        var resp = JObject.Parse(req.downloadHandler.text);
        string access = resp["access_token"]?.ToString();
        string refreshOut = resp["refresh_token"]?.ToString(); // may rotate
        int expiresIn = resp["expires_in"]?.ToObject<int>() ?? 3600;
        var expiresUtc = DateTime.UtcNow.AddSeconds(expiresIn);

        cb(!string.IsNullOrEmpty(access), access, refreshOut, expiresUtc, null);
    }

    private IEnumerator SignUpCoroutine(string email, string password, string username, Action<bool, string> cb)
    {
        string url = $"{SUPABASE_URL}/auth/v1/signup";
        var payload = new { email, password, data = new { display_name = username } };
        string json = JsonConvert.SerializeObject(payload);

        using var req = UnityWebRequest.Post(url, json, "application/json");
        req.SetRequestHeader("apikey", SUPABASE_API_KEY);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            cb(true, "Sign-up successful. Check your email to confirm.");
        }
        else
        {
            cb(false, $"Sign-up failed: {ExtractMsg(req)}");
        }
    }

    private IEnumerator GetUserInfo(string accessToken, Action<string> onUserId)
    {
        string url = $"{SUPABASE_URL}/auth/v1/user";
        using var req = UnityWebRequest.Get(url);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        req.SetRequestHeader("apikey", SUPABASE_API_KEY);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var result = JObject.Parse(req.downloadHandler.text);
            string userId = result["id"]?.ToString();
            onUserId?.Invoke(userId);
        }
        else
        {
            Debug.LogError("Failed to get user: " + ExtractMsg(req));
            onUserId?.Invoke(null);
        }
    }

    private IEnumerator UpsertsAfterLogin(string accessToken, string userId, string email, Action done)
    {
        // Users upsert
        string userUrl = $"{SUPABASE_URL}/rest/v1/Users";
        var userData = new Dictionary<string, object>
        {
            { "user_id", userId },
            { "email", email }
        };
        string userJson = JsonConvert.SerializeObject(userData);
        yield return PostToSupabase(userUrl, userJson, accessToken, "User", ignoreDups: true);

        // Elo upsert (solo)
        string eloUrl = $"{SUPABASE_URL}/rest/v1/PlayerEloRating";
        var eloData = new Dictionary<string, object>
        {
            { "match_type", "solo" },
            { "user_id", userId },
            { "elo_rating", 1200 }
        };
        string eloJson = JsonConvert.SerializeObject(eloData);
        yield return PostToSupabase(eloUrl, eloJson, accessToken, "ELO(solo)", ignoreDups: true);

        done?.Invoke();
    }

    private IEnumerator PostToSupabase(string url, string jsonData, string accessToken, string label, bool ignoreDups = false)
    {
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", SUPABASE_API_KEY);
        // CRUCIAL: use user's token so RLS can use auth.uid()
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        if (ignoreDups) req.SetRequestHeader("Prefer", "resolution=ignore-duplicates");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success || req.responseCode == 201)
            Debug.Log($"{label} upserted.");
        else
            Debug.LogError($"Failed to upsert {label}: {ExtractMsg(req)} | {req.downloadHandler.text}");
    }

    private static string ExtractMsg(UnityWebRequest www)
    {
        string server = www.downloadHandler?.text;
        if (!string.IsNullOrEmpty(server))
        {
            try
            {
                var err = JsonConvert.DeserializeObject<Dictionary<string, object>>(server);
                if (err != null)
                {
                    if (err.TryGetValue("error_description", out var ed)) return ed?.ToString();
                    if (err.TryGetValue("msg", out var msg)) return msg?.ToString();
                    if (err.TryGetValue("message", out var m2)) return m2?.ToString();
                }
            }
            catch { }
        }
        return $"{www.responseCode} {www.error}";
    }
}
