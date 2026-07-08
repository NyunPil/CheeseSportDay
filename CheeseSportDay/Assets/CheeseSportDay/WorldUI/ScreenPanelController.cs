using UdonSharp;
using TMPro;
using UnityEngine;
using VRC.SDKBase;

namespace CheeseSportDay.WorldUI
{
    [AddComponentMenu("Cheese Sport Day/World UI/Screen Panel Controller")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ScreenPanelController : UdonSharpBehaviour
    {
        [Tooltip("Shown while the main screen UI is hidden.")]
        public GameObject idleView;

        [Tooltip("Shown after the world button is pressed.")]
        public GameObject activeView;

        [Tooltip("Initial active view state when the world starts.")]
        public bool showActiveViewOnStart;

        [Tooltip("When enabled, one player's button press updates the screen for everyone.")]
        public bool syncForEveryone = true;

        [Header("ī����")]
        public TextMeshProUGUI countText;
        public string nowCounterCaptain;

        [UdonSynced]
        private bool activeViewVisible;

        private void Start()
        {
            activeViewVisible = showActiveViewOnStart;
            ApplyVisibility();

            if (syncForEveryone && Networking.IsOwner(gameObject))
            {
                RequestSerialization();
            }
        }

        public override void OnDeserialization()
        {
            if (syncForEveryone)
            {
                ApplyVisibility();
            }
        }

        public void ToggleActiveView()
        {
            SetActiveViewVisible(!activeViewVisible);
        }

        public void SetActiveViewVisible(bool visible)
        {
            if (syncForEveryone)
            {
                VRCPlayerApi localPlayer = Networking.LocalPlayer;
                if (Utilities.IsValid(localPlayer) && !Networking.IsOwner(gameObject))
                {
                    Networking.SetOwner(localPlayer, gameObject);
                }
            }

            activeViewVisible = visible;
            ApplyVisibility();

            if (syncForEveryone)
            {
                RequestSerialization();
            }
        }

        private void ApplyVisibility()
        {
            if (activeView != null)
            {
                activeView.SetActive(activeViewVisible);
            }

            if (!activeViewVisible)
            {
                nowCounterCaptain = null;
            }
        }
    }
}
