using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseSportDay.WorldUI
{
    [AddComponentMenu("Cheese Sport Day/World UI/Participant Team Column")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ParticipantTeamColumn : UdonSharpBehaviour
    {
        public Image teamColorImage;
        public Text teamNameText;
        public MemberCard[] members;

        public int GetMemberCapacity()
        {
            return members == null ? 0 : members.Length;
        }

        public void SetTeam(string teamName, Color teamColor)
        {
            if (teamNameText != null)
            {
                teamNameText.text = teamName;
            }

            if (teamColorImage != null)
            {
                teamColorImage.color = teamColor;
            }
        }

        public void ClearMembers()
        {
            int capacity = GetMemberCapacity();
            for (int i = 0; i < capacity; i++)
            {
                if (members[i] != null)
                {
                    members[i].gameObject.SetActive(false);
                }
            }
        }

        public void SetMember(int slotIndex, string memberName, Sprite portrait)
        {
            if (slotIndex < 0 || slotIndex >= GetMemberCapacity())
            {
                return;
            }

            if (members[slotIndex] != null)
            {
                members[slotIndex].gameObject.SetActive(true);
                members[slotIndex].UpdateCrad(memberName, portrait);
            }
        }
    }
}