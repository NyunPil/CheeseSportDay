// =============================================================
//  🧀 치즈 운동회 - 팀 현황패널 (한 팀)
// -------------------------------------------------------------
//  뽑힌 카드가 도킹될 슬롯 목록을 갖고, 보드의 요청에 따라 다음 빈 슬롯을 내어줌.
//  현재 차례면 turnIndicator 를 켜서 표시.
//  슬롯(slots)은 패널 위에 카드가 들어갈 빈 Transform 들을 위→아래 순으로 연결.
// =============================================================
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TeamRoster : UdonSharpBehaviour
{
    [Header("팀 정보")]
    public string teamName = "팀";

    [Header("카드가 도킹될 슬롯들 (위→아래 순서)")]
    public Transform[] slots;

    [Header("표시 (선택)")]
    public TextMesh headerLabel;       // 팀 이름 표시
    public GameObject turnIndicator;   // 현재 차례일 때 켜질 오브젝트

    int _fill;

    void Start()
    {
        if (headerLabel != null) headerLabel.text = teamName;
        if (turnIndicator != null) turnIndicator.SetActive(false);
    }

    // 보드가 상태를 다시 그릴 때마다 채움 카운터 초기화
    public void ResetFill() { _fill = 0; }

    // 다음 빈 슬롯을 반환 (없으면 마지막 슬롯, 슬롯 자체가 없으면 자기 자신)
    public Transform TakeNextSlot()
    {
        if (slots == null || slots.Length == 0) return transform;
        int idx = _fill;
        if (idx >= slots.Length) idx = slots.Length - 1;
        _fill++;
        return slots[idx];
    }

    public void SetActiveTurn(bool active)
    {
        if (turnIndicator != null) turnIndicator.SetActive(active);
    }
}
