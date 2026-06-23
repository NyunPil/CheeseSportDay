// =============================================================
//  🧀 치즈 운동회 - 스크린 컨트롤러 (메인 화면 표시)
// -------------------------------------------------------------
//  프레임의 "파란 영역"에 해당하는 디스플레이 메쉬(Renderer)의 머티리얼
//  텍스처를 바꿔서 카드 이미지를 크게 띄웁니다.
//
//  ▶ 화면 표시 원리
//   · displayRenderer : 프레임 파란 부분 메쉬(또는 그 구멍 뒤에 겹쳐 둔 쿼드).
//   · ShowCard()      : 그 메쉬 머티리얼의 텍스처를 카드 인물 이미지로 교체.
//   · 발광시키려면 textureProperty 를 "_EmissionMap" 으로 두고
//     머티리얼에서 Emission 을 켜면 화면처럼 빛납니다. (기본은 _MainTex)
//
//  이 스크립트는 동기화하지 않습니다 — DraftBoard 가 동기화된 상태를 보고
//  모든 클라이언트에서 동일하게 호출하므로 화면도 똑같이 보입니다.
// =============================================================
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ScreenController : UdonSharpBehaviour
{
    [Header("표시 대상")]
    [Tooltip("프레임 파란 영역 메쉬 (또는 구멍 뒤 디스플레이 쿼드)")]
    public Renderer displayRenderer;
    [Tooltip("교체할 텍스처 속성. 기본 _MainTex / 발광시키려면 _EmissionMap")]
    public string textureProperty = "_MainTex";

    [Header("대기 화면 (선택)")]
    public Texture idleTexture;        // 아무도 안 골랐을 때 보일 이미지(로고 등)

    [Header("안내 (선택)")]
    public TextMesh statusLabel;       // "○○ — 확정?" 같은 안내문
    public GameObject pendingOverlay;  // 확정 대기 중 켜질 오버레이(테두리/문구 등)

    [Header("뺏어오기 오버레이 (선택)")]
    public GameObject stealOverlay;    // "잠깐만! 뺏어오기!" 빨간 화면
    public TextMesh stealNamesLabel;   // 호출한 팀장 이름 목록

    Material _mat;

    void Start()
    {
        if (displayRenderer != null) _mat = displayRenderer.material;  // 인스턴스 머티리얼
        Clear();
    }

    public void ShowCard(Texture portrait, string playerName, int team, string teamName)
    {
        if (_mat != null && portrait != null) _mat.SetTexture(textureProperty, portrait);
        if (statusLabel != null) statusLabel.text = playerName + " → " + teamName + " ? (확정/취소)";
        if (pendingOverlay != null) pendingOverlay.SetActive(true);
    }

    public void Clear()
    {
        if (_mat != null && idleTexture != null) _mat.SetTexture(textureProperty, idleTexture);
        if (statusLabel != null) statusLabel.text = "";
        if (pendingOverlay != null) pendingOverlay.SetActive(false);
    }

    // 뺏어오기: 빨간 오버레이 + 호출한 팀장 이름 목록
    public void ShowSteal(string names)
    {
        if (stealOverlay != null) stealOverlay.SetActive(true);
        if (stealNamesLabel != null) stealNamesLabel.text = "잠깐만! 뺏어오기!\n" + names;
    }

    public void HideSteal()
    {
        if (stealOverlay != null) stealOverlay.SetActive(false);
        if (stealNamesLabel != null) stealNamesLabel.text = "";
    }
}
