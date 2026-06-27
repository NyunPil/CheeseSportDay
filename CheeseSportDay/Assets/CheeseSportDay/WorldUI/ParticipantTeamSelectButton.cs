using UdonSharp;
using UnityEngine;

namespace CheeseSportDay.WorldUI
{
    [AddComponentMenu("Cheese Sport Day/World UI/Participant Team Select Button")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ParticipantTeamSelectButton : UdonSharpBehaviour
    {
        public ParticipantRosterScreen rosterScreen;
        public int teamIndex = -1;

        public override void Interact()
        {
            AssignSelectedParticipant();
        }

        public void AssignSelectedParticipant()
        {
            if (rosterScreen != null)
            {
                rosterScreen.AssignSelectedParticipantToTeam(teamIndex);
            }
        }
    }
}
