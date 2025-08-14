using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class AccountDeletion : MonoBehaviour
{
    [SerializeField] private string supabaseUrl = "https://erqsrecsciorigewaihr.supabase.co";
    [SerializeField] private string functionName = "delete_account";

    public static Func<string> GetAccessToken;

    public void DeleteAccount()
    {
        DeleteMyAccount((success, message) =>
        {
            if (success)
            {
                Debug.Log("Account deleted successfully.");
            }
            else
            {
                Debug.LogError($"Account deletion failed: {message}");
            }
        });
    }
    public void DeleteMyAccount(Action<bool, string> callback)
    {
        StartCoroutine(DeleteCoroutine(callback));
    }

    private IEnumerator DeleteCoroutine(Action<bool, string> callback)
    {
        var token = GetAccessToken?.Invoke();
        if (string.IsNullOrEmpty(token))
        {
            callback?.Invoke(false, "No valid session token found.");
            yield break;
        }

        string url = $"{supabaseUrl}/functions/v1/{functionName}";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.SetRequestHeader("Authorization", $"Bearer {token}");
            req.SetRequestHeader("Content-Type", "application/json");

            byte[] body = Encoding.UTF8.GetBytes("{}");
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200)
            {
                Debug.Log("Account deletion successful.");
                callback?.Invoke(true, null);
            }
            else
            {
                Debug.LogError($"Account deletion failed: {req.downloadHandler.text}");
                callback?.Invoke(false, $"HTTP {req.responseCode}: {req.downloadHandler.text}");
            }
        }
    }
}
