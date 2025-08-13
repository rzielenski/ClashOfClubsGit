using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;


using UnityEngine.Networking;
using System.Linq;

public class ChooseActionManager : MonoBehaviour
{
    public Button practiceBtn;
    public GameObject matchPrefab;
    public GameObject matchPanel;
    public GameObject clanPanel;
    public TMP_InputField matchNameInput;
    public TMP_InputField clanNameInput;
    public TMP_InputField searchClan;
    public Transform scrollView;
    public Transform clanScrollView;
    public GameObject openMatchRowPrefab;
    public GameObject clanRowPrefab;

    private string SUPABASE_URL = "https://erqsrecsciorigewaihr.supabase.co/rest/v1/";
    private string SUPABASE_API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVycXNyZWNzY2lvcmlnZXdhaWhyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTQxMTIwNjYsImV4cCI6MjA2OTY4ODA2Nn0.0M6QpU8h-_6zESOlyuXB3lkq7RXlOLXhKEPMCax14zU";

    public string matchFormat;
    public bool practiceMatch = false;
    public bool publicMatch = false;
    public bool publicClan = false;

    public int clanMaxPlayers;

    void Awake()
    {
        RefreshOpenMatches();
        APIHandler.Instance.GetUserClans();
        APIHandler.Instance.GetLeaders();
        APIHandler.Instance.GetClanLeaders();
        APIHandler.Instance.VoidOldMatches();
    }
    public void toggleMatchPractice()
    {
        practiceMatch = !practiceMatch;
    }

    public void toggleMatchPublic()
    {
        publicMatch = !publicMatch;
    }
    public void toggleClanPublic()
    {
        publicClan = !publicClan;
    }
    public void Stroke(bool x)
    {
        if (x)
        {
            matchFormat = "stroke";
        }
        else
        {
            matchFormat = "scramble";
        }
    }

    public void ClanSize(int size)
    {
        clanMaxPlayers = size;
    }

    public void CreateMatch(string nameOverride = "")
    {
        string type = practiceMatch ? "practice" : "solo";
        string name = nameOverride != "" ? nameOverride : matchNameInput.text;
        CourseManager.Instance.roundType = type;
        CourseManager.Instance.updated = false;
        APIHandler.Instance.CreateMatch(type, matchFormat, name, publicMatch, practiceMatch);
        SceneManager.LoadScene("SelectCourse");
    }

    

    public void FindMatch()
    {
        APIHandler.Instance.GetBestMatch(CourseManager.Instance.user.user_id, match =>
        {
            if (match != null)
            {
                APIHandler.Instance.CreateMatchPlayer(match);
                CourseManager.Instance.updated = false;
                SceneManager.LoadScene("SelectCourse");
            }
            else
            {
                Debug.Log("No match found.");
            }
        });
    }

    public void CreateClan(string nameOverride = "")
    {
        string name = "";
        string type = "";
        if (clanMaxPlayers == 2) { type = "duo"; }
        else if (clanMaxPlayers == 4) { type = "squad"; }
        else { type = "squad"; }

        if (nameOverride != "")
        {
            name = nameOverride;
        }
        else
        {
            name = clanNameInput.text;
        }

        APIHandler.Instance.CreateClan(type, name, publicClan);
        SceneManager.LoadScene("ClanPage");
    }

    public void FindClan()
    {

    }

    public void UserClans()
    {
        APIHandler.Instance.GetUserClans();
    }


    public void OpenCloseMatch()
    {
        RectTransform rt = matchPanel.gameObject.GetComponent<RectTransform>();
        if (matchPanel.activeInHierarchy)
        {
            matchPanel.SetActive(false);
            scrollView.position = new Vector2(scrollView.position.x, scrollView.position.y + rt.sizeDelta.y);
        }
        else
        {
            matchPanel.SetActive(true);
            scrollView.position = new Vector2(scrollView.position.x, scrollView.position.y - rt.sizeDelta.y);
        }
    }

    public void OpenCloseClan()
    {
        RectTransform rt = clanPanel.gameObject.GetComponent<RectTransform>();
        if (clanPanel.activeInHierarchy)
        {
            clanPanel.SetActive(false);
            clanScrollView.position = new Vector2(clanScrollView.position.x, clanScrollView.position.y + rt.sizeDelta.y);
        }
        else
        {
            clanPanel.SetActive(true);
            clanScrollView.position = new Vector2(clanScrollView.position.x, clanScrollView.position.y - rt.sizeDelta.y);
        }
    }

    public void GetFinds()
    {
        APIHandler.Instance.GetTopFinds();
    }

