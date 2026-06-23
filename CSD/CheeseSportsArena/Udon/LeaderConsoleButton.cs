// =============================================================
//  🧀 치즈 운동회 - 콘솔 ◀▶ 버튼
// -------------------------------------------------------------
//  팀장 개인 콘솔의 이전/다음 버튼. 콜라이더 필수.
//  dir: -1 = 이전(◀), +1 = 다음(▶)
// =============================================================
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LeaderConsoleButton : UdonSharpBehaviour
{
    public LeaderConsole console;
    [Tooltip("-1 = 이전(◀), +1 = 다음(▶)")]
    public int dir = 1;

    public override void Interact()
    {
        if (console != null) console.Step(dir);
    }
}
