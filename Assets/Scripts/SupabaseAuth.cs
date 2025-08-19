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
using System.IO;
using SyE.BiometricsAuthentication;

public class SupabaseAuth : MonoBehaviour
{
    // ---------- Singleton & persistence ----------
    public static SupabaseAuth Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persist through scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // ---------- Session ----------
    public string AccessToken { get; private set; }

    // Keys & path used for local/session cleanup
    private const string RefreshTokenKey = "refresh_token";
    private static string SavePath => Path.Combine(Application.persistentDataPath, "saveData.json");

    // ---------- Supabase ----------
    [Header("Supabase")]
    [SerializeField] private string SUPABASE_URL = "https://erqsrecsciorigewaihr.supabase.co";
    [SerializeField] private string SUPABASE_API_KEY = "";

    // ---------- UI ----------
    [Header("UI")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField userInput;
    public Button signIn;
    public Button signUp;
    public TMP_Text statusText;

    private void Start()
    {
        // Allow other scripts to fetch current access token (e.g., AccountDeletion)
        AccountDeletion.GetAccessToken = () => AccessToken;

        var scene = SceneManager.GetActiveScene().name;

        if (scene == "SignIn")
        {
#if UNITY_IOS && !UNITY_EDITOR
            TryBiometricLogin();
#endif
        }
        
    }

    // ============================================================
    // Public: Sign Out (call from Profile/Settings in any scene)
    // ============================================================
    public void OnSignOutClicked()
    {
        StartCoroutine(SignOutAndWipeCoroutine(() =>
        {
            // After sign out + wipe, go to SignIn scene
            SceneManager.LoadScene("SignIn");
        }));
    }

    private IEnumerator SignOutAndWipeCoroutine(Action onDone)
    {
        // 1) Best effort: revoke session on Supabase (optional)
        yield return StartCoroutine(TrySupabaseLogout());

        // 2) Wipe local data (refresh token + saveData.json)
        WipeLocalData();

        // 3) Clear in-memory state
        AccessToken = null;
        if (CourseManager.Instance != null) CourseManager.Instance.user = null;

        onDone?.Invoke();
    }

    private IEnumerator TrySupabaseLogout()
    {
        if (string.IsNullOrEmpty(AccessToken))
            yield break;

        string url = $"{SUPABASE_URL}/auth/v1/logout";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("apikey", SUPABASE_API_KEY);
            req.SetRequestHeader("Authorization", $"Bearer {AccessToken}");

            // Some proxies expect a body
            byte[] body = System.Text.Encoding.UTF8.GetBytes("{}");
            req.uploadHandler = new UploadHandlerRaw(body);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Supabase logout failed: {req.responseCode} {req.error} {req.downloadHandler.text}");
            }
        }
    }

    private void WipeLocalData()
    {
        // Remove refresh token from secure storage (Keychain/PlayerPrefs)
        try { SecureStorage.Delete(RefreshTokenKey); } catch {}

        // Delete saved game/session file
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch {}
    }

    // =========================
    // Biometric -> Refresh flow
    // =========================
    public void TryBiometricLogin()
    {
        Biometrics.Authenticate(
            onSuccess: () =>
            {
                if (statusText) statusText.text = "Biometric confirmed";
                var refresh = SecureStorage.Get(RefreshTokenKey);
                if (string.IsNullOrEmpty(refresh))
                {
                    if (statusText) statusText.text = "No saved session on this device. Sign in once.";
                    return;
                }

                StartCoroutine(RefreshSupabaseSession(refresh, (ok, access, refreshOut, expiresUtc, msg) =>
                {
                    if (!ok)
                    {
                        if (statusText) statusText.text = $"Session refresh failed: {msg}";
                        return;
                    }

                    AccessToken = access;

                    if (!string.IsNullOrEmpty(refreshOut))
                        SecureStorage.Set(RefreshTokenKey, refreshOut);

                    // Fetch user and go
                    StartCoroutine(GetUserInfo(access, user =>
                    {
                        if (user == null)
                        {
                            if (statusText) statusText.text = "Failed to fetch user.";
                            return;
                        }

                        CourseManager.Instance.user = user;
                        SceneManager.LoadScene("ChooseAction");
                    }));
                }));
            },
            onFailure: () =>
            {
                if (statusText) statusText.text = "Biometric authentication failed.";
            }
        );
    }

    // =========================
    // Manual Sign In (password)
    // =========================
    public void OnSignInClicked(string _email = null, string _password = null)
    {

        string email = ""; 
        string password = "";
        if (_email == null)
        {
            email = emailInput?.text?.Trim();
        }
        else
        {
            email = _email;
        }

        if (_password == null)
        {
            password = passwordInput?.text;
        }
        else
        {
            password = _password;
        }



        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (statusText) statusText.text = "Please enter email and password.";
            return;
        }

        StartCoroutine(PasswordGrantSignIn(email, password, (ok, message, access, refresh, expiresUtc) =>
        {
            if (statusText) statusText.text = message;
            if (!ok) return;

            AccessToken = access;
            if (!string.IsNullOrEmpty(refresh))
                SecureStorage.Set(RefreshTokenKey, refresh);

            // Fetch user & upserts
            StartCoroutine(GetUserInfo(access, user =>
            {
                if (user == null)
                {
                    if (statusText) statusText.text = "Failed to fetch user";
                    return;
                }

                CourseManager.Instance.user = user;

                StartCoroutine(UpsertsAfterLogin(access, user.user_id, user.email, () =>
                {
                    SceneManager.LoadScene("ChooseAction");
                }));
            }));
        }));
    }

    // ========== Sign Up ==========
    public void OnSignUpClicked(string _email = null, string _password = null, string _username = null)
    {
        string email = ""; 
        string password = ""; 
        string username = "";
        if (_email == null)
        {
            email = emailInput?.text?.Trim();
        }
        else
        {
            email = _email;
        }
        
        if (_password == null)
        {
            password = passwordInput?.text;
        }
        else
        {
            password = _password;
        }

        if (_username == null)
        {
            username = userInput?.text?.Trim();
        }
        else
        {
            username = _username;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (statusText) statusText.text = "Please enter email and password.";
            return;
        }

        StartCoroutine(SignUpCoroutine(email, password, username, (success, message) =>
        {
            if (statusText) statusText.text = message;
            if (success)
            {
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

    private IEnumerator GetUserInfo(string accessToken, Action<User> onUser)
    {
        string url = $"{SUPABASE_URL}/auth/v1/user";
        using var req = UnityWebRequest.Get(url);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        req.SetRequestHeader("apikey", SUPABASE_API_KEY);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onUser?.Invoke(null);
            yield break;
        }

        var auth = JObject.Parse(req.downloadHandler.text);
        string userId  = auth["id"]?.ToString();
        string email   = auth["email"]?.ToString();
        string display = auth["user_metadata"]?["display_name"]?.ToString();

        // Fetch app user row
        string userUrl = $"{SUPABASE_URL}/rest/v1/Users?user_id=eq.{userId}&select=*";
        using (var req2 = UnityWebRequest.Get(userUrl))
        {
            req2.downloadHandler = new DownloadHandlerBuffer();
            req2.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            req2.SetRequestHeader("apikey", SUPABASE_API_KEY);
            yield return req2.SendWebRequest();

            if (req2.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Users row fetch failed: " + ExtractMsg(req2));
                onUser?.Invoke(null);
                yield break;
            }

            var arr = JArray.Parse(req2.downloadHandler.text);
            if (arr.Count > 0)
            {
                var user = arr[0].ToObject<User>();
                onUser?.Invoke(user);
            }
            else
            {
                onUser?.Invoke(new User
                {
                    user_id = userId,
                    email = email,
                    username = string.IsNullOrEmpty(display) ? null : display
                });
            }
        }
    }

    private IEnumerator UpsertsAfterLogin(string accessToken, string userId, string email, Action done)
    {
        // Users upsert (keep simple; you can add elo upserts later if needed)
        string username = CourseManager.Instance.user?.username;
        string userUrl = $"{SUPABASE_URL}/rest/v1/Users";
        var userData = new Dictionary<string, object>
        {
            { "user_id", userId },
            { "email", email },
            { "username", username }
        };
        string userJson = JsonConvert.SerializeObject(userData);
        yield return PostToSupabase(userUrl, userJson, accessToken, "User", ignoreDups: true);

        done?.Invoke();
    }

    private IEnumerator PostToSupabase(string url, string jsonData, string accessToken, string label, bool ignoreDups = false)
    {
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", SUPABASE_API_KEY);
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
