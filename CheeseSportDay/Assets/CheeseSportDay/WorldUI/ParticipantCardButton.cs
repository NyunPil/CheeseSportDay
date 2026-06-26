using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CheeseSportDay.WorldUI
{
    [AddComponentMenu("Cheese Sport Day/World UI/Participant Card Button")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ParticipantCardButton : UdonSharpBehaviour
    {
        public ParticipantRosterScreen rosterScreen;
        public int participantIndex = -1;

        public override void Interact()
        {
            Select();
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
