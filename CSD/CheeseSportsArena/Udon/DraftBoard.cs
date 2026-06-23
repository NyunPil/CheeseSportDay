// =============================================================
//  🧀 치즈 운동회 - 드래프트 보드 (중앙 컨트롤러)
// -------------------------------------------------------------
//  흐름: 카드 누름 → 미리보기(스크린에 크게, 확정 대기) → (뺏어오기) → 확정/취소
//   · TryPick(i)     : 카드 i 를 미리보기 (현재 차례 팀이 픽)
//   · StealCall(t)   : 미리보기 중 팀 t 가 "잠깐만! 뺏어오기" → 타겟 팀 = t (이름 누적)
//   · ConfirmPick()  : 미리보기 카드를 현재 타겟 팀으로 확정 → 팀 패널 도킹, 다음 차례
//   · CancelPick()   : 미리보기 취소 → 카드 원위치
//  상태(소유팀/미리보기/타겟/뺏기호출/차례)는 이 보드 하나만 동기화.
//
//  ※ VRChat SDK3(Worlds) + UdonSharp 필요. 빈 확인용 프로젝트엔 넣지 말 것.
// =============================================================
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DraftBoard : UdonSharpBehaviour
{
    [Header("카드 풀 (드래프트 대상 전원)")]
    public DraftCard[] cards;

    [Header("팀별 현황패널")]
    public TeamRoster[] teams;

    [Header("스크린 (선택 카드 크게 + 뺏기 오버레이)")]
    public ScreenController screen;

    [Header("확정/취소 버튼 묶음 (미리보기 중에만 보이게 — 선택)")]
    public GameObject confirmCancelUI;

    [Header("진행 규칙")]
    public bool enforceTurnOrder = true;

    // ---- 동기화 상태 ----
    [UdonSynced] int[] _ownerTeam;       // 카드별 확정 소유팀 (-1 = 미확정)
    [UdonSynced] int _pendingCard = -1;  // 미리보기(확정 대기) 카드 (-1 = 없음)
    [UdonSynced] int _pickTeam = -1;     // 처음 고른 팀
    [UdonSynced] int _targetTeam = -1;   // 현재 향할 팀 (뺏기로 바뀜)
    [UdonSynced] bool[] _stealCalled;    // 팀별 "잠깐만!" 호출 여부 (이름 표시용)
    [UdonSynced] int _currentTeam = 0;   // 현재 차례 팀

    // ---- 로컬 캐시 ----  shown: 0 그리드, 1 미리보기, 2 도킹
    int[] _shown;
    bool _receivedNetwork = false;

    void Start()
    {
        int n = cards.Length;
        _shown = new int[n];
        for (int i = 0; i < n; i++) { cards[i].Init(this, i); _shown[i] = 0; }

        if (Networking.IsOwner(gameObject))
        {
            _ownerTeam = new int[n];
            for (int i = 0; i < n; i++) _ownerTeam[i] = -1;
            ClearSteal();
            RequestSerialization();
        }
        ApplyState(true);
    }

    // ---- 입력(카드/버튼이 호출) ----

    public void TryPick(int index)
    {
        if (index < 0 || index >= cards.Length) return;
        if (_pendingCard >= 0) return;
        EnsureOwnership();
        if (_ownerTeam[index] >= 0) return;
        if (teams == null || teams.Length == 0) return;

        _pendingCard = index;
        _pickTeam = _currentTeam;
        _targetTeam = _currentTeam;
        ClearSteal();
        RequestSerialization();
        ApplyState(false);
    }

    // 미리보기 중 팀 team 이 "잠깐만! 뺏어오기" — 다시 누르면 빠짐
    public void StealCall(int team)
    {
        if (_pendingCard < 0) return;
        if (team < 0 || team >= teams.Length) return;
        EnsureOwnership();
        _stealCalled[team] = !_stealCalled[team];
        if (_stealCalled[team]) _targetTeam = team;     // 마지막 누른 팀이 타겟
        else _targetTeam = RecomputeTarget();           // 빠지면 남은 호출자, 없으면 원픽 팀
        RequestSerialization();
        ApplyState(false);
    }

    public void ConfirmPick()
    {
        if (_pendingCard < 0) return;
        EnsureOwnership();
        _ownerTeam[_pendingCard] = _targetTeam;
        if (enforceTurnOrder) _currentTeam = (_currentTeam + 1) % teams.Length;
        _pendingCard = -1; _pickTeam = -1; _targetTeam = -1;
        ClearSteal();
        RequestSerialization();
        ApplyState(false);
    }

    public void CancelPick()
    {
        if (_pendingCard < 0) return;
        EnsureOwnership();
        _pendingCard = -1; _pickTeam = -1; _targetTeam = -1;
        ClearSteal();
        RequestSerialization();
        ApplyState(false);
    }

    public void ResetDraft()
    {
        EnsureOwnership();
        for (int i = 0; i < _ownerTeam.Length; i++) _ownerTeam[i] = -1;
        _pendingCard = -1; _pickTeam = -1; _targetTeam = -1; _currentTeam = 0;
        ClearSteal();
        RequestSerialization();
        ApplyState(true);
    }

    int RecomputeTarget()
    {
        for (int t = teams.Length - 1; t >= 0; t--)
            if (t < _stealCalled.Length && _stealCalled[t]) return t;
        return _pickTeam;
    }

    void EnsureOwnership()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        if (_ownerTeam == null || _ownerTeam.Length != cards.Length)
        {
            _ownerTeam = new int[cards.Length];
            for (int i = 0; i < cards.Length; i++) _ownerTeam[i] = -1;
        }
        if (_stealCalled == null || _stealCalled.Length != teams.Length) ClearSteal();
    }

    void ClearSteal()
    {
        if (_stealCalled == null || _stealCalled.Length != teams.Length) _stealCalled = new bool[teams.Length];
        for (int t = 0; t < _stealCalled.Length; t++) _stealCalled[t] = false;
    }

    public override void OnDeserialization()
    {
        ApplyState(!_receivedNetwork);
        _receivedNetwork = true;
    }

    // 현재 상태를 화면에 반영. snapAll=true 면 연출 없이 즉시 배치
    void ApplyState(bool snapAll)
    {
        for (int t = 0; t < teams.Length; t++) teams[t].ResetFill();

        for (int i = 0; i < cards.Length; i++)
        {
            int owner = (_ownerTeam != null && i < _ownerTeam.Length) ? _ownerTeam[i] : -1;

            int desired;
            Transform slot = null;
            if (owner >= 0 && owner < teams.Length) { desired = 2; slot = teams[owner].TakeNextSlot(); }
            else if (i == _pendingCard) desired = 1;
            else desired = 0;

            if (snapAll)
            {
                if (desired == 2) cards[i].SnapToRoster(slot);
                else if (desired == 1) cards[i].SetPending();
                else cards[i].ResetInstant();
            }
            else if (_shown[i] != desired)
            {
                if (desired == 2) cards[i].DockToRoster(slot);
                else if (desired == 1) cards[i].SetPending();
                else { if (_shown[i] == 1) cards[i].ReturnToGrid(); else cards[i].ResetInstant(); }
            }
            _shown[i] = desired;
        }

        // 스크린: 미리보기 인물 + 뺏기 오버레이
        if (screen != null)
        {
            if (_pendingCard >= 0)
            {
                screen.ShowCard(cards[_pendingCard].portrait, cards[_pendingCard].playerName, _targetTeam, TeamName(_targetTeam));
                if (AnySteal()) screen.ShowSteal(StealNames()); else screen.HideSteal();
            }
            else { screen.Clear(); screen.HideSteal(); }
        }

        if (confirmCancelUI != null) confirmCancelUI.SetActive(_pendingCard >= 0);

        for (int t = 0; t < teams.Length; t++) teams[t].SetActiveTurn(t == _currentTeam);
    }

    bool AnySteal()
    {
        if (_stealCalled == null) return false;
        for (int t = 0; t < _stealCalled.Length; t++) if (_stealCalled[t]) return true;
        return false;
    }

    string StealNames()
    {
        string s = "";
        if (_stealCalled == null) return s;
        for (int t = 0; t < teams.Length; t++)
            if (t < _stealCalled.Length && _stealCalled[t])
            {
                if (s.Length > 0) s += ", ";
                s += teams[t].teamName;
            }
        return s;
    }

    string TeamName(int t)
    {
        if (t >= 0 && t < teams.Length) return teams[t].teamName;
        return "";
    }

    // ---- 개인 콘솔(LeaderConsole)이 읽는 접근자 ----
    public int CardCount() { return cards != null ? cards.Length : 0; }
    public int OwnerOf(int i)
    {
        if (_ownerTeam != null && i >= 0 && i < _ownerTeam.Length) return _ownerTeam[i];
        return -1;
    }
    public string TeamNameOf(int t) { return TeamName(t); }
}
