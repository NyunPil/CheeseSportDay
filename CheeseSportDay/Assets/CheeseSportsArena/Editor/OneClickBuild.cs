// =============================================================
//  🧀 치즈 운동회 - 원클릭 전체 생성 (OneClickBuild)
// -------------------------------------------------------------
//  버튼 하나로 순서대로 전부 생성:
//    ① 아레나  ② 갤러리홀 + 소품  ③ 텍스처 + 창문  ④ 액자  ⑤ 포탈 버튼 + 텔레포트 연결
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ ⭐ 원클릭 전체 생성
// =============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseSports
{
    public static class OneClickBuild
    {
        [MenuItem("Tools/🧀 치즈 운동회/⭐ 원클릭 전체 생성", false, -100)]
        static void Run()
        {
            bool ok = EditorUtility.DisplayDialog(
                "⭐ 원클릭 전체 생성",
                "아레나 → 갤러리홀/소품 → 텍스처/창문 → 액자 → 포탈+텔레포트 순서로 새로 생성합니다.\n\n" +
                "각 단계는 기존 생성물을 지우고 다시 만듭니다.\n" +
                "(개별로 돌려둔 액자 방향 등은 초기화돼요.)\n\n진행할까요?",
                "생성", "취소");
            if (!ok) return;

            try
            {
                EditorUtility.DisplayProgressBar("원클릭 전체 생성", "① 아레나(드래프트룸) 생성…", 0.10f);
                CheeseArenaBuilder.BuildDefault();

                EditorUtility.DisplayProgressBar("원클릭 전체 생성", "② 갤러리홀 + 소품 배치…", 0.35f);
                DecorPlacer.BuildDefault();

                EditorUtility.DisplayProgressBar("원클릭 전체 생성", "③ 텍스처 + 창문 입히기…", 0.60f);
                TextureApplier.RunDefault();

                EditorUtility.DisplayProgressBar("원클릭 전체 생성", "④ 액자 생성…", 0.80f);
                FramePlacer.BuildDefault();

                EditorUtility.DisplayProgressBar("원클릭 전체 생성", "⑤ 포탈 버튼 + 텔레포트 연결…", 0.93f);
                PortalBuilder.BuildAndWireDefault();

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("⭐ 원클릭 전체 생성 완료! (아레나 → 갤러리홀/소품 → 텍스처/창문 → 액자 → 포탈+텔레포트)");
            }
            catch (System.Exception e)
            {
                Debug.LogError("⭐ 원클릭 생성 중 오류 — 중단됨:\n" + e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
#endif
