using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ClanManager : MonoBehaviour
{
    public TextMeshProUGUI clanNameText;
    public TextMeshProUGUI clanElo;
    public GameObject matchPrefab;
    public TMP_InputField inputName;
    public List<string> starters = new List<string>();
    public bool practiceMatch = false;
    public string matchFormat = "stroke";
    public bool publicMatch = true;
    public Transform matchScrollView;
    public GameObject memberRowPrefab;
    public GameObject matchPanel;

    private int maxPlayers = 0;

    // NEW: track toggles by user id + selection order
    private readonly Dictionary<string, Toggle> toggleByUserId = new Dictionary<string, Toggle>();
    private readonly List<string> selectionOrder = new List<string>();
    private bool _updatingToggles = false; // guard to prevent recursive loops

    void Start()
    {
        var mode = CourseManager.Instance.SelectedClan.clan_mode;
        if (mode == "duo") maxPlayers = 2;
        else if (mode == "squad") maxPlayers = 4;
        else maxPlayers = 10;

        clanElo.text = CourseManager.Instance.SelectedClan.elo.ToString();
        clanNameText.text = CourseManager.Instance.SelectedClan.name;

        ShowClanMembers(); // <- call to populate members + toggles

        APIHandler.Instance.GetClanMatchHistory(CourseManager.Instance.SelectedClan.clan_id, matches =>
        {
            if (matches == null) { Debug.Log("No match found."); return; }
            Transform content = GameObject.Find("TabView/ViewGroup/View2/MatchHistory/Viewport/Content").transform;
            foreach (var match in matches)
            {
                GameObject matchGO = Instantiate(matchPrefab, content);
                var gradient = matchGO.transform.Find("Background").GetComponent<RainbowArt.CleanFlatUI.GradientModifier>();
                SetColorGradient(match.won ? Color.blue : Color.red, gradient);
                matchGO.transform.Find("Elo").GetComponent<TextMeshProUGUI>().text =
                    match.delta_elo.ToString();
                matchGO.transform.Find("Score").GetComponent<TextMeshProUGUI>().text =
                    (match.clan_avg_strokes ?? 0f).ToString("F1");
                matchGO.transform.Find("MatchType").GetComponent<TextMeshProUGUI>().text = match.match_type;
            }
        });
    }

    private void SetColorGradient(Color c, RainbowArt.CleanFlatUI.GradientModifier grad)
    {
        var g = new Gradient{
            colorKeys = new[]{
                new GradientColorKey(c, 0f),
                new GradientColorKey(c, 0.25f),
                new GradientColorKey(Color.white, 1f)
            },
            alphaKeys = new[]{
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };
        grad.BlendMode = RainbowArt.CleanFlatUI.GradientModifier.Blend.Override;
        grad.GradientStyle = RainbowArt.CleanFlatUI.GradientModifier.Style.Horizontal;
        grad.Gradient = g;
    }

    public void CreateClanMatchUI(string nameOverride = "")
    {
        string type = CourseManager.Instance.SelectedClan.clan_mode;
        string name = string.IsNullOrEmpty(nameOverride) ? inputName.text : nameOverride;

        CourseManager.Instance.roundType = practiceMatch ? "practice" : type;
        CourseManager.Instance.updated = false;

        string clanId = CourseManager.Instance.SelectedClan.clan_id;

        APIHandler.Instance.CreateClanMatch(
            clanId,
            starters,          // capped list
            matchFormat,
            name,
            publicMatch,
            practiceMatch
        );

        SceneManager.LoadScene("ChooseAction");
    }

    void ShowClanMembers()
    {
        APIHandler.Instance.GetClanMembers(CourseManager.Instance.SelectedClan.clan_id, members =>
        {
            Transform content = GameObject.Find("MembersSV/Viewport/Content").transform;

            // clear & reset state
            foreach (Transform child in content) Destroy(child.gameObject);
            toggleByUserId.Clear();
            selectionOrder.Clear();

            foreach (var m in members)
            {
                var row = Instantiate(memberRowPrefab, content.transform);

                var usernameTxt = row.transform.Find("Username")?.GetComponent<TextMeshProUGUI>();
                var roleTxt     = row.transform.Find("Role")?.GetComponent<TextMeshProUGUI>();
                if (usernameTxt) usernameTxt.text = string.IsNullOrEmpty(m.Users?.username) ? m.user_id : m.Users.username;
                if (roleTxt)     roleTxt.text     = string.IsNullOrEmpty(m.role) ? "member" : m.role;

                var t = row.transform.Find("StarterToggle")?.GetComponent<Toggle>();
                if (t == null) continue;

                string uid = m.user_id;
                toggleByUserId[uid] = t;

                // initialize toggle state from starters list if pre-populated
                _updatingToggles = true;
                bool shouldBeOn = starters.Contains(uid);
                t.isOn = shouldBeOn;
                if (shouldBeOn && !selectionOrder.Contains(uid)) selectionOrder.Add(uid);
                _updatingToggles = false;

                // IMPORTANT: add listener via lambda capturing t + uid
                t.onValueChanged.AddListener(isOn => OnMemberToggleChanged(t, uid, isOn));
            }

            UpdateInteractivity();
        });
    }

    // === core logic: add/remove + communicate with toggles ===
    private void OnMemberToggleChanged(Toggle t, string userId, bool isOn)
    {
        if (_updatingToggles) return;

        _updatingToggles = true;

        if (isOn)
        {
            if (!starters.Contains(userId))
            {
                if (starters.Count >= maxPlayers)
                {
                    // Evict the oldest selection, turn its toggle OFF
                    if (selectionOrder.Count > 0)
                    {
                        string toDrop = selectionOrder[0];
                        selectionOrder.RemoveAt(0);
                        starters.Remove(toDrop);

                        if (toggleByUserId.TryGetValue(toDrop, out var dropToggle))
                        {
                            // This will re-enter OnMemberToggleChanged, but we guard with _updatingToggles
                            dropToggle.isOn = false;
                        }
                    }
                }

                starters.Add(userId);
                selectionOrder.Add(userId);
            }
        }
        else
        {
            starters.Remove(userId);
            selectionOrder.Remove(userId);
        }

        _updatingToggles = false;
        UpdateInteractivity();
    }

    // Disable non-selected toggles when at cap; keep selected toggles interactable so users can deselect
    private void UpdateInteractivity()
    {
        bool atCap = starters.Count >= maxPlayers;

        foreach (var kv in toggleByUserId)
        {
            var t = kv.Value;
            if (t == null) continue;

            if (t.isOn)
                t.interactable = true;     // always allow turning off selected ones
            else
                t.interactable = !atCap;   // block turning on new ones if at cap
        }
    }

    public void toggleMatchPractice() => practiceMatch = !practiceMatch;
    public void toggleMatchPublic() => publicMatch = !publicMatch;

    public void Stroke(bool x) => matchFormat = x ? "stroke" : "scramble";

    public void OpenCloseMatch()
    {
        RectTransform rt = matchPanel.gameObject.GetComponent<RectTransform>();
        if (matchPanel.activeInHierarchy)
        {
            matchPanel.SetActive(false);
            matchScrollView.position = new Vector2(matchScrollView.position.x, matchScrollView.position.y + rt.sizeDelta.y);
        }
        else
        {
            matchPanel.SetActive(true);
            matchScrollView.position = new Vector2(matchScrollView.position.x, matchScrollView.position.y - rt.sizeDelta.y);
        }
    }


}

