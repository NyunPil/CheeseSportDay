// =============================================================
//  🧀 치즈 운동회 - 소품 배치기 (DecorPlacer)  ※ 중앙 블록 버전
// -------------------------------------------------------------
//  vr gallery scene(방)을 경기장으로 쓰고 프롭을 배치.
//   - 방을 목표 크기로 자동 스케일 + 원점 중심 + 바닥(y=0) 안착
//   - 관중석: 방 "가운데"에 4×3 단일 블록 (간격으로 벽 사이에 맞춤)
//   - 팀장석: 앞쪽 가운데 둥근 v (고딕)
//   - 의자 자동 크기 + 바닥 안착, 전부 인스턴스 → 씬에서 자유 이동
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 소품 배치기
// =============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseSports
{
    public class DecorPlacer : EditorWindow
    {
        GameObject galleryRoom, gothicChair, ringChair;
        GameObject[] props = new GameObject[6];

        float targetRoomWidth = 52f;    // 방 목표 가로폭(m) — 넉넉하게

        // 관중석 (가운데 단일 블록) — 기본값 확정
        int audCols = 5, audRows = 4;
        float seatSpacingX = 2.9f, rowSpacingZ = 1.63f;
        float seatSize = 1.45f;         // 관중 의자 크기(m) — 간격과 독립
        float audienceZ = -1f;          // 방 중심 기준 앞뒤 오프셋

        // 팀장석
        int leaderCount = 5;
        float leaderSpacing = 3.76f;
        float leaderSize = 1.56f;       // 팀장석 크기(m) — 간격과 독립
        float leaderZ = 3.5f;
        float leaderRotX = -90f, leaderRotY = 90f, leaderRotZ = 0f;  // 고딕체어 회전(확정값)

        bool autoSizeChairs = true;
        float manualChairScale = 1f;
        float chairRotX = -90f, chairRotY = 0f, chairRotZ = 0f;  // 의자 방향/뒤집기 보정 (이 모델은 X -90이 정위치)
        float propScale = 1f;
        float yOffset = 0f;

        const string RootName = "CheeseDecor";

        [MenuItem("Tools/🧀 치즈 운동회/소품 배치기")]
        static void Open()
        {
            var w = GetWindow<DecorPlacer>("🧀 소품 배치기");
            w.minSize = new Vector2(340, 540);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("🧀 소품 배치기 (중앙 4×3)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("① [에셋 자동 연결] → ② [배치]. 관중석은 방 가운데 단일 블록. 간격으로 벽 사이에 맞추세요.", MessageType.Info);

            GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
            if (GUILayout.Button("🔌 에셋 자동 연결 (Assets/Props)", GUILayout.Height(26))) AutoBind();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            galleryRoom = (GameObject)EditorGUILayout.ObjectField("방(경기장)", galleryRoom, typeof(GameObject), false);
            gothicChair = (GameObject)EditorGUILayout.ObjectField("팀장석(고딕)", gothicChair, typeof(GameObject), false);
            ringChair = (GameObject)EditorGUILayout.ObjectField("관중석(링)", ringChair, typeof(GameObject), false);
            EditorGUILayout.LabelField("인테리어 소품", EditorStyles.boldLabel);
            for (int i = 0; i < props.Length; i++)
                props[i] = (GameObject)EditorGUILayout.ObjectField($"소품 {i + 1}", props[i], typeof(GameObject), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("방", EditorStyles.boldLabel);
            targetRoomWidth = EditorGUILayout.Slider("방 목표 가로폭(m)", targetRoomWidth, 6f, 80f);

            EditorGUILayout.LabelField("관중석 (가운데 단일 블록)", EditorStyles.boldLabel);
            audCols = EditorGUILayout.IntSlider("열(가로)", audCols, 1, 10);
            audRows = EditorGUILayout.IntSlider("행(세로)", audRows, 1, 10);
            seatSpacingX = EditorGUILayout.Slider("가로 간격(m)", seatSpacingX, 0.6f, 6f);
            rowSpacingZ = EditorGUILayout.Slider("행 간격(m)", rowSpacingZ, 0.6f, 6f);
            seatSize = EditorGUILayout.Slider("의자 크기(m)", seatSize, 0.2f, 4f);
            audienceZ = EditorGUILayout.Slider("관중 앞뒤 위치", audienceZ, -25f, 25f);

            EditorGUILayout.LabelField("팀장석", EditorStyles.boldLabel);
            leaderCount = EditorGUILayout.IntSlider("수", leaderCount, 1, 6);
            leaderSpacing = EditorGUILayout.Slider("간격(m)", leaderSpacing, 0.6f, 8f);
            leaderSize = EditorGUILayout.Slider("팀장석 크기(m)", leaderSize, 0.2f, 4f);
            leaderZ = EditorGUILayout.Slider("앞뒤 위치", leaderZ, -25f, 25f);
            leaderRotX = EditorGUILayout.Slider("팀장석 X 회전", leaderRotX, -180f, 180f);
            leaderRotY = EditorGUILayout.Slider("팀장석 Y 회전", leaderRotY, -180f, 180f);
            leaderRotZ = EditorGUILayout.Slider("팀장석 Z 회전", leaderRotZ, -180f, 180f);

            EditorGUILayout.LabelField("공통", EditorStyles.boldLabel);
            autoSizeChairs = EditorGUILayout.Toggle("의자 자동 크기", autoSizeChairs);
            if (!autoSizeChairs) manualChairScale = EditorGUILayout.Slider("의자 수동 스케일", manualChairScale, 0.01f, 20f);
            EditorGUILayout.LabelField("의자 회전 보정 (뒤집힘 고치기)", EditorStyles.miniBoldLabel);
            chairRotX = EditorGUILayout.Slider("X 회전(상하 뒤집기)", chairRotX, -180f, 180f);
            chairRotY = EditorGUILayout.Slider("Y 회전(바라보는 방향)", chairRotY, -180f, 180f);
            chairRotZ = EditorGUILayout.Slider("Z 회전(좌우 기울기)", chairRotZ, -180f, 180f);
            propScale = EditorGUILayout.Slider("소품 스케일", propScale, 0.01f, 20f);
            yOffset = EditorGUILayout.Slider("바닥 높이 보정", yOffset, -3f, 3f);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            if (GUILayout.Button("🧀 배치", GUILayout.Height(38))) Place();
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("삭제", GUILayout.Height(38), GUILayout.Width(80))) Clear();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        void AutoBind()
        {
            galleryRoom = FindModel("Props/GalleryRoom");
            gothicChair = FindModel("Props/GothicChair");
            ringChair = FindModel("Props/RingChair");
            props[0] = FindModel("Props/CheeseWedge");
            props[1] = FindModel("Props/SwingChair");
            props[2] = FindModel("Props/LoungeChair");
            props[3] = FindModel("Props/Alphabet");
            int n = 0;
            if (galleryRoom) n++; if (gothicChair) n++; if (ringChair) n++;
            for (int i = 0; i < props.Length; i++) if (props[i]) n++;
            Debug.Log($"🔌 자동 연결: {n}개. 못 찾은 건 glTFast 설치 후 다시 누르거나 직접 드래그.");
            Repaint();
        }

        GameObject FindModel(string key)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.Contains(key))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null) return go;
                }
            }
            return null;
        }

        void Clear()
        {
            var ex = GameObject.Find(RootName);
            if (ex != null) { Undo.DestroyObjectImmediate(ex); Dirty(); }
        }

        void Place()
        {
            Clear();
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Place Decor");
            var T = root.transform;

            // 방: 목표 폭으로 스케일 + 원점 중심 + 바닥 y=0
            Bounds B;
            if (galleryRoom != null)
            {
                var room = Inst(galleryRoom, T, Vector3.zero, Quaternion.identity, "GalleryRoom");
                Bounds rb = GetBounds(room);
                if (rb.size.x > 0.0001f) room.transform.localScale *= (targetRoomWidth / rb.size.x);
                rb = GetBounds(room);
                room.transform.position += new Vector3(-rb.center.x, -rb.min.y, -rb.center.z);
                B = GetBounds(room);
            }
            else B = new Bounds(Vector3.zero, new Vector3(targetRoomWidth, 4f, targetRoomWidth));

            float cx = B.center.x, cz = B.center.z, fy = B.min.y + yOffset;
            Quaternion rot = Quaternion.Euler(chairRotX, chairRotY, chairRotZ);

            // 팀장석: 앞쪽 가운데 둥근 v
            if (gothicChair != null)
            {
                var lead = new GameObject("LeaderSeats").transform; lead.SetParent(T, false);
                Quaternion lrot = Quaternion.Euler(leaderRotX, leaderRotY, leaderRotZ);
                float totalW = (leaderCount - 1) * leaderSpacing;
                for (int i = 0; i < leaderCount; i++)
                {
                    float off = i - (leaderCount - 1) * 0.5f;
                    float x = cx - totalW * 0.5f + i * leaderSpacing;
                    float z = cz + leaderZ + Mathf.Abs(off) * leaderSpacing * 0.25f;
                    var go = Inst(gothicChair, lead, new Vector3(x, fy, z), lrot, $"Leader_{i + 1}");
                    FitAndDrop(go, leaderSize, fy);
                }
            }

            // 관중석: 방 가운데 단일 블록 (audCols × audRows)
            if (ringChair != null)
            {
                var aud = new GameObject("AudienceSeats").transform; aud.SetParent(T, false);
                float totalW = (audCols - 1) * seatSpacingX;
                int n = 0;
                for (int r = 0; r < audRows; r++)
                    for (int c = 0; c < audCols; c++)
                    {
                        float x = cx - totalW * 0.5f + c * seatSpacingX;
                        float z = cz + audienceZ - r * rowSpacingZ;
                        var go = Inst(ringChair, aud, new Vector3(x, fy, z), rot, $"Seat_{++n}");
                        FitAndDrop(go, seatSize, fy);
                    }
            }

            // 소품: 방 옆에 줄세움
            var deco = new GameObject("Props").transform; deco.SetParent(T, false);
            int p = 0;
            for (int i = 0; i < props.Length; i++)
            {
                if (props[i] == null) continue;
                var go = Inst(props[i], deco, new Vector3(cx - B.size.x * 0.5f - 2f - p * 2.5f, fy, cz), Quaternion.identity, $"Prop_{props[i].name}");
                go.transform.localScale *= propScale;
                p++;
            }

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
            Dirty();
            Debug.Log("🧀 배치 완료 (가운데 블록). 간격/위치는 슬라이더+씬에서 조정.");
        }

        void FitAndDrop(GameObject go, float target, float fy)
        {
            if (go == null) return;
            if (autoSizeChairs)
            {
                Bounds b = GetBounds(go);
                float dim = Mathf.Max(b.size.x, b.size.z);
                if (dim > 0.0001f) go.transform.localScale *= (target / dim);
            }
            else go.transform.localScale *= manualChairScale;

            Bounds b2 = GetBounds(go);
            go.transform.position += new Vector3(0, fy - b2.min.y, 0);
        }

        GameObject Inst(GameObject src, Transform parent, Vector3 pos, Quaternion rot, string name)
        {
            GameObject go = PrefabUtility.InstantiatePrefab(src) as GameObject;
            if (go == null) go = Instantiate(src);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, "Place " + name);
            return go;
        }

        static Bounds GetBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        void Dirty() => EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
