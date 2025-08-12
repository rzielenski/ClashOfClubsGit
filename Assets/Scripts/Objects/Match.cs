using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Match
{
    public string match_id;
    public string match_type;
    public string format;
    public bool complete;
    public string stream_url;
    public string name;
    public bool is_public;
}

[System.Serializable]
public class MatchPlayer
{
    public string match_id;
    public string user_id;
    public string? clan_id;
    public int strokes;
    public int mulligans_used;
    public bool score_visible;
}

[System.Serializable]
public class MatchHistoryItem {
    public string match_id;
    public string match_type;
    public int score;
    public int? delta_elo;
    public bool won;
    public string name;
    public string format;
    public bool is_public;
    public string match_created_at;
    public string finalized_at;
    public short side;
    public float side1_avg_strokes;
    public float side2_avg_strokes;
}

