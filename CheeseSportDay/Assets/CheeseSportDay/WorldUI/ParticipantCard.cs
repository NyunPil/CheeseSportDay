using UdonSharp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseSportDay.WorldUI
{
    [AddComponentMenu("Cheese Sport Day/World UI/Participant Card")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ParticipantCard : UdonSharpBehaviour
    {
        [Header("View")]
        public Image backgroundImage;
        public Image portraitImage;
        public TextMeshProUGUI nameText;
        public GameObject selectSuccessObj;

        [HideInInspector]
        public ParticipantRosterScreen rosterScreen;

        [HideInInspector]
        public int participantIndex = -1;

        public override void Interact()
        {
            Select();
        }

        public void Bind(ParticipantRosterScreen screen, int index)
        {
            rosterScreen = screen;
            participantIndex = index;
        }

        public void SetContent(string displayName, Sprite portrait, Color cardColor, bool isAssigned)
        {
            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = portrait != null;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = cardColor;
            }

            if (selectSuccessObj != null)
            {
                selectSuccessObj.SetActive(isAssigned);
            }
        }

        public void Select()
        {
            if (rosterScreen != null && participantIndex >= 0)
            {
                rosterScreen.SelectParticipant(participantIndex);
            }
        }
    }
}