// =============================================================
//  🧀 치즈 운동회 - 창문 슬롯 도구 (WindowSlotTool)
// -------------------------------------------------------------
//  방 모델 머티리얼 이름이 lambert1~35 식이라 "창문"을 이름으로 못 찾음.
//  → 메쉬를 선택하면 그 머티리얼 슬롯을 하나씩 보여주고,
//    [창문으로] 버튼으로 슬롯별로 바꿔가며 창문을 찾게 함. (틀리면 [원복])
//
//  사용:
//   1) Scene에서 창문이 있는 메쉬 클릭(선택)
//   2) 이 창에서 슬롯 0,1,2… [창문으로] 눌러보며 창문이 밝아지는 슬롯 찾기
//   3) 틀린 슬롯은 [원복]으로 되돌리기 (또는 Ctrl+Z)
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 창문 슬롯 도구
// =============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CheeseSports
{
    public class WindowSlotTool : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Tools/🧀 치즈 운동회/창문 슬롯 도구")]
        static void Open()
        {
            var w = GetWindow<WindowSlotTool>("🪟 창문 슬롯");
            w.minSize = new Vector2(320, 300);
        }

        void OnSelectionChange() => Repaint();

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "1) Scene에서 창문 있는 메쉬 클릭\n" +
                "2) 아래 슬롯들을 하나씩 [창문으로] 눌러 창문이 밝아지는 슬롯을 찾으세요\n" +
                "3) 틀린 슬롯은 [원복]", MessageType.Info);

            var go = Selection.activeGameObject;
            if (go == null) { EditorGUILayout.LabelField("▶ Scene에서 메쉬를 선택하세요."); return; }

            var r = go.GetComponent<Renderer>();
            if (r == null) r = go.GetComponentInChildren<Renderer>();
            if (r == null) { EditorGUILayout.LabelField($"'{go.name}' 에 렌더러가 없어요. 하위 메쉬를 직접 선택하세요."); return; }

            EditorGUILayout.LabelField("대상 메쉬:", r.gameObject.name, EditorStyles.boldLabel);
            var mats = r.sharedMaterials;
            EditorGUILayout.LabelField($"머티리얼 슬롯 {mats.Length}개:");

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < mats.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string nm = mats[i] != null ? mats[i].name : "None";
                EditorGUILayout.LabelField($"슬롯 {i}", GUILayout.Width(46));
                EditorGUILayout.LabelField(nm);
                if (GUILayout.Button("창문으로", GUILayout.Width(70))) SetSlot(r, i, true);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        void SetSlot(Renderer r, int index, bool window)
        {
            Undo.RecordObject(r, "Window Slot");
            var arr = r.sharedMaterials;
            if (index >= 0 && index < arr.Length)
            {
                arr[index] = TextureApplier.WindowMat();
                r.sharedMaterials = arr;
                EditorUtility.SetDirty(r);
                Debug.Log($"🪟 '{r.gameObject.name}' 슬롯 {index} → 창문 머티리얼. (아니면 Ctrl+Z)");
            }
            Repaint();
        }
    }
}
#endif
