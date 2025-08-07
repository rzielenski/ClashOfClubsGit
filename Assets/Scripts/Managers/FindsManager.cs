using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using NativeCameraNamespace;
using UnityEngine.Networking;

public class FindsManager : MonoBehaviour
{
    // Set these in the inspector or dynamically
    public string supabaseUrl = "https://erqsrecsciorigewaihr.supabase.co";
    public string supabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVycXNyZWNzY2lvcmlnZXdhaWhyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTQxMTIwNjYsImV4cCI6MjA2OTY4ODA2Nn0.0M6QpU8h-_6zESOlyuXB3lkq7RXlOLXhKEPMCax14zU";
    public string bucketName = "finds"; // your Supabase bucket name

    public void TakePhoto()
    {
        NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                Debug.Log("Image saved at: " + path);
                StartCoroutine(UploadPhoto(path));
            }
        }, maxSize: 2048); // optional: limit size
    }

    private IEnumerator UploadPhoto(string filePath)
    {
        byte[] imageBytes = File.ReadAllBytes(filePath);
        string fileName = "user-upload-" + System.Guid.NewGuid().ToString() + ".jpg";

        string url = $"{supabaseUrl}/storage/v1/object/{bucketName}/{fileName}";

        UnityWebRequest request = UnityWebRequest.Put(url, imageBytes);
        request.SetRequestHeader("apikey", supabaseApiKey);
        request.SetRequestHeader("Authorization", "Bearer " + supabaseApiKey);
        request.SetRequestHeader("Content-Type", "image/jpeg");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Upload successful!");
        }
        else
        {
            Debug.LogError("Upload failed: " + request.error + "\n" + request.downloadHandler.text);
        }
    }


}
