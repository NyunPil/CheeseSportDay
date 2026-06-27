using UdonSharp;
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
        public Text nameText;

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

        public void SetContent(string displayName, Sprite portrait, bool isSelected, Color normalColor, Color selectedColor)
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
                backgroundImage.color = isSelected ? selectedColor : normalColor;
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