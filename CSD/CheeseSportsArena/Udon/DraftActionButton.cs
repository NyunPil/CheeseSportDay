// =============================================================
//  🧀 치즈 운동회 - 드래프트 액션 버튼 (확정/취소/리셋/뺏어오기)
// -------------------------------------------------------------
//  눌렀을 때 DraftBoard 의 동작을 호출하는 버튼. 콜라이더 필수.
//  action: 0 = 확정, 1 = 취소, 2 = 리셋, 3 = 뺏어오기(잠깐만)
//  · action 3(뺏어오기)일 때만 teamIndex 사용 — 그 좌석 팀장의 팀 번호.
// =============================================================
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DraftActionButton : UdonSharpBehaviour
{
    public DraftBoard board;
    [Tooltip("0=확정, 1=취소, 2=리셋, 3=뺏어오기")]
    public int action = 0;
    [Tooltip("action=3(뺏어오기)일 때 이 좌석 팀 번호")]
    public int teamIndex = 0;

    public override void Interact()
    {
        if (board == null) return;
        if (action == 0) board.ConfirmPick();
        else if (action == 1) board.CancelPick();
        else if (action == 2) board.ResetDraft();
        else if (action == 3) board.StealCall(teamIndex);
    }
}
