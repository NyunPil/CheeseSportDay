
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class MemberCard : UdonSharpBehaviour
{
    public GameObject memberRoots;
    public Image memberPortraitImages;
    public Text memberNameTexts;

    public void UpdateCrad(string memberName, Sprite portrait)
    {
        memberNameTexts.text = memberName;

        memberPortraitImages.sprite = portrait;
        memberPortraitImages.enabled = portrait != null;
    }
}
