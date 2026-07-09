
using CheeseSportDay.WorldUI;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DetailScreenBackButton : UdonSharpBehaviour
{
    public ParticipantRosterScreen rosterScreen;
    public DetailScreen detailScreen;

    public override void Interact()
    {
        if (detailScreen == null)
        {
            return;
        }

        foreach (var item in rosterScreen.participantCards)
        {
            item.GetComponent<Collider>().enabled = true;
        }

        detailScreen.gameObject.SetActive(false);
    }
}