// =============================================================
//  🧀 치즈 운동회 - 갤러리 자동 생성 (FramePlacer)
// -------------------------------------------------------------
//  [자동 연결] → [생성] 한 번으로:
//   1) 갤러리 홀 자동 배치 (원점)
//   2) 콜렉션에서 선택한 액자(몸체+유리) 자동 추출
//   3) 홀 벽에 액자 줄지어 걸기
//   4) 이미지 폴더 사진을 각 액자 그림칸(유리)에 자동 배정
//  전부 인스턴스 → 씬에서 자유 이동.
//
//  액자 종류: landscape(≈1.55) / large(≈1.45) / small(≈1.23)
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 액자 걸기
// =============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseSports
{
    public class FramePlacer : EditorWindow
    {
        GameObject hallPrefab;        // 갤러리 홀 (프리팹/모델) → 원점에 배치
        GameObject frameCollection;   // 액자 콜렉션 (여기서 하나 추출)
        int frameType = 0;            // 0 landscape / 1 large / 2 small
        static readonly string[] TypeKeys = { "landscape", "large", "small" };
        static readonly string[] TypeLabels = { "landscape (≈1.55)", "large (≈1.45)", "small (≈1.23)" };

        int perWall = 4;
        float height = 1.7f;
        float inset = 0.12f;
        float frameScale = 1f;
        float frameYaw = 0f, framePitch = 0f;
        bool wallL = true, wallR = true, wallB = true, wallF = false;
        bool assignImages = true;
        string imageFolder = "Assets/Props/PictureFrames/Images";
        bool picRotate90 = false;   // 이미지가 90° 돌아있으면 체크
        bool picFlipX = false;      // 이미지가 좌우 반전이면 체크(기본은 이미 교정됨)
        bool fitFrameToImage = true; // 액자를 그림 종횡비에 맞춰 스케일

        const string RootName = "FrameGallery";
        const string MatFolder = "Assets/Props/PictureFrames/_FrameMats";
        Dictionary<string, Material> _cache;

        [MenuItem("Tools/🧀 치즈 운동회/액자 걸기")]
        static void Open()
        {
            var w = GetWindow<FramePlacer>("🖼️ 갤러리 생성");
            w.minSize = new Vector2(330, 540);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("① [에셋 자동 연결] → ② [생성]. 홀 배치 + 액자 걸기 + 사진 넣기까지 자동.", MessageType.Info);
            GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
            if (GUILayout.Button("🔌 에셋 자동 연결", GUILayout.Height(26))) AutoBind();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            hallPrefab = (GameObject)EditorGUILayout.ObjectField("갤러리 홀", hallPrefab, typeof(GameObject), false);
            frameCollection = (GameObject)EditorGUILayout.ObjectField("액자 콜렉션", frameCollection, typeof(GameObject), false);
            frameType = EditorGUILayout.Popup("액자 종류", frameType, TypeLabels);

            EditorGUILayout.Space();
            perWall = EditorGUILayout.IntSlider("벽당 액자 수", perWall, 1, 12);
            height = EditorGUILayout.Slider("높이(m)", height, 0.3f, 4.5f);
            inset = EditorGUILayout.Slider("벽에서 거리", inset, 0.01f, 1f);
            frameScale = EditorGUILayout.Slider("액자 크기", frameScale, 0.1f, 10f);
            EditorGUILayout.LabelField("액자 방향 보정(뒤돌면 조절)", EditorStyles.miniBoldLabel);
            frameYaw = EditorGUILayout.Slider("Y 회전", frameYaw, -180f, 180f);
            framePitch = EditorGUILayout.Slider("X 회전", framePitch, -180f, 180f);

            EditorGUILayout.LabelField("걸 벽", EditorStyles.boldLabel);
            wallL = EditorGUILayout.Toggle("왼쪽", wallL);
            wallR = EditorGUILayout.Toggle("오른쪽", wallR);
            wallB = EditorGUILayout.Toggle("뒤", wallB);
            wallF = EditorGUILayout.Toggle("앞", wallF);

            EditorGUILayout.Space();
            assignImages = EditorGUILayout.Toggle("이미지 자동 넣기", assignImages);
            if (assignImages)
            {
                imageFolder = EditorGUILayout.TextField("이미지 폴더", imageFolder);
                EditorGUILayout.LabelField($"  찾은 이미지: {LoadImages().Count}개");
                picRotate90 = EditorGUILayout.Toggle("이미지 90° 회전", picRotate90);
                picFlipX = EditorGUILayout.Toggle("이미지 좌우 반전", picFlipX);
                fitFrameToImage = EditorGUILayout.Toggle("액자를 그림 비율에 맞춤", fitFrameToImage);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            if (GUILayout.Button("🖼️ 생성", GUILayout.Height(36))) Place();
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("삭제", GUILayout.Height(36), GUILayout.Width(80))) Clear();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("특정 액자만 다른 방향으로: 씬에서 액자(Frame_…)를 고른 뒤 아래 버튼. 누를 때마다 90°씩 돌아갑니다(그림도 같이).", MessageType.None);
            GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
            if (GUILayout.Button("🔄 선택한 액자 90° 돌리기", GUILayout.Height(26))) RotateSelected90();
            GUI.backgroundColor = Color.white;
        }

        // 씬에서 선택한 액자(들)의 "바라보는 방향"을 90° 회전 (짝 그림도 함께)
        void RotateSelected90()
        {
            int n = 0;
            foreach (var go in Selection.gameObjects)
            {
                var t = go.transform;
                Vector3 pivot = t.position;
                Undo.RecordObject(t, "Rotate Frame 90");
                t.RotateAround(pivot, Vector3.up, 90f);

                // 짝 그림(Frame_XX_Pic)도 같은 축으로 회전
                Transform root = t.parent;
                Transform pic = root != null ? root.Find(go.name + "_Pic") : null;
                if (pic != null)
                {
                    Undo.RecordObject(pic, "Rotate Frame 90");
                    pic.RotateAround(pivot, Vector3.up, 90f);
                }
                n++;
            }
            if (n == 0) Debug.LogWarning("씬에서 액자(Frame_…)를 선택한 뒤 눌러주세요.");
            else { Dirty(); Debug.Log($"🔄 선택 액자 {n}개 90° 회전 완료."); }
        }

        void AutoBind()
        {
            hallPrefab = FindModel("Props/GalleryHall");
            frameCollection = FindModel("Props/PictureFrames");
            Debug.Log($"🔌 자동 연결: 홀={(hallPrefab ? "✅" : "❌")} 콜렉션={(frameCollection ? "✅" : "❌")}");
            Repaint();
        }

        static GameObject FindModel(string key)
        {
            foreach (var g in AssetDatabase.FindAssets("t:GameObject"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/');
                if (p.Contains(key)) { var go = AssetDatabase.LoadAssetAtPath<GameObject>(p); if (go) return go; }
            }
            return null;
        }

        void Clear()
        {
            var e = GameObject.Find(RootName);
            if (e != null) { Undo.DestroyObjectImmediate(e); Dirty(); }
        }

        List<Texture2D> LoadImages()
        {
            var list = new List<Texture2D>();
            if (!AssetDatabase.IsValidFolder(imageFolder)) return list;
            var paths = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { imageFolder }))
                paths.Add(AssetDatabase.GUIDToAssetPath(g));
            paths.Sort();
            foreach (var p in paths) { var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (t) list.Add(t); }
            return list;
        }

        void Place()
        {
            Clear();
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Gallery");
            var T = root.transform;

            // 1) 홀: 소품배치기(DecorPlacer)가 배치한 씬의 GalleryHall을 우선 사용(위치·크기 그대로)
            Bounds B = new Bounds(new Vector3(0, 2.5f, 0), new Vector3(19, 5, 28));
            var sceneHall = GameObject.Find("GalleryHall");
            if (sceneHall != null)
            {
                B = GetBounds(sceneHall);   // DecorPlacer가 놓은 홀(오프셋/크기 반영)
            }
            else if (hallPrefab != null)
            {
                var hall = Inst(hallPrefab, T, "GalleryHall");
                Bounds hb = GetBounds(hall);
                hall.transform.position += new Vector3(-hb.center.x, -hb.min.y, -hb.center.z);
                B = GetBounds(hall);
            }

            // 2) 콜렉션에서 액자 유닛 추출
            GameObject unit = frameCollection != null ? BuildFrameUnit(frameCollection, TypeKeys[frameType]) : null;
            if (unit == null) { Debug.LogWarning("액자 콜렉션/종류 확인 — 액자를 못 만들었어요."); Selection.activeGameObject = root; Dirty(); return; }

            // 3) 벽에 걸기 + 4) 이미지
            var images = assignImages ? LoadImages() : new List<Texture2D>();
            int gi = 0;
            if (wallL) WallRow(T, unit, new Vector3(B.min.x + inset, height, B.center.z), Vector3.right, Vector3.forward, B.size.z * 0.8f, "L", images, ref gi);
            if (wallR) WallRow(T, unit, new Vector3(B.max.x - inset, height, B.center.z), Vector3.left, Vector3.forward, B.size.z * 0.8f, "R", images, ref gi);
            if (wallB) WallRow(T, unit, new Vector3(B.center.x, height, B.min.z + inset), Vector3.forward, Vector3.right, B.size.x * 0.8f, "B", images, ref gi);
            if (wallF) WallRow(T, unit, new Vector3(B.center.x, height, B.max.z - inset), Vector3.back, Vector3.right, B.size.x * 0.8f, "F", images, ref gi);

            DestroyImmediate(unit);   // 템플릿 제거
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
            Dirty();
            Debug.Log($"🖼️ 갤러리 생성: 액자 {gi}개, 이미지 {images.Count}개 배정.");
        }

        // 콜렉션에서 선택 종류의 몸체+유리를 뽑아 하나의 액자 유닛으로
        GameObject BuildFrameUnit(GameObject collection, string type)
        {
            GameObject col = Instantiate(collection);
            col.transform.position = Vector3.zero;
            Transform body = null, glass = null;
            foreach (var t in col.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLower();
                if (!n.Contains(type) || n.Contains(".001")) continue;
                if (t.GetComponent<MeshFilter>() == null && t.GetComponentInChildren<MeshFilter>() == null) continue;
                bool isGlass = n.Contains("glass") || n.Contains("g;ass") || n.Contains("g;");
                if (isGlass) { if (glass == null) glass = t; }
                else { if (body == null) body = t; }
            }
            if (body == null && glass == null) { DestroyImmediate(col); return null; }

            var unit = new GameObject("FrameUnit");
            Bounds center = glass ? GetBounds(glass.gameObject) : GetBounds(body.gameObject);
            unit.transform.position = center.center;
            if (body) body.SetParent(unit.transform, true);
            if (glass) glass.SetParent(unit.transform, true);
            DestroyImmediate(col);
            return unit;
        }

        void WallRow(Transform parent, GameObject unit, Vector3 wc, Vector3 inward, Vector3 along, float span, string tag, List<Texture2D> images, ref int gi)
        {
            Quaternion face = Quaternion.LookRotation(inward, Vector3.up) * Quaternion.Euler(framePitch, frameYaw, 0f);
            for (int i = 0; i < perWall; i++)
            {
                float t = (perWall == 1) ? 0.5f : i / (float)(perWall - 1);
                Vector3 pos = wc + along * ((t - 0.5f) * span);
                var go = Instantiate(unit);
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                go.transform.rotation = face;
                go.transform.localScale *= frameScale;
                go.name = $"Frame_{tag}{i + 1}";

                if (images.Count > 0)
                {
                    var pp = FindPicturePlane(go);
                    if (pp != null)
                    {
                        var tex = images[gi % images.Count];
                        if (fitFrameToImage) FitFrameToImage(go.transform, pp, tex);  // 액자를 그림 비율로
                        PlacePicture(parent, go.transform, pp, tex, MatFor(tex));
                        pp.enabled = false;   // 원본 유리(아틀라스 UV라 잘림/회전) 숨김
                    }
                }
                Undo.RegisterCreatedObjectUndo(go, "Frame");
                gi++;
            }
        }

        // 유리 칸 자리에 "깨끗한 UV 평면(Quad)"을 새로 깔아 전체 이미지를 안 잘리고 표시.
        // 개구부(유리 바운즈)에 이미지 비율 유지하며 맞추고, 90°회전/좌우반전 토글 반영.
        void PlacePicture(Transform root, Transform frame, Renderer glass, Texture2D tex, Material mat)
        {
            Bounds b = glass.bounds;                    // 월드 AABB
            Vector3 s = b.size;
            float thick = Mathf.Min(s.x, Mathf.Min(s.y, s.z));
            float openH = s.y;                          // 세로(위아래)
            float openW = Mathf.Max(s.x, s.z);          // 가로(벽 방향)
            if (openW < 1e-4f || openH < 1e-4f) return;

            float aspect = (float)tex.width / Mathf.Max(1, tex.height);
            if (picRotate90) aspect = 1f / aspect;      // 90° 돌면 표시 비율도 뒤집힘

            // 개구부 안에 비율 유지하며 최대로 채우기
            float w, h;
            if (openW / openH > aspect) { h = openH; w = h * aspect; }
            else { w = openW; h = w / aspect; }

            // 90° 회전이면 쿼드를 법선축(Z) 기준 90° 돌리고, 회전 전 크기는 가로/세로 스왑
            float quadW = picRotate90 ? h : w;
            float quadH = picRotate90 ? w : h;
            float zRot = picRotate90 ? 90f : 0f;

            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = frame.name + "_Pic";
            var qc = q.GetComponent<Collider>(); if (qc != null) DestroyImmediate(qc);
            q.transform.SetParent(root, true);          // 루트(스케일1)에 붙여 월드 크기 그대로
            q.transform.rotation = frame.rotation * Quaternion.Euler(0f, 0f, zRot);
            q.transform.position = b.center + frame.forward * (thick * 0.5f + 0.01f);
            // 기본이 거울상이라 기본으로 뒤집어 교정. picFlipX 켜면 원래대로(반전).
            float sx = picFlipX ? quadW : -quadW;
            q.transform.localScale = new Vector3(sx, quadH, 1f);
            q.GetComponent<Renderer>().sharedMaterial = mat;
            Undo.RegisterCreatedObjectUndo(q, "Picture");
        }

        // 액자(프레임) 자체를 이미지 종횡비에 맞춰 가로 스케일 조정 → 그림이 액자에 꽉 참(레터박스 X)
        void FitFrameToImage(Transform frame, Renderer glass, Texture2D tex)
        {
            Bounds gb = glass.bounds;
            float curH = gb.size.y;
            float curW = Mathf.Max(gb.size.x, gb.size.z);   // 벽 방향(가로) = 프레임 로컬 X
            if (curW < 1e-4f || curH < 1e-4f) return;

            float cur = curW / curH;
            float tgt = (float)tex.width / Mathf.Max(1, tex.height);
            if (picRotate90) tgt = 1f / tgt;

            float fix = tgt / cur;
            var ls = frame.localScale;
            frame.localScale = new Vector3(ls.x * fix, ls.y, ls.z);   // 로컬 X만 조정
        }

        static Renderer FindPicturePlane(GameObject frameGo)
        {
            Renderer best = null; float bestFlat = float.MaxValue;
            foreach (var mf in frameGo.GetComponentsInChildren<MeshFilter>())
            {
                var r = mf.GetComponent<Renderer>();
                if (r == null || mf.sharedMesh == null) continue;
                var s = mf.sharedMesh.bounds.size;
                float mx = Mathf.Max(s.x, s.y, s.z);
                if (mx <= 0f) continue;
                float flat = Mathf.Min(s.x, s.y, s.z) / mx;
                if (flat < bestFlat) { bestFlat = flat; best = r; }
            }
            return best;
        }

        Material MatFor(Texture2D tex)
        {
            if (_cache == null) _cache = new Dictionary<string, Material>();
            if (_cache.TryGetValue(tex.name, out var cached)) return cached;
            EnsureFolder(MatFolder);
            string path = $"{MatFolder}/{SafeName(tex.name)}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Sprites/Default");   // 양면 렌더 + 조명 무관
                if (sh == null) sh = Shader.Find("Unlit/Texture");
                if (sh == null) sh = Shader.Find("Standard");
                m = new Material(sh) { mainTexture = tex };
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                // 이전 방식(Unlit + 타일링/오프셋 변경)으로 만든 기존 mat 정상화
                var sh = Shader.Find("Sprites/Default");
                if (sh != null && m.shader != sh) m.shader = sh;
                m.mainTexture = tex;
                m.mainTextureScale = Vector2.one;
                m.mainTextureOffset = Vector2.zero;
                EditorUtility.SetDirty(m);
            }
            _cache[tex.name] = m;
            return m;
        }

        GameObject Inst(GameObject src, Transform parent, string name)
        {
            GameObject go = PrefabUtility.InstantiatePrefab(src) as GameObject;
            if (go == null) go = Instantiate(src);
            go.transform.SetParent(parent, false);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, "Place " + name);
            return go;
        }

        static string SafeName(string s)
        {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var p = folder.Split('/'); string cur = p[0];
            for (int i = 1; i < p.Length; i++) { string nx = cur + "/" + p[i]; if (!AssetDatabase.IsValidFolder(nx)) AssetDatabase.CreateFolder(cur, p[i]); cur = nx; }
        }

        static Bounds GetBounds(GameObject go)
        {
            var r = go.GetComponentsInChildren<Renderer>();
            if (r.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = r[0].bounds;
            for (int i = 1; i < r.Length; i++) b.Encapsulate(r[i].bounds);
            return b;
        }

        void Dirty() => EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
