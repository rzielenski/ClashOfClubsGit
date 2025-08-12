using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class User
{
    public string user_id;
    public string username;
    public string email;
    public string created_at;
    public int gems_balance;
    public int total_wins;
    public int total_matches;
    public int elo;
}
