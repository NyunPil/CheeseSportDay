using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace CheeseSportDay.WorldUI
{
    [AddComponentMenu("Cheese Sport Day/World UI/Participant Team Board Screen")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ParticipantTeamBoardScreen : UdonSharpBehaviour
    {
        [Header("Data")]
        public ParticipantRosterScreen rosterScreen;
        public string[] teamNames = { "Red Team", "Blue Team" };

        [Header("View")]
        public Text[] teamNameTexts;
        public Text[] teamMemberTexts;
        public string emptyTeamText = "-";

        [UdonSynced, HideInInspector]
        public int[] participantTeamIndices = new int[0];

        private void Start()
        {
            bool changed = EnsureAssignmentArray();
            if (changed && Networking.IsOwner(gameObject))
            {
                RequestSerialization();
            }

            RefreshAllViews();
        }

        public override void OnDeserialization()
        {
            EnsureAssignmentArray();
            RefreshAllViews();
        }

        public void AssignParticipant(int participantIndex, int teamIndex)
        {
            if (rosterScreen == null
                || participantIndex < 0
                || participantIndex >= rosterScreen.GetParticipantCount())
            {
                return;
            }

            EnsureAssignmentArray();

            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(localPlayer) && !Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            participantTeamIndices[participantIndex] = IsValidTeam(teamIndex) ? teamIndex : -1;
            RequestSerialization();
            RefreshAllViews();
        }

        public int GetParticipantTeam(int participantIndex)
        {
            if (participantTeamIndices == null
                || participantIndex < 0
                || participantIndex >= participantTeamIndices.Length)
            {
                return -1;
            }

            return participantTeamIndices[participantIndex];
        }

        public string GetTeamName(int teamIndex)
        {
            if (!IsValidTeam(teamIndex))
            {
                return "";
            }

            string value = teamNames[teamIndex];
            return string.IsNullOrEmpty(value) ? "Team " + (teamIndex + 1).ToString() : value;
        }

        public void RefreshAllViews()
        {
            RefreshBoard();

            if (rosterScreen != null)
            {
                rosterScreen.RefreshAll();
            }
        }

        private void RefreshBoard()
        {
            int teamCount = GetTeamCount();
            for (int teamIndex = 0; teamIndex < teamCount; teamIndex++)
            {
                if (teamNameTexts != null
                    && teamIndex < teamNameTexts.Length
                    && teamNameTexts[teamIndex] != null)
                {
                    teamNameTexts[teamIndex].text = GetTeamName(teamIndex);
                }

                if (teamMemberTexts != null
                    && teamIndex < teamMemberTexts.Length
                    && teamMemberTexts[teamIndex] != null)
                {
                    teamMemberTexts[teamIndex].text = BuildTeamMemberList(teamIndex);
                }
            }
        }

        private string BuildTeamMemberList(int teamIndex)
        {
            if (rosterScreen == null)
            {
                return emptyTeamText;
            }

            string result = "";
            int participantCount = rosterScreen.GetParticipantCount();
            for (int participantIndex = 0; participantIndex < participantCount; participantIndex++)
            {
                if (GetParticipantTeam(participantIndex) != teamIndex)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(result))
                {
                    result += "\n";
                }

                result += rosterScreen.GetParticipantName(participantIndex);
            }

            return string.IsNullOrEmpty(result) ? emptyTeamText : result;
        }

        private bool EnsureAssignmentArray()
        {
            int participantCount = rosterScreen == null ? 0 : rosterScreen.GetParticipantCount();
            if (participantTeamIndices != null && participantTeamIndices.Length == participantCount)
            {
                return false;
            }

            int[] previous = participantTeamIndices;
            int[] resized = new int[participantCount];
            for (int i = 0; i < resized.Length; i++)
            {
                resized[i] = -1;
                if (previous != null && i < previous.Length)
                {
                    resized[i] = previous[i];
                }
            }

            participantTeamIndices = resized;
            return true;
        }

        private bool IsValidTeam(int teamIndex)
        {
            return teamIndex >= 0 && teamIndex < GetTeamCount();
        }

        private int GetTeamCount()
        {
            return teamNames == null ? 0 : teamNames.Length;
        }
    }
}
