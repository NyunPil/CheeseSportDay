// =============================================================
//  🧀 치즈 운동회 - 드래프트 카드 (개별 카드)
// -------------------------------------------------------------
//  흐름:
//   1) 그리드에서 카드를 누르면 → 보드에 "미리보기" 요청 (스크린에 크게 뜸)
//      카드 자신은 살짝 떠올라 강조됨(아직 확정 아님)
//   2) 확정하면 → 팀 패널 슬롯으로 이동·축소하여 도킹
//      취소하면 → 원래 그리드 자리로 복귀
//   * 미리보기 중 카드를 "다시 누르면" 확정으로도 처리(확정 버튼 대용)
//
//  portrait/playerName 은 스크린·패널 표시에 쓰입니다.
// =============================================================
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DraftCard : UdonSharpBehaviour
{
    [Header("카드 데이터")]
    public string playerName = "";
    [Tooltip("스크린/패널에 띄울 인물 이미지")]
    public Texture portrait;
    public TextMesh nameLabel;        // 선택: 카드에 이름 표시

    [Header("스탯 (정수 점수)")]
    public int gameSkill;   // 게임실력
    public int gameSense;   // 게임센스
    public int teamwork;    // 협동력
    public int luck;        // 운

    [Header("연출 값")]
    public float moveSpeed = 4f;
    public float rotSpeed = 6f;
    public float pendingRaise = 0.4f;   // 미리보기 때 떠오르는 높이
    public float pendingScale = 1.25f;  // 미리보기 때 살짝 커짐
    public float rosterScale = 0.5f;    // 패널 도킹 배율

    DraftBoard _board;
    int _index;
    Vector3 _homePos; Quaternion _homeRot; Vector3 _homeScale;

    // phase: 0 그리드, 1 미리보기(떠오름), 2 패널로 이동, 3 도킹, 4 그리드 복귀
    int _phase = 0;
    Vector3 _targetPos; Quaternion _targetRot; Vector3 _targetScale;

    public void Init(DraftBoard board, int index)
    {
        _board = board;
        _index = index;
        _homePos = transform.position;
        _homeRot = transform.rotation;
        _homeScale = transform.localScale;
        if (nameLabel != null && playerName.Length > 0) nameLabel.text = playerName;
    }

    public override void Interact()
    {
        if (_board == null) return;
        if (_phase == 1) _board.ConfirmPick();   // 미리보기 중 다시 누르면 확정
        else if (_phase == 0) _board.TryPick(_index);
    }

    // --- 보드가 호출하는 상태 전환들 ---

    public void SetPending()   // 미리보기: 살짝 떠오르며 강조 (확정 대기)
    {
        _targetPos = _homePos + Vector3.up * pendingRaise;
        _targetRot = _homeRot;
        _targetScale = _homeScale * pendingScale;
        _phase = 1;
        DisableInteractive = false;   // 다시 눌러 확정 가능
    }

    public void DockToRoster(Transform slot)   // 확정: 패널로 이동·축소
    {
        if (slot != null) { _targetPos = slot.position; _targetRot = slot.rotation; }
        _targetScale = _homeScale * rosterScale;
        _phase = 2;
        DisableInteractive = true;
    }

    public void ReturnToGrid()   // 취소: 원위치로 복귀(애니메이션)
    {
        _targetPos = _homePos;
        _targetRot = _homeRot;
        _targetScale = _homeScale;
        _phase = 4;
        DisableInteractive = false;
    }

    public void SnapToRoster(Transform slot)   // 즉시 도킹(입장/초기화)
    {
        if (slot != null) { transform.position = slot.position; transform.rotation = slot.rotation; }
        transform.localScale = _homeScale * rosterScale;
        _phase = 3;
        DisableInteractive = true;
    }

    public void ResetInstant()   // 즉시 그리드 원위치
    {
        transform.position = _homePos;
        transform.rotation = _homeRot;
        transform.localScale = _homeScale;
        _phase = 0;
        DisableInteractive = false;
    }

    void Update()
    {
        if (_phase == 0 || _phase == 3) return;   // 정지 상태
        float dt = Time.deltaTime;

        transform.position = Vector3.Lerp(transform.position, _targetPos, dt * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, dt * rotSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, dt * moveSpeed);

        if (Vector3.Distance(transform.position, _targetPos) < 0.05f)
        {
            transform.position = _targetPos;
            transform.rotation = _targetRot;
            transform.localScale = _targetScale;
            if (_phase == 2) _phase = 3;        // 도킹 완료
            else if (_phase == 4) _phase = 0;   // 그리드 복귀 완료
            // _phase == 1(미리보기)은 떠오른 상태로 유지
        }
    }
}
