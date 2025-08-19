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
    public string supabaseApiKey = "";
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
