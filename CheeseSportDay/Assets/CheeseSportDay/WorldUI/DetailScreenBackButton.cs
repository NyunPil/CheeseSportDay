
using CheeseSportDay.WorldUI;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DetailScreenBackButton : UdonSharpBehaviour
{
    public DetailScreen detailScreen;

    public override void Interact()
    {
        if (detailScreen == null)
        {
            return;
        }

        detailScreen.gameObject.SetActive(false);
    }
}