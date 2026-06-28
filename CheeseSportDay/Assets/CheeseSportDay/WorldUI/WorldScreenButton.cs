using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CheeseSportDay.WorldUI
{
    public enum WorldScreenButtonAction
    {
        Toggle,
        Show,
        Hide
    }

    [AddComponentMenu("Cheese Sport Day/World UI/World Screen Button")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class WorldScreenButton : UdonSharpBehaviour
    {
        [Tooltip("The screen controller this interact button should drive.")]
        public ScreenPanelController screenController;

        [Header("Captain Name")]
        public string captainName;

        public override void Interact()
        {
            if (screenController == null)
            {
                return;
            }

            if(!string.IsNullOrEmpty(screenController.nowCounterCaptain) && screenController.nowCounterCaptain != captainName)
            {
                return;
            }

            screenController.nowCounterCaptain = captainName;

            screenController.ToggleActiveView();
            screenController.countText.text = $"»¯ ¾î ¿À ±â!\n{captainName}";
        }
    }
}
