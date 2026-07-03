using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CheeseSportDay.Interactions
{
    [AddComponentMenu("Cheese Sport Day/Interactions/Animator Bool Toggle Button")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AnimatorBoolToggleButton : UdonSharpBehaviour
    {
        [Tooltip("The Animator whose bool parameter should be toggled.")]
        public Animator targetAnimator;

        [Tooltip("The exact name of a bool parameter in the target Animator Controller.")]
        public string boolParameter = "IsActive";

        [Tooltip("Synchronize the bool value so every player sees the same state.")]
        public bool syncForEveryone = true;

        [UdonSynced]
        private bool syncedValue;

        private void Start()
        {
            if (!HasValidTarget())
            {
                return;
            }

            syncedValue = targetAnimator.GetBool(boolParameter);
            ApplyValue(syncedValue);

            if (syncForEveryone && Networking.IsOwner(gameObject))
            {
                RequestSerialization();
            }
        }

        public override void Interact()
        {
            if (!HasValidTarget())
            {
                return;
            }

            if (!syncForEveryone)
            {
                ApplyValue(!targetAnimator.GetBool(boolParameter));
                return;
            }

            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(localPlayer) && !Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            syncedValue = !syncedValue;
            ApplyValue(syncedValue);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            if (syncForEveryone && HasValidTarget())
            {
                ApplyValue(syncedValue);
            }
        }

        private bool HasValidTarget()
        {
            return targetAnimator != null && !string.IsNullOrEmpty(boolParameter);
        }

        private void ApplyValue(bool value)
        {
            targetAnimator.SetBool(boolParameter, value);
        }
    }
}
