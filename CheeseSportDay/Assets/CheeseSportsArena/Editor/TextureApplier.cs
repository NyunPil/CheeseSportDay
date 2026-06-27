// =============================================================
//  🧀 치즈 운동회 - 텍스처 자동 입히기 (TextureApplier)
// -------------------------------------------------------------
//  배치된 CheeseDecor의 회색 모델에, 각 모델 폴더의 텍스처를 찾아
//  머티리얼을 만들어 입혀줍니다. (노멀/AO/라이트맵/블러 등은 자동 제외)
//  · 결과 머티리얼은 Assets/Props/_AutoMats 에 에셋으로 저장(영구)
//  · 인스턴스 렌더러에만 적용 → 원본 FBX는 안 건드림, Undo 가능
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 텍스처 자동 입히기
//  안 맞는 부분은 그 머티리얼의 Albedo에 텍스처만 바꿔주면 됩니다.
// =============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CheeseSports
{
    public static class TextureApplier
    {
        const string MatFolder = "Assets/Props/_AutoMats";

        // 앨베도로 쓰면 안 되는 텍스처(이름에 포함되면 제외)
        static readonly string[] Bad = {
            "normal", "norm", "_n.", "ao", "occlusion", "rough", "metal", "gloss",
            "spec", "height", "disp", "light", "blur", "mask", "emis", "ground", "internal"
        };
        // 앨베도일 가능성 높은 이름(우선순위 순)
        static readonly string[] Good = {
            "albedo", "basecolor", "base_color", "base", "color", "diffuse", "bake", "estofado", "tx_", "tx"
        };

        [MenuItem("Tools/🧀 치즈 운동회/텍스처 자동 입히기")]
        static void Apply()
        {
            var root = GameObject.Find("CheeseDecor");
            if (root == null) { Debug.LogWarning("CheeseDecor 가 없어요 — 먼저 [소품 배치기]로 배치하세요."); return; }

            EnsureFolder(MatFolder);
            var cache = new Dictionary<string, Material>();
            int applied = 0, skipped = 0;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (IsWindowMesh(r.gameObject))   // ★ 알려진 창문 메쉬(polySurface4) → 창문 머티리얼 자동
                {
                    Material wm = GetWindowMat();
                    Undo.RecordObject(r, "Apply Texture");
                    var wa = r.sharedMaterials;
                    for (int i = 0; i < wa.Length; i++) wa[i] = wm;
                    r.sharedMaterials = wa;
                    applied++;
                    continue;
                }
                if (IsGlass(r.gameObject)) { skipped++; continue; }   // 그 외 유리/거울은 그대로 둠

                string topFolder = TopFolderOf(r.gameObject);
                if (topFolder == null) { skipped++; continue; }

                Texture2D tex = FindAlbedo(topFolder);
                if (tex == null) { skipped++; continue; }

                Material mat = GetMat(tex, cache);
                Undo.RecordObject(r, "Apply Texture");
                var arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                r.sharedMaterials = arr;
                applied++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"🎨 텍스처 적용: {applied}개 렌더러. 텍스처 못 찾아 패스: {skipped}개 (색 있는 건 그대로 둠).");
        }

        // 선택한 오브젝트(렌더러)에 깨끗한 창문 머티리얼을 입힘
        [MenuItem("Tools/🧀 치즈 운동회/선택에 창문 머티리얼 입히기")]
        static void ApplyWindow()
        {
            var sel = Selection.gameObjects;
            if (sel == null || sel.Length == 0)
            {
                Debug.LogWarning("창문 메쉬를 먼저 선택하세요 (Hierarchy에서 GalleryRoom 펼쳐 창문 부분 클릭).");
                return;
            }
            EnsureFolder(MatFolder);
            var mat = GetWindowMat();
            int n = 0;
            foreach (var go in sel)
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    Undo.RecordObject(r, "Window Material");
                    var arr = r.sharedMaterials;
                    for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                    r.sharedMaterials = arr;
                    n++;
                }
            AssetDatabase.SaveAssets();
            Debug.Log($"🪟 창문 머티리얼 적용: {n}개 렌더러. (밝은 흰빛 창)");
        }

        public static Material WindowMat()   // 다른 도구에서 사용
        {
            EnsureFolder(MatFolder);
            return GetWindowMat();
        }

        static Material GetWindowMat()
        {
            string path = MatFolder + "/_Window.mat";
            var ex = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (ex != null) return ex;
            var m = new Material(Shader.Find("Standard"));
            var c = new Color(0.82f, 0.90f, 1f);
            m.color = c;
            m.SetFloat("_Glossiness", 0.85f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.55f);                 // 살짝 발광 → 햇빛 느낌
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // 이 갤러리 모델에서 창문 메쉬 이름(여기에 추가하면 자동으로 창문 머티리얼 적용)
        static readonly string[] WindowMeshNames = { "polysurface4" };
        static bool IsWindowMesh(GameObject go)
        {
            string n = go.name.ToLower();
            foreach (var w in WindowMeshNames) if (n == w) return true;
            return false;
        }

        static readonly string[] GlassWords = { "glass", "window", "mirror", "water", "sky", "reflect", "pane" };

        // 메쉬/부모 이름에 유리/창문류가 있으면 텍스처 입히지 않음
        static bool IsGlass(GameObject go)
        {
            var t = go.transform;
            for (int depth = 0; depth < 4 && t != null; depth++)
            {
                string n = t.name.ToLower();
                foreach (var w in GlassWords) if (n.Contains(w)) return true;
                t = t.parent;
            }
            return false;
        }

        // 렌더러가 속한 임포트 모델의 상위 폴더(예: Assets/Props/GalleryRoom)
        static string TopFolderOf(GameObject go)
        {
            var instRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (instRoot == null) return null;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(instRoot);
            if (src == null) return null;
            string path = AssetDatabase.GetAssetPath(src).Replace('\\', '/');   // .../GalleryRoom/source/x.fbx
            if (string.IsNullOrEmpty(path)) return null;
            string dir = Path.GetDirectoryName(path).Replace('\\', '/');         // .../GalleryRoom/source
            // /source 가 있으면 그 상위, 없으면 그 폴더
            int si = dir.ToLower().LastIndexOf("/source");
            return si > 0 ? dir.Substring(0, si) : dir;
        }

        static Texture2D FindAlbedo(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            Texture2D best = null; int bestScore = -1; long bestSize = -1;
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                string n = Path.GetFileName(p).ToLower();
                bool bad = false;
                foreach (var b in Bad) if (n.Contains(b)) { bad = true; break; }
                if (bad) continue;

                int score = 0;
                for (int i = 0; i < Good.Length; i++) if (n.Contains(Good[i])) { score = Good.Length - i; break; }
                long size = 0; try { size = new FileInfo(p).Length; } catch { }

                if (score > bestScore || (score == bestScore && size > bestSize))
                {
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                    if (t != null) { best = t; bestScore = score; bestSize = size; }
                }
            }
            return best;
        }

        static Material GetMat(Texture2D tex, Dictionary<string, Material> cache)
        {
            string key = AssetDatabase.GetAssetPath(tex);
            if (cache.TryGetValue(key, out var m)) return m;

            string matPath = $"{MatFolder}/{SafeName(tex.name)}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) { cache[key] = existing; return existing; }

            var mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = tex;
            AssetDatabase.CreateAsset(mat, matPath);
            cache[key] = mat;
            return mat;
        }

        static string SafeName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
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