    void RefreshOpenMatches()
    {
        Transform openMatchesContent = scrollView.Find("Viewport/Content");
        if (openMatchesContent == null || openMatchRowPrefab == null) return;

        foreach (Transform c in openMatchesContent) Destroy(c.gameObject);

        APIHandler.Instance.GetAllOpenMatchesForUser(CourseManager.Instance.user.user_id, list =>
        {
            foreach (var m in list)
            {
	    	    Debug.Log(m.name);
                var row = Instantiate(openMatchRowPrefab, openMatchesContent);
                row.transform.Find("Title").GetComponent<TextMeshProUGUI>().text =
                    string.IsNullOrEmpty(m.name) ? "Match" : m.name;
                row.transform.Find("Type").GetComponent<TextMeshProUGUI>().text =
                    string.IsNullOrEmpty(m.match_type) ? "unknown" : m.match_type;

                // Optional: show side / clan or created_at
                // row.transform.Find("Meta").GetComponent<TextMeshProUGUI>().text =
                //     $"Side {m.side?.ToString() ?? "?"} • {(m.is_practice ? "practice" : "rated")}";

		    //var playBtn = row.transform.Find("PlayButton").GetComponent<Button>();
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // Hydrate your existing Match object if you need it, or just pass id forward
                    CourseManager.Instance.curMatch = new Match {
                        match_id = m.match_id,
                        match_type = m.match_type,
                        format = m.format,
                        name = m.name,
                        is_public = m.is_public
                    };
                    CourseManager.Instance.updated = false;
                    SceneManager.LoadScene("SelectCourse");
                });
            }
        });
    }
        public void SearchClan()
	{
	
		string term = (searchClan ? searchClan.text : "").Trim();
		string enc  = UnityWebRequest.EscapeURL(term);

		string url  = $"{SUPABASE_URL}Clans" +
		      "?select=clan_id,name,clan_mode,is_public,elo,max_players," +
		      "member_count:ClanMembers(count)" +
		      "&is_public=is.true" +
		      $"&name=ilike.%25{enc}%25" +
		      "&order=elo.desc&limit=50";
		    

	    StartCoroutine(GetRequest(url, json =>
	    {
		if (string.IsNullOrEmpty(json)) { Debug.LogError("No JSON returned from search."); return; }

		List<ClanSearchItem> clans = null;
		try {
		    clans = JsonConvert.DeserializeObject<List<ClanSearchItem>>(json);
		} catch (System.Exception e) {
		    Debug.LogError("Search parse error: " + e.Message);
		    return;
		}

		foreach (Transform child in clanScrollView) Destroy(child.gameObject);

		if (clans == null || clans.Count == 0) return;

		foreach (var c in clans)
		{
			    
			int count = (c.member_count != null && c.member_count.Count > 0) ? c.member_count[0].count : 0;
			int cap   = c.max_players ?? int.MaxValue;
			if (count >= cap) continue;   // hide full clans


		    var row = Instantiate(clanRowPrefab, clanScrollView);

		    row.transform.Find("ClanName").GetComponent<TMPro.TextMeshProUGUI>().text = c.name;

		    var meta = row.transform.Find("Meta")?.GetComponent<TMPro.TextMeshProUGUI>();
		    if (meta) meta.text = $"{(string.IsNullOrEmpty(c.clan_mode) ? "clan" : c.clan_mode)} • {count}/{(c.max_players ?? 0)} • Elo {c.elo}";

		    row.GetComponent<Button>().onClick.AddListener(() =>
		    {
			JoinClanSimple(c.clan_id, ok =>
			{
			    if (!ok) { Debug.LogError("Join failed."); return; }
			    // Refresh list to update counts / hide the now-full clan
			    SearchClan();
			    // (Optional) refresh user's clan list elsewhere
			    // APIHandler.Instance.GetUserClans();
			});
		    });
		}

		// optional: resize content (assumes ~150px per row)
		var rt = clanScrollView.GetComponent<RectTransform>();
		if (rt) rt.sizeDelta = new Vector2(rt.sizeDelta.x, clanScrollView.childCount * 150f);
	    }));
	} 

	// Simple REST join (no RPC). If duplicate, we treat it as success.
	private void JoinClanSimple(string clanId, System.Action<bool> done)
	{
	    string url = $"{SUPABASE_URL}ClanMembers?select=*&on_conflict=clan_id,user_id"; // upsert-friendly
	    var row = new Dictionary<string, object> {
		{ "clan_id", clanId },
		{ "user_id", CourseManager.Instance.user.user_id },
		{ "role", "member" }
	    };
	    string json = JsonConvert.SerializeObject(row);

	    // If your PostData sets "Prefer: return=representation", keep it.
	    // To ignore duplicates cleanly, you can add header "Prefer: resolution=ignore-duplicates" in PostData.
	    StartCoroutine(PostData(url, json, resp => {
		// Consider any non-empty response a success; if your PostData exposes statusCode, you can handle 201/409 explicitly
		done?.Invoke(true);
	    }));
	}


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

            if (www.result == UnityWebRequest.Result.Success || www.responseCode == 201)
            {
                Debug.Log("Round inserted successfully.");
                Debug.Log(www.downloadHandler.text);  // Optional: show inserted data
                string json = www.downloadHandler.text;
                callback?.Invoke(json);
            }
            else
            {
                Debug.LogError($"Failed to insert: {www.error}, Response: {www.downloadHandler.text}");
                callback?.Invoke(null);
            }
        }
    }

}
