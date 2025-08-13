using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;
using RainbowArt.CleanFlatUI;

public class ProfileManager : MonoBehaviour
{
	// Start is called before the first frame update
	public GameObject matchPrefab;
	public GameObject clanButtonPrefab;
	public Transform panel;
	void Start()
	{
		APIHandler.Instance.GetUserMatchHistory(CourseManager.Instance.user.user_id, matches =>
			{
				if (matches != null)
				{
					Transform content = GameObject.Find("ViewGroup/View1/MatchHistory/Viewport/Content").transform;
					foreach (var match in matches)
					{
						GameObject matchGO = Instantiate(matchPrefab, content);
						GradientModifier gradient = matchGO.transform.Find("Background").GetComponent<GradientModifier>();
						SetColorGradient(match.won ? Color.blue : Color.red, gradient);

						matchGO.transform.Find("Elo").GetComponent<TextMeshProUGUI>().text = match.delta_elo.ToString();

						matchGO.transform.Find("Score").GetComponent<TextMeshProUGUI>().text = match.score.ToString();

						matchGO.transform.Find("MatchType").GetComponent<TextMeshProUGUI>().text = match.match_type;
					}

				}
				else
				{
					Debug.Log("No match found.");
				}
			});

		APIHandler.Instance.GetUserClansProfile(CourseManager.Instance.user.user_id, clans =>
		{
			if (clans != null)
			{
				Transform content = GameObject.Find("ViewGroup/View2/MyClans/Viewport/Content").transform;
				foreach (Transform child in content)
					Destroy(child.gameObject);

				foreach (var clan in clans)
				{
					Debug.Log($"Clan: {clan.Clans.name}, ID: {clan.Clans.clan_id}");
					float _distance = 0;
					GameObject buttonObj = Instantiate(clanButtonPrefab, content);
					buttonObj.transform.Find("ClanName").GetComponentInChildren<TextMeshProUGUI>().text = clan.Clans.name;

					buttonObj.GetComponent<Button>().onClick.AddListener(() =>
					{
						CourseManager.Instance.SelectedClan = clan.Clans;
						SceneManager.LoadScene("ClanPage");
					});
				}

			}
			else
			{
				Debug.Log("No clan found.");
			}
		});

		panel.Find("UserName").GetComponent<TextMeshProUGUI>().text = CourseManager.Instance.user.username;

		panel.Find("UserElo").GetComponent<TextMeshProUGUI>().text = "ELO: " + CourseManager.Instance.user.elo.ToString();
	}

	private void SetColorGradient(Color c, GradientModifier grad)
	{
		var g = new Gradient
		{
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
		grad.BlendMode = GradientModifier.Blend.Override;
		grad.GradientStyle = GradientModifier.Style.Horizontal;
		grad.Gradient = g;
	}
	public void SignOut()
	{
		SceneManager.LoadScene("SignIn");
   }
}
