// =============================================================
//  🧀 치즈 운동회 - 단어 만들기 (WordBuilder)
// -------------------------------------------------------------
//  씬에 있는 알파벳 글자 오브젝트(이름이 C, H, E … 식)를 복제해서
//  입력한 단어를 한 줄로 세워줍니다. (기본 "CHEESE")
//   - 만든 단어는 빈 오브젝트 하나로 묶여서 통째로 이동/회전 가능
//   - 벽에 붙이려면 만든 뒤 그 묶음을 회전/이동해서 벽에 대면 됨
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 단어 만들기 (CHEESE)
// =============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CheeseSports
{
    public class WordBuilder : EditorWindow
    {
        string word = "CHEESE";
        float scale = 1f;
        float gap = 0.1f;
        bool flip = true;   // 글자 순서 뒤집기(보는 방향에 맞춤)

        [MenuItem("Tools/🧀 치즈 운동회/단어 만들기 (CHEESE)")]
        static void Open()
        {
            var w = GetWindow<WordBuilder>("🔤 단어 만들기");
            w.minSize = new Vector2(300, 170);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("씬의 알파벳 글자(C,H,E…)를 복제해 단어를 한 줄로 세웁니다.\n만든 뒤 묶음을 회전/이동해서 벽에 붙이세요.", MessageType.Info);
            word = EditorGUILayout.TextField("단어", word);
            scale = EditorGUILayout.Slider("크기", scale, 0.1f, 10f);
            gap = EditorGUILayout.Slider("글자 간격", gap, 0f, 2f);
            flip = EditorGUILayout.Toggle("글자 순서 뒤집기", flip);

            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            if (GUILayout.Button("🔤 만들기", GUILayout.Height(34))) Build();
            GUI.backgroundColor = Color.white;
        }

        void Build()
        {
            if (string.IsNullOrEmpty(word)) return;
            string w = word.ToUpper();

            var sign = new GameObject("Word_" + w);   // 원점·identity·scale1 상태로 레이아웃
            Undo.RegisterCreatedObjectUndo(sign, "Build Word");

            char[] chars = w.ToCharArray();
            if (flip) System.Array.Reverse(chars);   // 보는 방향에 맞춰 순서 뒤집기

            float cursor = 0f;
            int made = 0;
            string missing = "";
            foreach (char ch in chars)
            {
                if (ch == ' ') { cursor += 0.6f + gap; continue; }
                var src = FindLetter(ch);
                if (src == null) { missing += ch; cursor += 0.6f + gap; continue; }

                var clone = Instantiate(src.gameObject);
                clone.name = ch.ToString();
                clone.transform.SetParent(sign.transform, true);
                Bounds b = GetBounds(clone);
                clone.transform.position += new Vector3(cursor - b.min.x, -b.min.y, -b.center.z);
                b = GetBounds(clone);
                cursor += b.size.x + gap;
                made++;
                Undo.RegisterCreatedObjectUndo(clone, "Build Word");
            }

            // 가로 중앙 정렬 (아직 sign이 원점·scale1 이므로 local==world)
            foreach (Transform c in sign.transform) c.localPosition -= new Vector3(cursor * 0.5f, 0, 0);
            sign.transform.localScale = Vector3.one * scale;

            // ★ 지금 보고 있는 씬뷰 한가운데로 이동 (안 보이는 문제 해결)
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) sign.transform.position = sv.pivot;

            Selection.activeGameObject = sign;
            if (sv != null) sv.FrameSelected();

            EditorUtility.SetDirty(sign);
            if (made == 0)
                Debug.LogWarning($"🔤 '{w}': 글자를 하나도 못 찾았어요! 알파벳 글자 오브젝트 이름이 'C','H','E'… 가 아닐 수 있어요. 글자 하나 클릭해서 정확한 이름을 알려주세요. (못 찾은 글자: {missing})");
            else
                Debug.Log($"🔤 '{w}' 생성: {made}글자" + (missing.Length > 0 ? $" / 못 찾은 글자: {missing}" : " (전부 찾음)") + $". 화면 중앙에 생성됨, 선택됨.");
        }

        static Transform FindLetter(char ch)
        {
            string target = ch.ToString().ToUpper();
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
            {
                // 이름이 "A:C" 처럼 접두사가 붙을 수 있어 ':' 뒤 부분으로 비교
                string n = t.name;
                int colon = n.LastIndexOf(':');
                string seg = (colon >= 0) ? n.Substring(colon + 1) : n;
                if (seg.ToUpper() == target && t.GetComponentInChildren<Renderer>() != null)
                    return t;
            }
            return null;
        }

        static Bounds GetBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }
    }
}
#endif
