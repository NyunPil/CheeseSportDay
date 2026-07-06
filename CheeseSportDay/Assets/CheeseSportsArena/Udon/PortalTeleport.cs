// =============================================================
//  🧀 치즈 운동회 - 포탈 텔레포트 (PortalTeleport)
// -------------------------------------------------------------
//  빛나는 원형 포탈을 클릭(Use)하면 지정한 도착 지점으로 순간이동.
//  같은 월드 안 이동(드래프트장 ↔ 갤러리)용.
//
//  세팅: 포탈 오브젝트에 이 컴포넌트를 붙이고
//        destination 칸에 "상대편 포탈 앞 도착 마커"를 넣으세요.
//        (포탈 배치기가 마커까지 자동 생성해 둠)
// =============================================================
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CheeseSports
{
    [AddComponentMenu("Cheese Sport Day/Portal/Portal Teleport")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PortalTeleport : UdonSharpBehaviour
    {
        [Tooltip("도착 지점. 클릭하면 여기로 순간이동합니다. (상대편 포탈 앞 도착 마커)")]
        public Transform destination;

        public override void Interact()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (destination == null || !Utilities.IsValid(localPlayer))
            {
                return;
            }

            localPlayer.TeleportTo(destination.position, destination.rotation);
        }
    }
}
