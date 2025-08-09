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


