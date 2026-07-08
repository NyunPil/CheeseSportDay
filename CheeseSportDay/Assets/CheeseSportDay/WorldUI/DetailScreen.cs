
using UdonSharp;
using TMPro;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class DetailScreen : UdonSharpBehaviour
{
    [Header("Detail")]
    public GameObject detailRoot;
    public Image detailPortraitImage;
    public TextMeshProUGUI detailNameText;

    public TextMeshProUGUI detailBodyText;

    public TextMeshProUGUI gameSkillTitleText;
    public TextMeshProUGUI teamworkTitleText;
    public TextMeshProUGUI luckTitleText;

    public TextMeshProUGUI gameSkillValueText;
    public TextMeshProUGUI teamworkValueText;
    public TextMeshProUGUI luckValueText;

    [Header("Team")]
    public TextMeshProUGUI currentTeamText;
    public string currentTeamLabel = "\uD300";
    public string unassignedTeamLabel = "\uBBF8\uBC30\uC815";

    [Header("Labels")]
    public string gameSkillLabel = "Skill";
    public string teamworkLabel = "Teamwork";
    public string luckLabel = "Luck";

    public void RefreshDetail(string name, string body, Sprite profile, int gameSkill, int luck, int teamwork)
    {
        SetText(detailNameText, name);
        SetText(detailBodyText, body);

        if (detailPortraitImage != null)
        {
            detailPortraitImage.sprite = profile;
            detailPortraitImage.enabled = profile != null;
        }

        SetText(gameSkillTitleText, gameSkillLabel);
        SetText(teamworkTitleText, teamworkLabel);
        SetText(luckTitleText, luckLabel);

        SetValueText(gameSkillValueText, gameSkill);
        SetValueText(teamworkValueText, teamwork);
        SetValueText(luckValueText, luck);
    }

    public void RefreshTeam(string teamName)
    {
        if (currentTeamText == null)
        {
            return;
        }

        string value = string.IsNullOrEmpty(teamName) ? unassignedTeamLabel : teamName;
        currentTeamText.text = currentTeamLabel + ": " + value;
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void SetValueText(TextMeshProUGUI target, int value)
    {
        if (target != null)
        {
            target.text = value.ToString();
        }
    }
}
