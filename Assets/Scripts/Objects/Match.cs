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
