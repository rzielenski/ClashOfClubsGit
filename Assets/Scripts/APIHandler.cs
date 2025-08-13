using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;


public class APIHandler : MonoBehaviour
{
    [System.Serializable]
    public class StorageListItem {
        public string name;
        public string created_at;
    }

    private string SUPABASE_URL = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/";
    private string SUPABASE_API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVycXNyZWNzY2lvcmlnZXdhaWhyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTQxMTIwNjYsImV4cCI6MjA2OTY4ODA2Nn0.0M6QpU8h-_6zESOlyuXB3lkq7RXlOLXhKEPMCax14zU";
    public static APIHandler Instance { get; private set; }
    public GameObject courseButtonPrefab;
    public GameObject clanButtonPrefab;
    public GameObject eloPrefab;
    public GameObject teesButtonPrefab;
    public LocationData locationData;
    public GameObject findImageItemPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }


    // -----------------  COURSE HELPER FUNCTIONS -----------------
    public void GetHoles(System.Action callback)
    {
        string id = CourseManager.Instance.SelectedCourse.tees.teebox_id;
        string url = $"{SUPABASE_URL}Holes?teebox_id=eq.{id}&select=*";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<Hole> holes = JsonConvert.DeserializeObject<List<Hole>>(json);

            CourseManager.Instance.SelectedCourse.tees.holes = holes;
            callback?.Invoke();
        }));
    }

    public void GetTees()
    {
        string id = CourseManager.Instance.SelectedCourse.course_id;
        string url = $"{SUPABASE_URL}Teeboxes?course_id=eq.{id}&select=*";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<TeeBox> tees = JsonConvert.DeserializeObject<List<TeeBox>>(json);
            Transform scrollView = GameObject.FindWithTag("TeesView").transform;
            foreach (Transform child in scrollView)
            {
                Destroy(child.gameObject);
            }
            foreach (var tee in tees)
            {
                var temptee = tee;
                GameObject buttonObj = Instantiate(teesButtonPrefab, scrollView);
                buttonObj.transform.Find("TeeName").GetComponentInChildren<TextMeshProUGUI>().text = tee.name;
                buttonObj.transform.Find("Yardage").GetComponentInChildren<TextMeshProUGUI>().text = tee.total_yards.ToString();
                buttonObj.transform.Find("Par").GetComponentInChildren<TextMeshProUGUI>().text = tee.par.ToString();

                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Debug.Log("Making onclick");
                    CourseManager.Instance.SelectedCourse.tees = temptee;
                    SceneManager.LoadScene("Scorecard");
                });
            }
            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(scrollView.GetComponent<RectTransform>().sizeDelta.x, scrollView.childCount * 150);
        }));
    }

    public void SearchCourse()
    {
        TMP_InputField input = GameObject.FindWithTag("CourseSearchInput").GetComponent<TMP_InputField>();
        string searchName = UnityWebRequest.EscapeURL(input.text);
        string url = $"{SUPABASE_URL}Courses?course_name=ilike.%25{searchName}%25";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<Course> courses = JsonConvert.DeserializeObject<List<Course>>(json);

            Transform scrollView = GameObject.FindWithTag("CoursesView").transform;
            foreach (Transform child in scrollView)
                Destroy(child.gameObject);

            foreach (var course in courses)
            {
                float _distance = 0;
                GameObject buttonObj = Instantiate(courseButtonPrefab, scrollView);
                buttonObj.transform.Find("CourseName").GetComponentInChildren<TextMeshProUGUI>().text = course.course_name;

                locationData.GetCourseDist(course.latitude, course.longitude, distance =>
                {
                    if (distance >= 0)
                    {
                        GameObject distText = buttonObj.transform.Find("Dist").gameObject;
                        distText.GetComponentInChildren<TextMeshProUGUI>().text = distance.ToString("F2");
                        _distance = distance;
                    }
                });

                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    CourseManager.Instance.SelectedCourse = course;
                    CourseManager.Instance.CourseDistance = _distance;
                    SceneManager.LoadScene("SelectTees");
                });
            }
            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(scrollView.GetComponent<RectTransform>().sizeDelta.x, scrollView.childCount * 150);
        }));
    }

    public void GetBestClanMatch(string clan_id, string clan_type, System.Action<Match> callback)
    {
        string url = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/rpc/get_best_match";
        var requestBody = new Dictionary<string, object>
        {
            { "p_clan_id", clan_id },
            { "p_match_type", clan_type }
        };
        string jsonData = JsonConvert.SerializeObject(requestBody);
        Debug.Log($"Requesting best match for clan {clan_id} with data: {jsonData}");
        StartCoroutine(PostData(url, jsonData, (responseJson) =>
        {
            if (string.IsNullOrEmpty(responseJson))
            {
                Debug.Log("No match found.");
                callback(null);
                return;
            }

            try
            {
                Match match = JsonConvert.DeserializeObject<Match>(responseJson);
                callback(match);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing match response: " + e.Message);
                callback(null);
            }
        }));
    }

    public void GetBestMatch(string user_id, System.Action<Match> callback)
    {
        string url = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/rpc/get_best_match";
        var requestBody = new Dictionary<string, object>
        {
            { "p_user_id", user_id },
            { "p_match_type", "solo" },
            { "p_format", "stroke" }
        };
        string jsonData = JsonConvert.SerializeObject(requestBody);
        Debug.Log($"Requesting best match for user {user_id} with data: {jsonData}");
        StartCoroutine(PostData(url, jsonData, (responseJson) =>
        {
            if (string.IsNullOrEmpty(responseJson))
            {
                Debug.Log("No match found.");
                callback(null);
                return;
            }

            try
            {
                Match match = JsonConvert.DeserializeObject<Match>(responseJson);
                callback(match);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing match response: " + e.Message);
                callback(null);
            }
        }));
    }

    public void CreateMatch(string match_type, string format, string name, bool is_public, bool is_practice)
    {
        string url = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/Matches?select*";

        var matchData = new Dictionary<string, object>
        {
            { "match_type", match_type },
            { "format", format },
            { "name", name },
            { "is_public", is_public },
            { "is_practice", is_practice }
        };

        string jsonData = JsonConvert.SerializeObject(matchData);

        StartCoroutine(PostData(url, jsonData, resjson =>
        {
            if (string.IsNullOrEmpty(resjson))
            {
                Debug.LogError("Match insert returned empty response.");
                return;
            }

            try
            {
                List<Match> matches = JsonConvert.DeserializeObject<List<Match>>(resjson);
                if (matches == null || matches.Count == 0)
                {
                    Debug.LogError("Match insert response did not contain a valid match.");
                    return;
                }

                Match match = matches[0];

                string newurl = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/MatchPlayers";

                var matchPlayerData = new Dictionary<string, object>
                {
                    { "match_id", match.match_id },
                    { "user_id", CourseManager.Instance.user.user_id },
                    { "side", 1 }
                };

                string newJsonData = JsonConvert.SerializeObject(matchPlayerData);
                StartCoroutine(PostData(newurl, newJsonData, json => { }));

                CourseManager.Instance.curMatch = match;
                Debug.Log($"Created match with ID: {match.match_id}");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing Supabase response: " + e.Message);
            }
        }));
    }

    // Add this method to APIHandler.cs
    public void VoidOldMatches(System.Action<bool> callback = null)
    {
        string url = $"{SUPABASE_URL}rpc/void_old_matches";
        string jsonData = "{}"; // No parameters needed

        StartCoroutine(PostData(url, jsonData, response =>
        {
            bool success = response != null; // Success if not null ("" for VOID); failure if null
            if (success)
            {
                Debug.Log("Old matches voided successfully.");
            }
            else
            {
                Debug.LogError("Failed to void old matches.");
            }
            callback?.Invoke(success);
        }));
    }

    public void CreateMatchPlayer(Match match)
    {
        string url = SUPABASE_URL + "MatchPlayers";
        var matchPlayerData = new Dictionary<string, object>
        {
            { "match_id", match.match_id },
            { "user_id", CourseManager.Instance.user.user_id },
            { "side", 2 }
        };
        string jsonData = JsonConvert.SerializeObject(matchPlayerData);
        StartCoroutine(PostData(url, jsonData, json => { }));
        CourseManager.Instance.curMatch = match;
    }

	public void CreateClanMatch(string clanId, List<string> userIds, string matchType, string match_format, string matchName, bool isPublic, bool isPractice)
    {
        string url = $"{SUPABASE_URL}Matches?select=*";
        int max = matchType == "duo" ? 2 : 4;
        var matchData = new Dictionary<string, object>
        {
            { "match_type", matchType },     // e.g. "duo" / "squad"
            { "format", match_format },      // e.g. "stroke" / "scramble"
            { "name", matchName },
            { "is_public", isPublic },
            { "is_practice", isPractice },
            { "max_players", 2 * max }
        };

        string jsonData = JsonConvert.SerializeObject(matchData);
        StartCoroutine(PostData(url, jsonData, (matchJson) =>
        {
            if (string.IsNullOrEmpty(matchJson))
            {
                Debug.LogError("Failed to create match.");
                return;
            }

            try
            {
                var created = JsonConvert.DeserializeObject<List<Match>>(matchJson);
                if (created == null || created.Count == 0)
                {
                    Debug.LogError("Create match returned no rows.");
                    return;
                }

                var match = created[0];
                CourseManager.Instance.curMatch = match;

                AddClanPlayersToMatch(match.match_id, clanId, userIds, 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateClanMatch parse error: {e.Message} | raw: {matchJson}");
            }
        }));
    }


	public void AddClanPlayersToMatch(string matchId, string clanId, List<string> userIds, int side)
    {
        string url = $"{SUPABASE_URL}MatchPlayers";
        foreach (var userId in userIds)
        {
            var playerData = new Dictionary<string, object>
            {
                { "match_id", matchId },
                { "user_id", userId },
                { "clan_id", clanId },
                { "side", side }
            };
            string jsonData = JsonConvert.SerializeObject(playerData);
            Debug.Log(jsonData);
            StartCoroutine(PostData(url, jsonData, json => { }));
        }
    }

	public void GetBestClanMatch(string clanId, string clanMode, string matchFormat, System.Action<Match> callback)
    {
        string url = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/rpc/get_best_match";
        var body = new Dictionary<string, object>
        {
            { "p_user_id",  null },          // not used for clan
            { "p_clan_id",  clanId },
            { "p_match_type", clanMode },    // 'duo'/'squad'
            { "p_format", matchFormat }      // 'stroke'/'scramble'
        };
        string json = JsonConvert.SerializeObject(body);
        Debug.Log(json);
        StartCoroutine(PostData(url, json, responseJson =>
        {
            if (string.IsNullOrEmpty(responseJson) || responseJson == "null" || responseJson == "{}")
            {
                callback(null);
                return;
            }

            try
            {
                // Try array first (optional, but kept for robustness)
                var matches = JsonConvert.DeserializeObject<List<Match>>(responseJson);
                if (matches != null && matches.Count > 0)
                {
                    var match = matches[0];
                    if (!string.IsNullOrEmpty(match.match_id))
                    {
                        callback(match);
                        return;
                    }
                }
            }
            catch { /* fall through */ }

            try
            {
                // Fallback: single object
                var single = JsonConvert.DeserializeObject<Match>(responseJson);
                if (single != null && !string.IsNullOrEmpty(single.match_id))
                {
                    callback(single);
                }
                else
                {
                    callback(null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing best clan match response: " + e.Message + " | " + responseJson);
                callback(null);
            }
        }));
    }


        public void GetClanMembers(string clanId, System.Action<List<ClanMemberWithUser>> cb)
	{
	    // Join Users to get username/email/elo/etc.
	    string url = $"{SUPABASE_URL}ClanMembers?clan_id=eq.{clanId}&select=*,Users(*)&order=created_at.asc";

	    StartCoroutine(GetRequest(url, json =>
	    {
		if (string.IsNullOrEmpty(json))
		{
		    Debug.LogError("No JSON returned from GetClanMembers.");
		    cb?.Invoke(new List<ClanMemberWithUser>());
		    return;
		}
		try
		{
		    var members = JsonConvert.DeserializeObject<List<ClanMemberWithUser>>(json) 
				  ?? new List<ClanMemberWithUser>();
		    cb?.Invoke(members);
		}
		catch (System.Exception e)
		{
		    Debug.LogError("GetClanMembers parse error: " + e.Message);
		    cb?.Invoke(new List<ClanMemberWithUser>());
		}
	    }));
	}
	

    // -----------------  FINDS HELPER FUNCTIONS -----------------
    
    public void GetTopFinds()
    {
        string url = "https://erqsrecsciorigewaihr.supabase.co/storage/v1/object/list/finds";
        var payload = new {
            prefix = "",
            limit = 10,
            offset = 0,
            sortBy = new { column = "created_at", order = "desc" }
        };
        string body = JsonConvert.SerializeObject(payload);
        StartCoroutine(PostJson(url, body, (ok, json) =>
        {
            if (!ok || string.IsNullOrEmpty(json))
            {
                Debug.LogError("Failed to list files");
                return;
            }

            var items = JsonConvert.DeserializeObject<List<StorageListItem>>(json);
            Transform content = GameObject.Find("TopViewGroup").transform.Find("TopView3/TabViewLine_FillExpand/ViewGroup/View4/ScrollView/Viewport/Content");
            foreach (Transform child in content) Destroy(child.gameObject);

            foreach (var it in items)
            {
                string publicUrl = $"https://erqsrecsciorigewaihr.supabase.co/storage/v1/object/public/finds/{it.name}";
                GameObject go = Instantiate(findImageItemPrefab, content);
                var raw = go.GetComponentInChildren<UnityEngine.UI.RawImage>();
                StartCoroutine(LoadImage(publicUrl, tex => { if (raw) raw.texture = tex; }));
            }

            content.GetComponent<RectTransform>().sizeDelta = new Vector2(content.GetComponent<RectTransform>().sizeDelta.x, content.childCount * 900);
        }));
    }
    private IEnumerator PostJson(string url, string json, System.Action<bool,string> cb)
    {
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", SUPABASE_API_KEY);
        req.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");

        yield return req.SendWebRequest();

        var body = req.downloadHandler?.text;
        if (req.result == UnityWebRequest.Result.Success)
            cb(true, body);
        else
        {
            Debug.LogError($"List failed: {req.responseCode} {req.error} | {body}");
            cb(false, null);
        }
    }


    private IEnumerator LoadImage(string url, System.Action<Texture2D> cb)
    {
        using var www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        cb(www.result == UnityWebRequest.Result.Success ? DownloadHandlerTexture.GetContent(www) : null);
    }

    // -----------------  CLAN HELPER FUNCTIONS -----------------

    public void GetUserClans()
    {

        //string url = $"{SUPABASE_URL}ClanMembers?user_id=eq.{CourseManager.Instance.user.user_id}";
 
        string url = $"{SUPABASE_URL}ClanMembers?user_id=eq.{CourseManager.Instance.user.user_id}&select=*,Clans(*)";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<ClanJoinResponse> clans = JsonConvert.DeserializeObject<List<ClanJoinResponse>>(json);

            Transform scrollView = GameObject.Find("TopViewGroup").transform.Find("View2").Find("CurrentClans").Find("Viewport").Find("Content");
            foreach (Transform child in scrollView)
                Destroy(child.gameObject);

            foreach (var clan in clans)
            {
                float _distance = 0;
                GameObject buttonObj = Instantiate(clanButtonPrefab, scrollView);
                buttonObj.transform.Find("ClanName").GetComponentInChildren<TextMeshProUGUI>().text = clan.Clans.name;

                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    CourseManager.Instance.SelectedClan = clan.Clans;
                    SceneManager.LoadScene("ClanPage");
                });
            }
        }));
    }
    
    public void GetUserClansProfile(string userId, System.Action<List<ClanJoinResponse>> callback)
    {

        //string url = $"{SUPABASE_URL}ClanMembers?user_id=eq.{CourseManager.Instance.user.user_id}";
 
        string url = $"{SUPABASE_URL}ClanMembers?user_id=eq.{userId}&select=*,Clans(*)";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<ClanJoinResponse> clans = JsonConvert.DeserializeObject<List<ClanJoinResponse>>(json);
            if (clans == null || clans.Count == 0)
            {
                Debug.LogError("No clans found for user.");
                callback?.Invoke(new List<ClanJoinResponse>());
                return;
            }
            else
            {
                callback?.Invoke(clans);
                return;
            }
        }));
    }

    public void CreateClan(string match_type, string name, bool is_public)
    {
        string url = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/Clans?select*";
        int max = match_type == "duo" ? 2 : 4;
        var clanData = new Dictionary<string, object>
        {
            { "clan_mode", match_type },
            { "name", name },
            { "is_public", is_public },
            { "max_players", max}
        };

        string jsonData = JsonConvert.SerializeObject(clanData);

        StartCoroutine(PostData(url, jsonData, resjson =>
        {
            if (string.IsNullOrEmpty(resjson))
            {
                Debug.LogError("clan insert returned empty response.");
                return;
            }

            try
            {
                List<Clan> clans = JsonConvert.DeserializeObject<List<Clan>>(resjson);
                if (clans == null || clans.Count == 0)
                {
                    Debug.LogError("clan insert response did not contain a valid match.");
                    return;
                }

                Clan clan = clans[0];
                CourseManager.Instance.SelectedClan = clan;

                string newurl = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/ClanMembers";

                var clanMemberData = new Dictionary<string, object>
                {
                    { "clan_id", clan.clan_id },
                    { "user_id", CourseManager.Instance.user.user_id },
                    { "role", "leader" }
                };

                string newJsonData = JsonConvert.SerializeObject(clanMemberData);
                StartCoroutine(PostData(newurl, newJsonData, json => { }));

            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing Supabase response: " + e.Message);
            }
        }));
    }

    public void CreateClanPlayer(string clan_id)
    {
        string newurl = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/ClanMembers";

        var clanMemberData = new Dictionary<string, object>
        {
            { "clan_id", clan_id },
            { "user_id", CourseManager.Instance.user.user_id }
        };

        string newJsonData = JsonConvert.SerializeObject(clanMemberData);
        StartCoroutine(PostData(newurl, newJsonData, json => { }));

    }

     public void GetAllClans()
    {

        //string url = $"{SUPABASE_URL}ClanMembers?user_id=eq.{CourseManager.Instance.user.user_id}";

        string url = $"{SUPABASE_URL}Clans?is_public=eq.true&order=created_at.desc&limit=10";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<ClanJoinResponse> clans = JsonConvert.DeserializeObject<List<ClanJoinResponse>>(json);

            Transform scrollView = GameObject.Find("TopViewGroup").transform.Find("View2").Find("CurrentClans").Find("Viewport").Find("Content");
            foreach (Transform child in scrollView)
                Destroy(child.gameObject);

            foreach (var clan in clans)
            {
                float _distance = 0;
                GameObject buttonObj = Instantiate(clanButtonPrefab, scrollView);
                buttonObj.transform.Find("ClanName").GetComponentInChildren<TextMeshProUGUI>().text = clan.Clans.name;

                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    CourseManager.Instance.SelectedClan = clan.Clans;
                    SceneManager.LoadScene("ClanPage");
                });
            }
        }));
    
    }
    public void GetClanHistory()
    {

        //string url = $"{SUPABASE_URL}ClanMembers?user_id=eq.{CourseManager.Instance.user.user_id}";

        string url = $"{SUPABASE_URL}Clans?is_public=eq.true&order=esc&limit=10";
        StartCoroutine(GetRequest(url, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from search.");
                return;
            }

            List<ClanJoinResponse> clans = JsonConvert.DeserializeObject<List<ClanJoinResponse>>(json);

            Transform scrollView = GameObject.Find("TopViewGroup").transform.Find("View2").Find("CurrentClans").Find("Viewport").Find("Content");
            foreach (Transform child in scrollView)
                Destroy(child.gameObject);

            foreach (var clan in clans)
            {
                float _distance = 0;
                GameObject buttonObj = Instantiate(clanButtonPrefab, scrollView);
                buttonObj.transform.Find("ClanName").GetComponentInChildren<TextMeshProUGUI>().text = clan.Clans.name;

                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    CourseManager.Instance.SelectedClan = clan.Clans;
                    SceneManager.LoadScene("ClanPage");
                });
            }
        }));
    }  
    public void GetLeaders()
    {
        string leadersurl = $"{APIHandler.Instance.SUPABASE_URL}Users?order=elo.desc&limit=10";
        StartCoroutine(GetRequest(leadersurl, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from leaders request.");
                return;
            }
            Debug.Log(json);
            List<User> users = JsonConvert.DeserializeObject<List<User>>(json);
            //Transform scrollView = GameObject.FindWithTag("LeadersView").transform;
            Transform scrollView = GameObject.Find("TopViewGroup").transform.Find("TopView3/TabViewLine_FillExpand/ViewGroup/View1/CurrentSoloLeaders/Viewport/Content");
            foreach (Transform child in scrollView)
                Destroy(child.gameObject);

            foreach (var user in users)
            {
                GameObject buttonObj = Instantiate(eloPrefab, scrollView);
                buttonObj.transform.Find("LeaderName").GetComponentInChildren<TextMeshProUGUI>().text = user.username;
                buttonObj.transform.Find("EloScore").GetComponentInChildren<TextMeshProUGUI>().text = user.elo.ToString();
                // buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                // {
                //     CourseManager.Instance.user = user;
                //     SceneManager.LoadScene("Profile");
                // });
            }
            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(scrollView.GetComponent<RectTransform>().sizeDelta.x, scrollView.childCount * 150);
        }));
    }

    public void GetClanLeaders()
    {
        /// DUOS
        string leadersurl = $"{APIHandler.Instance.SUPABASE_URL}Clans?clan_mode=eq.duo&order=elo.desc&limit=10";
        StartCoroutine(GetRequest(leadersurl, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from leaders request.");
                return;
            }
            Debug.Log(json);
            List<Clan> clans = JsonConvert.DeserializeObject<List<Clan>>(json);
            //Transform scrollView = GameObject.FindWithTag("LeadersView").transform;
            Transform scrollView = GameObject.Find("TopViewGroup").transform.Find("TopView3/TabViewLine_FillExpand/ViewGroup/View2/CurrentDuoLeaders/Viewport/Content");
            foreach (Transform child in scrollView)
                Destroy(child.gameObject);

            foreach (var clan in clans)
            {
                GameObject buttonObj = Instantiate(eloPrefab, scrollView);
                buttonObj.transform.Find("LeaderName").GetComponentInChildren<TextMeshProUGUI>().text = clan.name;
                buttonObj.transform.Find("EloScore").GetComponentInChildren<TextMeshProUGUI>().text = clan.elo.ToString();
                // buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                // {
                //     CourseManager.Instance.user = user;
                //     SceneManager.LoadScene("Profile");
                // });
            }
            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(scrollView.GetComponent<RectTransform>().sizeDelta.x, scrollView.childCount * 150);
        }));

         /// DUOS
        leadersurl = $"{APIHandler.Instance.SUPABASE_URL}Clans?clan_mode=eq.squad&order=elo.desc&limit=10";
        StartCoroutine(GetRequest(leadersurl, json =>
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("No JSON returned from leaders request.");
                return;
            }
            Debug.Log(json);
            List<Clan> clans = JsonConvert.DeserializeObject<List<Clan>>(json);
            //Transform scrollView = GameObject.FindWithTag("LeadersView").transform;
            Transform scrollView = GameObject.Find("TopViewGroup").transform.Find("TopView3/TabViewLine_FillExpand/ViewGroup/View3/CurrentSquadLeaders/Viewport/Content");
            foreach (Transform child in scrollView)
                Destroy(child.gameObject);

            foreach (var clan in clans)
            {
                GameObject buttonObj = Instantiate(eloPrefab, scrollView);
                buttonObj.transform.Find("LeaderName").GetComponentInChildren<TextMeshProUGUI>().text = clan.name;
                buttonObj.transform.Find("EloScore").GetComponentInChildren<TextMeshProUGUI>().text = clan.elo.ToString();
                // buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                // {
                //     CourseManager.Instance.user = user;
                //     SceneManager.LoadScene("Profile");
                // });
            }
            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(scrollView.GetComponent<RectTransform>().sizeDelta.x, scrollView.childCount * 150);
        }));

    }


    public void GetUserMatchHistory(string userId, System.Action<List<MatchHistoryItem>> callback)
    {
        string url = $"{SUPABASE_URL}rpc/get_user_match_history";
        Debug.Log(url);
        var body = new Dictionary<string, object> {
        { "p_user_id", userId },
            { "p_include_practice", false } // or true
    	};
        string jsonData = JsonConvert.SerializeObject(body);

        StartCoroutine(PostData(url, jsonData, (responseJson) =>
        {
            if (string.IsNullOrEmpty(responseJson))
            {
                Debug.LogError("No match found.");
                callback(null);
                return;
            }

            try
            {
                List<MatchHistoryItem> matches = JsonConvert.DeserializeObject<List<MatchHistoryItem>>(responseJson);
                if (matches.Count > 0)
                {
                    callback(matches);
                }
                else
                    callback(null);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing match response: " + e.Message);
                callback(null);
            }
        }));

    }


	public void GetAllOpenMatchesForUser(string userId, System.Action<List<OpenMatchItem>> cb)
	{
	    string url =
		$"{SUPABASE_URL}Matches" +
		$"?select=match_id,match_type,format,name,is_public,is_practice,created_at," +
		$"MatchPlayers!inner(user_id,side,clan_id)" +
		$"&MatchPlayers.user_id=eq.{userId}" +
		$"&completed=is.false" +
		$"&order=created_at.desc";

	    StartCoroutine(GetRequest(url, json => {
		var list = new List<OpenMatchItem>();
		try {
		    var raw = Newtonsoft.Json.Linq.JArray.Parse(json ?? "[]");
		    foreach (var row in raw)
		    {
			var mpArr = row["MatchPlayers"] as Newtonsoft.Json.Linq.JArray; // <-- it's an array
			short? side = null;
			string clan = null;
			if (mpArr != null && mpArr.Count > 0)
			{
			    side = mpArr[0].Value<short?>("side");
			    clan = mpArr[0].Value<string>("clan_id");
			}

			list.Add(new OpenMatchItem {
			    match_id    = row.Value<string>("match_id"),
			    match_type  = row.Value<string>("match_type"),
			    format      = row.Value<string>("format"),
			    name        = row.Value<string>("name"),
			    is_public   = row.Value<bool?>("is_public") ?? false,
			    is_practice = row.Value<bool?>("is_practice") ?? false,
			    created_at  = row.Value<string>("created_at"),
			    side        = side,
			    clan_id     = clan
			});
		    }
		} catch (System.Exception e) {
		    Debug.LogError("Open matches parse error: " + e.Message);
		}
		cb?.Invoke(list);
	    }));
	}
    public void GetClanMatchHistory(string clanId, System.Action<List<ClanMatchHistoryItem>> callback)
    {
        string url = $"{SUPABASE_URL}rpc/get_clan_match_history";
	Debug.Log(url);
   	var body = new Dictionary<string, object> {
		{ "p_clan_id", clanId },
        	{ "p_include_practice", false } // or true
    	};
    	string jsonData = JsonConvert.SerializeObject(body);

        StartCoroutine(PostData(url, jsonData, (responseJson) =>
        {
            if (string.IsNullOrEmpty(responseJson))
            {
                Debug.LogError("No match found.");
                callback(null);
                return;
            }

            try
            {
                List<ClanMatchHistoryItem> matches = JsonConvert.DeserializeObject<List<ClanMatchHistoryItem>>(responseJson);
                if (matches.Count > 0){
                    callback(matches);
                }
                else
                    callback(null);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing match response: " + e.Message);
                callback(null);
            }
        }));

    }

    // -----------------  GENERIC SUPABASE REQUESTS -----------------

        IEnumerator GetRequest(string url, System.Action<string> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                callback?.Invoke(json);  // Pass JSON back via callback
            }
            else
            {
                Debug.LogError($"Supabase Error: {www.error}, Response: {www.downloadHandler.text}");
                callback?.Invoke(null);  // Pass null or empty on failure
            }
        }
    }

    
    IEnumerator PostData(string url, string jsonData, System.Action<string> callback)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Prefer", "return=representation");
            www.SetRequestHeader("apikey", SUPABASE_API_KEY);
            www.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Request successful.");
                callback?.Invoke(www.downloadHandler.text);  // "" for VOID RPC success
            }
            else
            {
                Debug.LogError($"Failed to insert: {www.error}, Response: {www.downloadHandler.text}");
                callback?.Invoke(null);  // null on failure
            }
        }
    }

	// POST but allow passing a user bearer token (falls back to anon key if empty)
	IEnumerator PostDataWithBearer(string url, string jsonData, string bearer, System.Action<string> callback)
	{
	    using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
	    {
		byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
		www.uploadHandler = new UploadHandlerRaw(bodyRaw);
		www.downloadHandler = new DownloadHandlerBuffer();

		www.SetRequestHeader("Content-Type", "application/json");
		www.SetRequestHeader("Prefer", "return=representation");
		www.SetRequestHeader("apikey", SUPABASE_API_KEY);
		www.SetRequestHeader("Authorization",
		    string.IsNullOrEmpty(bearer) ? $"Bearer {SUPABASE_API_KEY}" : $"Bearer {bearer}");

		yield return www.SendWebRequest();

		if (www.result == UnityWebRequest.Result.Success || www.responseCode == 201)
		{
		    callback?.Invoke(www.downloadHandler.text);
		}
		else
		{
		    Debug.LogError($"Failed to POST: {www.error}, Response: {www.downloadHandler.text}");
		    callback?.Invoke(null);
		}
	    }
	}
}


