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
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseSports
{
    // 저장된 액자 배치(월드 위치/회전 + 순서대로 그림 인덱스)
    [System.Serializable] public class FrameLayoutEntry { public Vector3 pos; public Vector3 rot; public string img; }
    [System.Serializable] public class FrameLayoutData { public List<FrameLayoutEntry> frames = new List<FrameLayoutEntry>(); }

    public class FramePlacer : EditorWindow
    {
        GameObject hallPrefab;        // 갤러리 홀 (프리팹/모델) → 원점에 배치
        GameObject frameCollection;   // 액자 콜렉션 (여기서 하나 추출)
        int frameType = 0;            // 0 landscape / 1 large / 2 small
        static readonly string[] TypeKeys = { "landscape", "large", "small" };
        static readonly string[] TypeLabels = { "landscape (≈1.55)", "large (≈1.45)", "small (≈1.23)" };

        // 방향별 액자 수 (바라보는 방향 기준). 0 = 안 검
        int cntEast = 8;    // 동쪽 바라봄 = 왼쪽 벽
        int cntWest = 8;    // 서쪽 바라봄 = 오른쪽 벽
        int cntNorth = 2;   // 북쪽 바라봄 = 뒤 벽
        int cntSouth = 2;   // 남쪽 바라봄 = 앞 벽
        float height = 1.7f;
        float inset = 0.12f;
        float frameScale = 3f;
        float frameYaw = 0f, framePitch = 0f;
        bool assignImages = true;
        string imageFolder = "Assets/Props/PictureFrames/Images";
        bool picRotate90 = false;   // 이미지가 90° 돌아있으면 체크
        bool picFlipX = false;      // 이미지가 좌우 반전이면 체크(기본은 이미 교정됨)
        bool fitFrameToImage = true; // 액자를 그림 종횡비에 맞춰 스케일
        bool mergeFrameAndPic = true; // 액자+그림을 메쉬 하나로 병합(진짜 한 오브젝트)
        bool pinOriginals = true;    // 기존 7개 그림을 서쪽4/동쪽3에 고정 배정
        // 서쪽=오른쪽 벽(W) 1~4, 동쪽=왼쪽 벽(E) 1~3 에 고정 (파일명, 확장자 제외)
        static readonly string[] PinnedWest = { "년필신음", "라이즈궁", "마오리족의습격", "부끄꼴" };
        static readonly string[] PinnedEast = { "삼천궁녀샷", "저글링만설", "호두쌍욕" };
        Dictionary<string, Texture2D> _byName;
        List<Texture2D> _rest;
        int _restIdx;

        const string RootName = "FrameGallery";
        const string MatFolder = "Assets/Props/PictureFrames/_FrameMats";
        const string LayoutPath = "Assets/CheeseSportsArena/FrameLayout.json";
        bool ignoreSavedLayout = false;   // 저장 파일 있으면 기본으로 복원. 켜면 무시하고 새로.
        Dictionary<string, Material> _cache;

        [MenuItem("Tools/🧀 치즈 운동회/액자 걸기")]
        static void Open()
        {
            var w = GetWindow<FramePlacer>("🖼️ 갤러리 생성");
            w.minSize = new Vector2(330, 540);
        }

        // 원클릭용: 창 안 열고 자동 연결 후 기본값으로 액자 생성
        public static void BuildDefault()
        {
            var w = CreateInstance<FramePlacer>();
            try { w.AutoBind(); w.Place(); } finally { DestroyImmediate(w); }
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
            EditorGUILayout.LabelField("방향별 액자 수 (바라보는 방향, 0=안 검)", EditorStyles.boldLabel);
            cntEast = EditorGUILayout.IntSlider("동쪽 바라봄 (왼쪽벽)", cntEast, 0, 20);
            cntWest = EditorGUILayout.IntSlider("서쪽 바라봄 (오른쪽벽)", cntWest, 0, 20);
            cntNorth = EditorGUILayout.IntSlider("북쪽 바라봄 (뒤벽)", cntNorth, 0, 20);
            cntSouth = EditorGUILayout.IntSlider("남쪽 바라봄 (앞벽)", cntSouth, 0, 20);

            EditorGUILayout.Space();
            height = EditorGUILayout.Slider("높이(m)", height, 0.3f, 4.5f);
            inset = EditorGUILayout.Slider("벽에서 거리", inset, 0.01f, 1f);
            frameScale = EditorGUILayout.Slider("액자 크기", frameScale, 0.1f, 10f);
            EditorGUILayout.LabelField("액자 방향 보정(뒤돌면 조절)", EditorStyles.miniBoldLabel);
            frameYaw = EditorGUILayout.Slider("Y 회전", frameYaw, -180f, 180f);
            framePitch = EditorGUILayout.Slider("X 회전", framePitch, -180f, 180f);

            EditorGUILayout.Space();
            assignImages = EditorGUILayout.Toggle("이미지 자동 넣기", assignImages);
            if (assignImages)
            {
                imageFolder = EditorGUILayout.TextField("이미지 폴더", imageFolder);
                EditorGUILayout.LabelField($"  찾은 이미지: {LoadImages().Count}개");
                picRotate90 = EditorGUILayout.Toggle("이미지 90° 회전", picRotate90);
                picFlipX = EditorGUILayout.Toggle("이미지 좌우 반전", picFlipX);
                fitFrameToImage = EditorGUILayout.Toggle("액자를 그림 비율에 맞춤", fitFrameToImage);
                pinOriginals = EditorGUILayout.Toggle("기존 7개 그림 고정(서4/동3)", pinOriginals);
            }
            mergeFrameAndPic = EditorGUILayout.Toggle("액자+그림 하나로 병합", mergeFrameAndPic);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("배치 저장/복원", EditorStyles.boldLabel);
            bool hasLayout = File.Exists(LayoutPath);
            EditorGUILayout.HelpBox(hasLayout
                ? "저장된 배치 있음 → [생성]·원클릭이 이 배치(위치+그림)로 복원됩니다."
                : "저장된 배치 없음 → 기본 벽걸이로 생성됩니다. 배치 후 아래 버튼으로 저장하세요.",
                MessageType.None);
            GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
            if (GUILayout.Button("💾 현재 배치 저장 (위치 + 그림 기억)", GUILayout.Height(24))) SaveLayout();
            GUI.backgroundColor = Color.white;
            using (new EditorGUI.DisabledScope(!hasLayout))
                ignoreSavedLayout = EditorGUILayout.Toggle("저장 무시하고 새로 생성", ignoreSavedLayout);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            if (GUILayout.Button("🖼️ 생성", GUILayout.Height(36))) PlaceButton();
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("삭제", GUILayout.Height(36), GUILayout.Width(80))) Clear();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("특정 액자만 다른 방향으로: 씬에서 액자(Frame_…)를 고른 뒤 아래 버튼. 누를 때마다 90°씩 돌아갑니다(그림도 같이).", MessageType.None);
            GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
            if (GUILayout.Button("🔄 선택한 액자 90° 돌리기", GUILayout.Height(26))) RotateSelected90();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("그림 중복 없이 정리: 원본7 넣을 액자 7개를 선택 → 아래 버튼 → 💾 저장.\n선택한 7개=원본7, 나머지 액자=나머지13 을 전부 유니크로 배정.", MessageType.None);
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("🎯 선택7=원본 · 나머지=나머지13 (전부 유니크)", GUILayout.Height(26))) AssignAllUnique();
            GUI.backgroundColor = Color.white;
        }

        // 선택한 액자 = 원본7, 그 외 모든 액자 = 나머지 그림들 → 전부 겹치지 않게 배정
        void AssignAllUnique()
        {
            var imgs = LoadImages();
            if (imgs.Count == 0) { Debug.LogWarning("이미지 폴더에 그림이 없어요."); return; }
            var byName = BuildNameMap(imgs);

            var origNames = new List<string>();
            origNames.AddRange(PinnedWest); origNames.AddRange(PinnedEast);   // 원본 7 (순서)
            var origSet = new HashSet<string>();
            foreach (var n in origNames) origSet.Add(n.ToLower());

            // 원본 제외한 나머지 그림
            var rest = new List<Texture2D>();
            foreach (var t in imgs) if (!origSet.Contains(t.name.ToLower())) rest.Add(t);

            var selSet = new HashSet<GameObject>(Selection.gameObjects ?? new GameObject[0]);

            // 씬의 모든 액자 (이름 순)
            var frames = new List<GameObject>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (t.name.StartsWith("Frame_")) frames.Add(t.gameObject);
            if (frames.Count == 0) { Debug.LogWarning("씬에 액자(Frame_…)가 없어요."); return; }
            frames.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            int oi = 0, ri = 0, applied = 0, selCount = 0;
            foreach (var f in frames)
            {
                Texture2D tex = null;
                if (selSet.Contains(f) && oi < origNames.Count)
                {
                    byName.TryGetValue(origNames[oi].ToLower(), out tex); oi++; selCount++;
                }
                else if (rest.Count > 0) { tex = rest[ri % rest.Count]; ri++; }
                if (tex != null && SetFramePicture(f, tex)) applied++;
            }
            Dirty();
            Debug.Log($"🎯 유니크 배정 완료: 선택 {selCount}개=원본, 나머지={rest.Count}개에서 채움. 총 {applied}개 적용. 이제 💾 저장하세요." +
                      (selSet.Count == 0 ? "\n(액자 선택 안 함 → 원본 자리 없이 나머지로만 채워짐. 원본7 자리를 원하면 7개 선택 후 다시 눌러요.)" : ""));
        }

        // 액자(병합/그룹 무관)의 그림칸 재질을 지정 텍스처로 교체 (Sprites/Default 슬롯 = 그림)
        bool SetFramePicture(GameObject frame, Texture2D tex)
        {
            var mat = MatFor(tex);
            foreach (var r in frame.GetComponentsInChildren<Renderer>())
            {
                var arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] != null && arr[i].shader != null && arr[i].shader.name == "Sprites/Default")
                    {
                        arr[i] = mat; r.sharedMaterials = arr; return true;
                    }
            }
            return false;
        }

        // 씬에서 선택한 액자(들)의 "바라보는 방향"을 90° 회전 (짝 그림도 함께)
        void RotateSelected90()
        {
            int n = 0;
            foreach (var go in Selection.gameObjects)
            {
                var t = go.transform;
                Undo.RecordObject(t, "Rotate Frame 90");
                t.RotateAround(t.position, Vector3.up, 90f);   // 그룹째 회전 → 몸체+그림 같이 돎
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

        // 씬의 액자(Frame_…) 위치·회전 + 각 액자에 걸린 실제 그림(이름)을 저장.
        void SaveLayout()
        {
            var frames = new List<Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (t.name.StartsWith("Frame_")) frames.Add(t);
            if (frames.Count == 0) { Debug.LogWarning("씬에 액자(Frame_…)가 없어요 — 먼저 [생성]하고 배치하세요."); return; }
            frames.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            // 텍스처 → 파일명 맵 (현재 이미지 폴더 기준)
            var texName = new Dictionary<Texture, string>();
            foreach (var img in LoadImages()) if (img != null && !texName.ContainsKey(img)) texName[img] = img.name;

            var data = new FrameLayoutData();
            int withImg = 0;
            foreach (var f in frames)
            {
                string img = DetectImage(f, texName);
                if (!string.IsNullOrEmpty(img)) withImg++;
                data.frames.Add(new FrameLayoutEntry { pos = f.position, rot = f.eulerAngles, img = img });
            }

            var dir = Path.GetDirectoryName(LayoutPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(LayoutPath, JsonUtility.ToJson(data, true));
            AssetDatabase.Refresh();
            ignoreSavedLayout = false;   // 저장했으니 이제 이 배치로 복원되게
            Debug.Log($"💾 배치 저장: 액자 {frames.Count}개 (그림 인식 {withImg}개). 이제 [생성]·원클릭이 이 배치로 복원돼요. ({LayoutPath})");
        }

        // 액자에 걸린 그림 이름 감지 (재질의 mainTexture가 이미지 폴더 텍스처면 그 이름)
        static string DetectImage(Transform frame, Dictionary<Texture, string> texName)
        {
            foreach (var r in frame.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.mainTexture != null && texName.TryGetValue(m.mainTexture, out var n)) return n;
            return "";
        }

        Dictionary<string, Texture2D> BuildNameMap(List<Texture2D> imgs)
        {
            var d = new Dictionary<string, Texture2D>();
            foreach (var t in imgs) { string k = t.name.ToLower(); if (!d.ContainsKey(k)) d[k] = t; }
            return d;
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

        // [생성] 버튼 전용: 저장 없이 새로 만들어 배치가 날아갈 상황이면 먼저 저장 확인
        void PlaceButton()
        {
            bool willRestore = File.Exists(LayoutPath) && !ignoreSavedLayout;
            var existing = GameObject.Find(RootName);
            bool hasFrames = existing != null && existing.GetComponentsInChildren<Transform>().Length > 1;
            if (hasFrames && !willRestore)   // 복원이 아니라 새로 만드는 경우 = 현재 배치 사라짐
            {
                int opt = EditorUtility.DisplayDialogComplex("액자 재생성",
                    "저장된 배치를 안 쓰고 새로 만듭니다. 지금 배치를 먼저 저장할까요?\n\n" +
                    "· 저장하고 생성: 지금 배치(위치+그림) 저장 후 그대로 복원\n" +
                    "· 그냥 새로: 기본 벽걸이로 새로 (배치 사라짐)",
                    "저장하고 생성", "취소", "그냥 새로");
                if (opt == 1) return;                                 // 취소
                if (opt == 0) { SaveLayout(); ignoreSavedLayout = false; }  // 저장 → 복원되게
                // opt == 2: 그냥 새로 → 진행
            }
            Place();
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

            bool restore = File.Exists(LayoutPath) && !ignoreSavedLayout;
            if (restore)
            {
                // 저장된 배치 복원: 저장 위치·회전 + 저장된 그림(이름으로 조회)
                var byName = BuildNameMap(images);
                var data = JsonUtility.FromJson<FrameLayoutData>(File.ReadAllText(LayoutPath));
                for (int i = 0; data != null && i < data.frames.Count; i++)
                {
                    var e = data.frames[i];
                    Texture2D tex = null;
                    if (!string.IsNullOrEmpty(e.img)) byName.TryGetValue(e.img.ToLower(), out tex);
                    if (tex == null && images.Count > 0) tex = images[i % images.Count];   // 폴백: 순서대로
                    BuildOneFrame(T, unit, e.pos, Quaternion.Euler(e.rot), $"Frame_S{i + 1}", tex);
                    gi++;
                }
            }
            else
            {
                SetupImageAssign(images);   // 기존 7개 고정 + 나머지 채움 준비
                // 자동 벽걸이: 왼쪽 벽 → +X(동) / 오른쪽 → -X(서) / 뒤 → +Z(북) / 앞 → -Z(남)
                if (cntEast > 0)  WallRow(T, unit, new Vector3(B.min.x + inset, height, B.center.z), Vector3.right,   Vector3.forward, B.size.z * 0.8f, cntEast,  "E", images, ref gi);
                if (cntWest > 0)  WallRow(T, unit, new Vector3(B.max.x - inset, height, B.center.z), Vector3.left,    Vector3.forward, B.size.z * 0.8f, cntWest,  "W", images, ref gi);
                if (cntNorth > 0) WallRow(T, unit, new Vector3(B.center.x, height, B.min.z + inset), Vector3.forward, Vector3.right,   B.size.x * 0.8f, cntNorth, "N", images, ref gi);
                if (cntSouth > 0) WallRow(T, unit, new Vector3(B.center.x, height, B.max.z - inset), Vector3.back,    Vector3.right,   B.size.x * 0.8f, cntSouth, "S", images, ref gi);
            }

            DestroyImmediate(unit);   // 템플릿 제거
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
            Dirty();
            Debug.Log($"🖼️ 갤러리 생성: 액자 {gi}개, 이미지 {images.Count}개" + (restore ? " (저장된 배치 복원)" : "") + ".");
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

        void WallRow(Transform parent, GameObject unit, Vector3 wc, Vector3 inward, Vector3 along, float span, int count, string tag, List<Texture2D> images, ref int gi)
        {
            Quaternion face = Quaternion.LookRotation(inward, Vector3.up) * Quaternion.Euler(framePitch, frameYaw, 0f);
            for (int i = 0; i < count; i++)
            {
                float t = (count == 1) ? 0.5f : i / (float)(count - 1);
                Vector3 pos = wc + along * ((t - 0.5f) * span);
                Texture2D tex;
                if (images.Count == 0) tex = null;
                else if (pinOriginals) tex = ResolveTex(tag, i);
                else tex = images[gi % images.Count];
                BuildOneFrame(parent, unit, pos, face, $"Frame_{tag}{i + 1}", tex);
                gi++;
            }
        }

        // 고정 그림 + 나머지 채움 준비: _byName(이름→텍스처), _rest(고정 제외한 나머지)
        void SetupImageAssign(List<Texture2D> all)
        {
            _byName = new Dictionary<string, Texture2D>();
            foreach (var t in all) { string k = t.name.ToLower(); if (!_byName.ContainsKey(k)) _byName[k] = t; }

            var pinned = new HashSet<string>();
            foreach (var n in PinnedWest) pinned.Add(n.ToLower());
            foreach (var n in PinnedEast) pinned.Add(n.ToLower());

            _rest = new List<Texture2D>();
            foreach (var t in all) if (!pinned.Contains(t.name.ToLower())) _rest.Add(t);
            _restIdx = 0;
        }

        // 벽/순번에 맞는 텍스처: 서(W)1~4·동(E)1~3은 고정, 그 외는 나머지에서 순서대로
        Texture2D ResolveTex(string tag, int i)
        {
            if (tag == "W" && i < PinnedWest.Length && _byName.TryGetValue(PinnedWest[i].ToLower(), out var tw)) return tw;
            if (tag == "E" && i < PinnedEast.Length && _byName.TryGetValue(PinnedEast[i].ToLower(), out var te)) return te;
            if (_rest == null || _rest.Count == 0) return null;
            var r = _rest[_restIdx % _rest.Count];
            _restIdx++;
            return r;
        }

        // 한 액자 생성: 그룹(Frame_XX) 안에 Body+Pic → (병합 시) 메쉬 하나로
        void BuildOneFrame(Transform parent, GameObject unit, Vector3 pos, Quaternion rot, string name, Texture2D tex)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.position = pos;
            group.transform.rotation = rot;

            var go = Instantiate(unit);       // 액자 몸체(Body)
            go.name = "Body";
            go.transform.SetParent(group.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * frameScale;

            if (tex != null)
            {
                var pp = FindPicturePlane(go);
                if (pp != null)
                {
                    if (fitFrameToImage) FitFrameToImage(go.transform, pp, tex);
                    PlacePicture(group.transform, pp, tex, MatFor(tex));
                    pp.enabled = false;
                }
            }
            Undo.RegisterCreatedObjectUndo(group, "Frame");
            if (mergeFrameAndPic) CombineFrame(group);
        }

        // 그룹(Body + Pic)을 메쉬 하나로 병합 → 진짜 한 오브젝트. 실패 시 그룹 유지.
        void CombineFrame(GameObject group)
        {
            var combines = new List<CombineInstance>();
            var mats = new List<Material>();
            Matrix4x4 w2l = group.transform.worldToLocalMatrix;
            foreach (var r in group.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled) continue;                       // 숨긴 원본 유리 제외
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh;
                if (!mesh.isReadable)   // 병합하면 빈 메쉬가 됨 → 그룹 상태로 유지
                {
                    Debug.LogWarning($"'{mesh.name}' 메쉬가 Read/Write 꺼짐 → 액자 병합 생략(그룹 유지). 모델 임포트 설정에서 Read/Write Enabled 켜면 병합됩니다.");
                    return;
                }
                var rmats = r.sharedMaterials;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    combines.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = w2l * r.transform.localToWorldMatrix });
                    mats.Add(rmats.Length > 0 ? rmats[Mathf.Min(s, rmats.Length - 1)] : null);
                }
            }
            if (combines.Count == 0) return;

            var combined = new Mesh { name = "FrameMesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            try { combined.CombineMeshes(combines.ToArray(), false, true); }
            catch (System.Exception e)
            {
                Debug.LogWarning("액자 병합 실패(모델 메쉬 Read/Write 꺼짐일 수 있음) — 그룹 상태로 둡니다: " + e.Message);
                return;
            }

            var kids = new List<Transform>();
            foreach (Transform c in group.transform) kids.Add(c);
            foreach (var c in kids) DestroyImmediate(c.gameObject);

            group.AddComponent<MeshFilter>().sharedMesh = combined;
            group.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
        }

        // 유리 칸 자리에 "깨끗한 UV 평면(Quad)"을 새로 깔아 전체 이미지를 안 잘리고 표시.
        // 개구부(유리 바운즈)에 이미지 비율 유지하며 맞추고, 90°회전/좌우반전 토글 반영.
        void PlacePicture(Transform group, Renderer glass, Texture2D tex, Material mat)
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
            q.name = "Pic";
            var qc = q.GetComponent<Collider>(); if (qc != null) DestroyImmediate(qc);
            // 먼저 월드로 배치(부모 없음) → 그룹 하위로(월드 유지). 그룹 스케일1이라 스큐 없음.
            q.transform.rotation = group.rotation * Quaternion.Euler(0f, 0f, zRot);
            q.transform.position = b.center + group.forward * (thick * 0.5f + 0.01f);
            // 기본이 거울상이라 기본으로 뒤집어 교정. picFlipX 켜면 원래대로(반전).
            float sx = picFlipX ? quadW : -quadW;
            q.transform.localScale = new Vector3(sx, quadH, 1f);
            q.transform.SetParent(group, true);
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
