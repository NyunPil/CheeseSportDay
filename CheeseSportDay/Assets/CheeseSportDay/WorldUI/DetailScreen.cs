
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine;
using UnityEngine.UI;

public class DetailScreen : UdonSharpBehaviour
{
    [Header("Detail")]
    public GameObject detailRoot;
    public Image detailPortraitImage;
    public Text detailNameText;
    public Text detailTitleText;
    public Text detailBodyText;
    public Text gameSkillText;
    public Text gameSenseText;
    public Text teamworkText;
    public Text physicalText;
    public Text luckText;

    [Header("Labels")]
    public string gameSkillLabel = "Skill";
    public string gameSenseLabel = "Sense";
    public string teamworkLabel = "Teamwork";
    public string physicalLabel = "Physical";
    public string luckLabel = "Luck";

    public void RefreshDetail(string name, string title, string body, Sprite sprite, int value1, int value2, int value3, int value4, int value5)
    {
        if (detailNameText != null)
        {
            detailNameText.text = name;
        }

        if (detailTitleText != null)
        {
            detailTitleText.text = title;
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = body;
        }

        if (detailPortraitImage != null)
        {
            Sprite portrait = sprite;
            detailPortraitImage.sprite = portrait;
            detailPortraitImage.enabled = portrait != null;
        }

        SetStatText(gameSkillText, gameSkillLabel, value1);
        SetStatText(gameSenseText, gameSenseLabel, value2);
        SetStatText(teamworkText, teamworkLabel, value3);
        SetStatText(physicalText, physicalLabel, value4);
        SetStatText(luckText, luckLabel, value5);
    }

    private void SetStatText(Text target, string label, int value)
    {
        if (target != null)
        {
            target.text = $"{label}\n{value.ToString()}";
        }
    }
}
