// =============================================================
//  🧀 치즈 운동회 - 선택 → 프리팹 저장 (PrefabSaver)
// -------------------------------------------------------------
//  씬에서 선택한 오브젝트를 프리팹 에셋으로 저장(+씬 인스턴스 연결).
//  협업용: 씬 통째로 커밋 대신 프리팹만 올리면 충돌이 적음.
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 선택 → 프리팹으로 저장
// =============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CheeseSports
{
    public static class PrefabSaver
    {
        const string Folder = "Assets/CheeseSportsArena/Prefabs";

        [MenuItem("Tools/🧀 치즈 운동회/선택 → 프리팹으로 저장")]
        static void SaveSelected()
        {
            var sel = Selection.gameObjects;
            if (sel == null || sel.Length == 0)
            {
                Debug.LogWarning("씬에서 프리팹으로 만들 오브젝트를 선택한 뒤 눌러주세요.");
                return;
            }
            EnsureFolder(Folder);

            int n = 0;
            foreach (var go in sel)
            {
                if (go == null) continue;
                string path = AssetDatabase.GenerateUniqueAssetPath($"{Folder}/{go.name}.prefab");
                var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.UserAction);
                if (prefab != null) { Debug.Log($"✅ 프리팹 저장: {path}"); n++; }
                else Debug.LogWarning($"❌ '{go.name}' 프리팹 저장 실패 (이미 프리팹이거나 문제 있음)");
            }
            AssetDatabase.SaveAssets();
            if (n > 0) Debug.Log($"🧩 총 {n}개 프리팹 저장 완료 → {Folder}. 씬 인스턴스는 프리팹에 연결됨.");
        }

        [MenuItem("Tools/🧀 치즈 운동회/선택 → 하나로 묶어 프리팹 저장")]
        static void SaveSelectedGrouped()
        {
            var sel = Selection.gameObjects;
            if (sel == null || sel.Length == 0)
            {
                Debug.LogWarning("씬에서 묶을 오브젝트들을 선택한 뒤 눌러주세요.");
                return;
            }
            EnsureFolder(Folder);

            // 선택된 것 중 최상위만 (다른 선택의 자식은 제외)
            var set = new System.Collections.Generic.HashSet<Transform>();
            foreach (var g in sel) if (g != null) set.Add(g.transform);
            var roots = new System.Collections.Generic.List<Transform>();
            foreach (var t in set)
            {
                bool childOfSel = false;
                for (var p = t.parent; p != null; p = p.parent) if (set.Contains(p)) { childOfSel = true; break; }
                if (!childOfSel) roots.Add(t);
            }
            if (roots.Count == 0) return;

            var group = new GameObject("GalleryArea");
            Undo.RegisterCreatedObjectUndo(group, "Group Prefab");
            var scene = roots[0].gameObject.scene;
            UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(group, scene);
            foreach (var t in roots) Undo.SetTransformParent(t, group.transform, "Group Prefab");

            string path = AssetDatabase.GenerateUniqueAssetPath($"{Folder}/GalleryArea.prefab");
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(group, path, InteractionMode.UserAction);
            AssetDatabase.SaveAssets();
            if (prefab != null) Debug.Log($"🧩 하나로 묶어 프리팹 저장: {path} (자식 {roots.Count}개). 씬엔 이 그룹이 프리팹 인스턴스로 있음.");
            else Debug.LogWarning("프리팹 저장 실패.");
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
