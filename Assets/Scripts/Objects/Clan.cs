using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Clan
{
    public string clan_id;
    public string name;
    public string clan_mode;
    public int elo;
    public bool is_public;
    public string created_at;
    public int total_wins;
    public int total_matches;
}

[System.Serializable]
public class ClanJoinResponse
{
    public string clan_id;
    public string created_at;
    public string role;
    public string user_id;
    public Clan Clans;
}
