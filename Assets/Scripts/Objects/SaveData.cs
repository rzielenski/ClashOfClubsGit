using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;
using Newtonsoft.Json;

[System.Serializable]
public static class SaveData
{
    public static void SetData(Course course, List<int> scores, Match match)
    {
        string path = Path.Combine(Application.persistentDataPath, "saveData.json");
        GameData gameData = new GameData(course, scores, match);
        string jsonData = JsonConvert.SerializeObject(gameData);
        File.WriteAllText(path, jsonData);
    }
}

[System.Serializable]
public class GameData
{
    public Course course;
    public List<int> scores;
    public Match match = null;
    public GameData(Course _course, List<int> _scores, Match _match)
    {
        course = _course;
        scores = _scores;
        match = _match;
    }
    public GameData()
    {
    }
}

public static class SecureStorage
{
    public static void Set(string key, string value)
    {
        string encrypted = LocalEncryptor.Encrypt(value);
        PlayerPrefs.SetString(key, encrypted);
        PlayerPrefs.Save();
    }

    public static string Get(string key)
    {
        if (!PlayerPrefs.HasKey(key)) return null;
        string encrypted = PlayerPrefs.GetString(key);
        return LocalEncryptor.Decrypt(encrypted);
    }

    public static void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }
}

public static class LocalEncryptor
{
    private static readonly string key = "2OW7WV8jQ5IYcYR7etrMsuP4mAZA8zFI"; 
    private static readonly string iv = "j0XivQDrB984yso5";  

    public static string Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);

        using var encryptor = aes.CreateEncryptor();
        byte[] input = Encoding.UTF8.GetBytes(plainText);
        byte[] encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);

        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string cipherText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);

        using var decryptor = aes.CreateDecryptor();
        byte[] input = Convert.FromBase64String(cipherText);
        byte[] decrypted = decryptor.TransformFinalBlock(input, 0, input.Length);

        return Encoding.UTF8.GetString(decrypted);
    }
}
