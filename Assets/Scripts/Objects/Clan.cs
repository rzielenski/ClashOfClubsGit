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

[System.Serializable]
public class ClanMatchHistoryItem {
    public string match_id;
    public string match_type;
    public short? clan_side;
    public float? clan_avg_strokes;
    public float? opponent_avg_strokes;
    public int delta_elo;          // signed for the clan
    public bool won;
    public string name;
    public string format;
    public bool is_public;
    public string match_created_at;
    public string finalized_at;
    public string[] opponent_clans; // uuids as strings
}

[System.Serializable]
public class ClanMemberWithUser
{
    public string clan_id;
    public string user_id;
    public string role;
    public string created_at;
    public User Users;   // joined user row
}


[System.Serializable] public class CountRow { public int count; }

[System.Serializable]
public class ClanSearchItem {
    public string clan_id;
    public string name;
    public string clan_mode;
    public bool   is_public;
    public long   elo;
    public int?   max_players;           // smallint -> int?
    public List<CountRow> member_count;  // from ClanMembers(count) embed
}

