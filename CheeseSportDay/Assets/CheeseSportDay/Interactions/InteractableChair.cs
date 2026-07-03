using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRCStation = VRC.SDK3.Components.VRCStation;

namespace CheeseSportDay.Interactions
{
    [AddComponentMenu("Cheese Sport Day/Interactions/Interactable Chair")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class InteractableChair : UdonSharpBehaviour
    {
        [Tooltip("The VRCStation used by this chair.")]
        public VRCStation station;

        [Tooltip("Allow the seated local player to interact again to leave the chair.")]
        public bool interactAgainToExit = true;

        private bool localPlayerIsSeated;

        private void Start()
        {
            if (station == null)
            {
                station = (VRCStation)GetComponent(typeof(VRCStation));
            }
        }

        public override void Interact()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (station == null || !Utilities.IsValid(localPlayer))
            {
                return;
            }

            if (localPlayerIsSeated && interactAgainToExit)
            {
                station.ExitStation(localPlayer);
                return;
            }

            if (!localPlayerIsSeated)
            {
                station.UseStation(localPlayer);
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                localPlayerIsSeated = true;
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                localPlayerIsSeated = false;
            }
        }
    }
}
