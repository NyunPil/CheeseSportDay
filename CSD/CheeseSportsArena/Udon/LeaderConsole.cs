// =============================================================
//  🧀 치즈 운동회 - 팀장 개인 콘솔 (좌석마다 작은 스크린)
// -------------------------------------------------------------
//  ◀▶ 버튼으로 후보(에이전트)를 넘겨보며 인물 + 이름 + 4스탯을 조회.
//  이미 확정된 후보면 X(선택완료) + 어느 팀인지 표시. 메인 스크린엔 영향 없음.
//  좌석별로 동기화 — 주변 사람도 같은 화면을 봄.
//
//  데이터는 DraftBoard.cards 를 그대로 읽습니다(별도 입력 불필요).
// =============================================================
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LeaderConsole : UdonSharpBehaviour
{
    [Header("데이터 소스")]
    public DraftBoard board;

    [Header("표시")]
    [Tooltip("작은 스크린 면(인물 이미지가 뜰 곳)")]
    public Renderer displayRenderer;
    public string textureProperty = "_MainTex";
    public TextMesh nameLabel;     // 이름
    public TextMesh statsLabel;    // 4스탯
    public TextMesh takenLabel;    // "선택완료 · ○○팀"
    public GameObject xMark;       // 확정된 후보일 때 켜질 X 표

    [UdonSynced] int _viewIndex = 0;
    Material _mat;

    void Start()
    {
        if (displayRenderer != null) _mat = displayRenderer.material;
        Refresh();
    }

    // ◀▶ 버튼이 호출
    public void Step(int dir)
    {
        int n = (board != null) ? board.CardCount() : 0;
        if (n <= 0) return;
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _viewIndex = (_viewIndex + dir + n) % n;
        RequestSerialization();
        Refresh();
    }

    public void Prev() { Step(-1); }
    public void Next() { Step(1); }

    public override void OnDeserialization() { Refresh(); }

    void Refresh()
    {
        if (board == null) return;
        int n = board.CardCount();
        if (n <= 0) return;
        if (_viewIndex < 0) _viewIndex = 0;
        if (_viewIndex >= n) _viewIndex = n - 1;

        DraftCard c = board.cards[_viewIndex];
        if (_mat != null && c.portrait != null) _mat.SetTexture(textureProperty, c.portrait);
        if (nameLabel != null) nameLabel.text = c.playerName;
        if (statsLabel != null)
            statsLabel.text =
                "게임실력  " + c.gameSkill + "\n" +
                "게임센스  " + c.gameSense + "\n" +
                "협동력    " + c.teamwork + "\n" +
                "운        " + c.luck;

        int owner = board.OwnerOf(_viewIndex);
        bool taken = owner >= 0;
        if (xMark != null) xMark.SetActive(taken);
        if (takenLabel != null) takenLabel.text = taken ? ("선택완료 · " + board.TeamNameOf(owner)) : "";
    }
}
