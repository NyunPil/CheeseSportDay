// =============================================================
//  🧀 치즈 운동회 - LOD 컬링 방지 (LodCullFixer)
// -------------------------------------------------------------
//  선택한 오브젝트(자식 포함)의 LOD Group에서 마지막 LOD의
//  컬링 기준을 0으로 → 아무리 멀어져도 안 사라지게 함.
//  (울타리처럼 멀어지면 깜빡/사라지는 프랍 고칠 때. LOD 성능은 유지)
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 🔭 선택한 것 LOD 안사라지게(Culled 0)
// =============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseSports
{
    public static class LodCullFixer
    {
        [MenuItem("Tools/🧀 치즈 운동회/🔭 선택한 것 LOD 안사라지게(Culled 0)")]
        static void Fix()
        {
            var sel = Selection.gameObjects;
            if (sel == null || sel.Length == 0)
            {
                Debug.LogWarning("LOD 있는 오브젝트(울타리 등)를 먼저 선택하세요. 여러 개 다중 선택 가능.");
                return;
            }

            int groups = 0;
            foreach (var go in sel)
            {
                foreach (var lg in go.GetComponentsInChildren<LODGroup>(true))
                {
                    var lods = lg.GetLODs();
                    if (lods == null || lods.Length == 0) continue;
                    Undo.RecordObject(lg, "LOD Culled 0");
                    // 마지막(제일 가벼운) LOD의 전환 높이를 0으로 → 그 밑으로 안 잘림 = 안 사라짐
                    lods[lods.Length - 1].screenRelativeTransitionHeight = 0f;
                    lg.SetLODs(lods);
                    EditorUtility.SetDirty(lg);
                    groups++;
                }
            }

            if (groups == 0)
                Debug.LogWarning("선택한 것 안에 LOD Group이 없어요. (울타리 조각을 선택했는지 확인)");
            else
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log($"🔭 LOD Group {groups}개 처리 완료 → 마지막 LOD를 계속 표시(안 사라짐). Ctrl+S로 씬 저장하세요.");
            }
        }
    }
}
#endif
