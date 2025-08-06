using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class User
{
    public string user_id;
    public string username;
    public int gem_balance;
    public int total_wins;
    public int total_matches;
    public PlayerEloRating solo;
}
[System.Serializable]
public class PlayerEloRating
{
    public string match_type;
    public string user_id;
    public int elo_rating;

    public PlayerEloRating(string mt, string uid, int r)
    {
        match_type = mt;
        user_id = uid;
        elo_rating = r;
    }
}