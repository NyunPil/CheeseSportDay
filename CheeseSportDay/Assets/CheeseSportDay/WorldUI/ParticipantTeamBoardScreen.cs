using UdonSharp;
using UnityEngine;
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
        public string[] teamCaptainNames = { "Red Captain", "Blue Captain" };
        public Sprite[] teamCaptainPortraits = new Sprite[2];
        public Color[] teamColors = { new Color(0.82f, 0.18f, 0.18f, 1f), new Color(0.15f, 0.4f, 0.85f, 1f) };

        [Header("View")]
        public ParticipantTeamColumn[] teamColumns;

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

        public int GetTeamCount()
        {
            int teamCount = teamNames == null ? 0 : teamNames.Length;
            if (teamCaptainNames != null && teamCaptainNames.Length > teamCount)
            {
                teamCount = teamCaptainNames.Length;
            }

            return teamCount;
        }

        public string GetTeamName(int teamIndex)
        {
            if (!IsValidTeam(teamIndex))
            {
                return "";
            }

            if (teamNames != null
                && teamIndex < teamNames.Length
                && !string.IsNullOrEmpty(teamNames[teamIndex]))
            {
                return teamNames[teamIndex];
            }

            return GetTeamButtonLabel(teamIndex) + " Team";
        }

        public string GetTeamButtonLabel(int teamIndex)
        {
            if (!IsValidTeam(teamIndex))
            {
                return "";
            }

            if (teamCaptainNames != null
                && teamIndex < teamCaptainNames.Length
                && !string.IsNullOrEmpty(teamCaptainNames[teamIndex]))
            {
                return teamCaptainNames[teamIndex];
            }

            return GetTeamNameFallback(teamIndex);
        }

        public Color GetTeamColor(int teamIndex)
        {
            if (teamColors != null && teamIndex >= 0 && teamIndex < teamColors.Length)
            {
                return teamColors[teamIndex];
            }

            return Color.gray;
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
            if (teamColumns == null)
            {
                return;
            }

            int teamCount = GetTeamCount();
            for (int teamIndex = 0; teamIndex < teamColumns.Length; teamIndex++)
            {
                ParticipantTeamColumn column = teamColumns[teamIndex];
                if (column == null)
                {
                    continue;
                }

                bool hasTeam = teamIndex < teamCount;
                column.gameObject.SetActive(hasTeam);
                if (!hasTeam)
                {
                    continue;
                }

                column.SetTeam(GetTeamName(teamIndex), GetTeamColor(teamIndex));
                column.ClearMembers();

                int memberSlot = 0;
                string captainName = GetCaptainName(teamIndex);
                if (!string.IsNullOrEmpty(captainName))
                {
                    column.SetMember(memberSlot, captainName, GetCaptainPortrait(teamIndex));
                    memberSlot++;
                }

                if (rosterScreen == null)
                {
                    continue;
                }

                int participantCount = rosterScreen.GetParticipantCount();
                for (int participantIndex = 0; participantIndex < participantCount; participantIndex++)
                {
                    if (GetParticipantTeam(participantIndex) != teamIndex)
                    {
                        continue;
                    }

                    column.SetMember(
                        memberSlot,
                        rosterScreen.GetParticipantName(participantIndex),
                        rosterScreen.GetParticipantPortrait(participantIndex));
                    memberSlot++;

                    if (memberSlot >= column.GetMemberCapacity())
                    {
                        break;
                    }
                }
            }
        }

        private string GetCaptainName(int teamIndex)
        {
            if (teamCaptainNames == null || teamIndex < 0 || teamIndex >= teamCaptainNames.Length)
            {
                return "";
            }

            return teamCaptainNames[teamIndex];
        }

        private Sprite GetCaptainPortrait(int teamIndex)
        {
            if (teamCaptainPortraits == null || teamIndex < 0 || teamIndex >= teamCaptainPortraits.Length)
            {
                return null;
            }

            return teamCaptainPortraits[teamIndex];
        }

        private string GetTeamNameFallback(int teamIndex)
        {
            if (teamNames != null
                && teamIndex >= 0
                && teamIndex < teamNames.Length
                && !string.IsNullOrEmpty(teamNames[teamIndex]))
            {
                return teamNames[teamIndex];
            }

            return "Team " + (teamIndex + 1).ToString();
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
    }
}