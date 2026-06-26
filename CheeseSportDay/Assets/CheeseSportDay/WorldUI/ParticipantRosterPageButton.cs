using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CheeseSportDay.WorldUI
{
    public enum ParticipantRosterPageAction
    {
        Previous,
        Next
    }

    [AddComponentMenu("Cheese Sport Day/World UI/Participant Roster Page Button")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ParticipantRosterPageButton : UdonSharpBehaviour
    {
        public ParticipantRosterScreen rosterScreen;
        public ParticipantRosterPageAction pageAction = ParticipantRosterPageAction.Next;

        public override void Interact()
        {
            Press();
        }

        public void Press()
        {
            if (rosterScreen == null)
            {
                return;
            }

            if (pageAction == ParticipantRosterPageAction.Previous)
            {
                rosterScreen.PreviousPage();
            }
            else
            {
                rosterScreen.NextPage();
            }
        }
    }
}
