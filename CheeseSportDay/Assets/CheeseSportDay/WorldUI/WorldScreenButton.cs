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

        [Tooltip("What happens when a player interacts with this button.")]
        public WorldScreenButtonAction buttonAction = WorldScreenButtonAction.Toggle;

        public override void Interact()
        {
            if (screenController == null)
            {
                return;
            }

            if (buttonAction == WorldScreenButtonAction.Show)
            {
                screenController.ShowActiveView();
            }
            else if (buttonAction == WorldScreenButtonAction.Hide)
            {
                screenController.HideActiveView();
            }
            else
            {
                screenController.ToggleActiveView();
            }
        }
    }
}
