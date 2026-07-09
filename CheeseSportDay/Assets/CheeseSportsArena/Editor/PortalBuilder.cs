// =============================================================
//  🧀 치즈 운동회 - 텔레포트 버튼 배치기 (PortalBuilder)
// -------------------------------------------------------------
//  벽/바닥 버튼 패널로 3곳을 오감: 팀원룸 ↔ 드래프트룸 ↔ 갤러리
//  (드래프트룸이 허브 → 버튼 4개, 착지 마커 3개)
//
//  [배치] 후 각 버튼에 PortalTeleport(UdonSharp)만 붙여 destination 연결:
//    · Btn_Team_to_Draft      → Arrival_Draft
//    · Btn_Gallery_to_Draft   → Arrival_Draft
//    · Btn_Draft_to_Team      → Arrival_Team
//    · Btn_Draft_to_Gallery   → Arrival_Gallery
//
//  버튼 색(연결선별 2색): 팀원↔드래프트=노랑 / 드래프트↔갤러리=파랑
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 포탈 배치기
// =============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseSports
{
    public class PortalBuilder : EditorWindow
    {
        // 각 공간 노드 위치 (X, Z)
        Vector2 draftPos = new Vector2(0f, -20f);    // 드래프트룸(원형 GalleryRoom) = 허브
        Vector2 teamPos = new Vector2(0f, -57f);     // 팀원룸(아레나)
        Vector2 galleryPos = new Vector2(40f, 0f);   // 갤러리
        float buttonY = 1.2f;        // 버튼 높이(중심)
        float arrivalAhead = 1.6f;   // 착지 지점: 노드 앞쪽 거리

        const string RootName = "TeleportSystem";
        const string MatFolder = "Assets/CheeseSportsArena/Materials";

        [MenuItem("Tools/🧀 치즈 운동회/포탈 배치기")]
        static void Open()
        {
            var w = GetWindow<PortalBuilder>("🔘 텔레포트 버튼");
            w.minSize = new Vector2(340, 420);
        }

        // 원클릭용: 창 안 열고 배치 + 텔레포트 연결까지
        public static void BuildAndWireDefault()
        {
            var w = CreateInstance<PortalBuilder>();
            try { w.Build(); w.WireTeleports(); }
            finally { DestroyImmediate(w); }
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "3곳 체인: 팀원룸 ↔ 드래프트룸 ↔ 갤러리 (드래프트룸 허브)\n" +
                "[배치] 후 각 버튼에 PortalTeleport 붙이고 destination 연결:\n" +
                "  Btn_Team_to_Draft → Arrival_Draft\n" +
                "  Btn_Gallery_to_Draft → Arrival_Draft\n" +
                "  Btn_Draft_to_Team → Arrival_Team\n" +
                "  Btn_Draft_to_Gallery → Arrival_Gallery", MessageType.Info);

            GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
            if (GUILayout.Button("🔎 씬에서 드래프트룸·갤러리 위치 자동 채우기", GUILayout.Height(24))) AutoFill();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("공간 위치 (X, Z)", EditorStyles.boldLabel);
            draftPos = EditorGUILayout.Vector2Field("드래프트룸(허브)", draftPos);
            teamPos = EditorGUILayout.Vector2Field("팀원룸(박스 → 씬에서 이동)", teamPos);
            galleryPos = EditorGUILayout.Vector2Field("갤러리", galleryPos);

            EditorGUILayout.Space();
            buttonY = EditorGUILayout.Slider("버튼 높이", buttonY, 0.3f, 2.5f);
            arrivalAhead = EditorGUILayout.Slider("착지 거리(앞)", arrivalAhead, 0.5f, 5f);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            if (GUILayout.Button("🔘 배치", GUILayout.Height(36))) Build();
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("삭제", GUILayout.Height(36), GUILayout.Width(80))) Clear();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("버튼/노드가 벽을 등지면: 씬에서 선택 → 아래 버튼. 누를 때마다 90°씩 (마커 포함해 통째로 돌리려면 Node를 선택).", MessageType.None);
            GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
            if (GUILayout.Button("🔄 선택한 것 90° 돌리기", GUILayout.Height(26))) RotateSelected90();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("색판(Panel)/구슬(Marker)만 옮겼을 때: 클릭 콜라이더(Btn_)·도착지점(Arrival_)을 그 위치로 맞춥니다. 옮긴 뒤 한 번 누르세요.", MessageType.None);
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("🔧 색판/구슬 위치로 정렬 (버튼+마커)", GUILayout.Height(26))) AlignButtonsToPanels();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("착지 마커가 안 보일 때: 방별 색 구슬로 보이게 + 한곳에 모읍니다.\n색: 드래프트=노랑 / 팀원=초록 / 갤러리=파랑.\n선택한 오브젝트(예: 알파벳 프랍) 위치로 모임. 선택 없으면 원점.", MessageType.None);
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button("📍 마커 보이게 + 한곳에 모으기 (전부)", GUILayout.Height(26))) GatherMarkers();
            GUI.backgroundColor = new Color(0.75f, 0.75f, 0.75f);
            if (GUILayout.Button("🙈 마커 구슬 숨기기(투명·게임에서 안 보이게)", GUILayout.Height(22))) HideMarkers();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("텔레포트 자동 연결: 4개 버튼에 PortalTeleport(UdonSharp) + destination을 자동 세팅. 실행 전 씬 저장 권장.", MessageType.Warning);
            GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
            if (GUILayout.Button("🔌 텔레포트 자동 연결 (버튼 4개)", GUILayout.Height(30))) WireTeleports();
            GUI.backgroundColor = Color.white;
        }

        // 깨진 컴포넌트 정리 → 프로그램 애셋 생성/컴파일 → 4개 버튼 연결
        void WireTeleports()
        {
            var root = GameObject.Find(RootName);
            if (root == null) { Debug.LogWarning("TeleportSystem이 없어요."); return; }
            var pairs = new string[][] {
                new[]{ "Btn_Draft_to_Team",    "Arrival_Team" },
                new[]{ "Btn_Draft_to_Gallery", "Arrival_Gallery" },
                new[]{ "Btn_Team_to_Draft",    "Arrival_Draft" },
                new[]{ "Btn_Gallery_to_Draft", "Arrival_Draft" },
            };
            var all = root.GetComponentsInChildren<Transform>(true);
            Transform FindT(string nm) { foreach (var t in all) if (t.name == nm) return t; return null; }

#if UDONSHARP
            // 0) 버튼의 기존/깨진 PortalTeleport + UdonBehaviour 싹 제거
            foreach (var p in pairs)
            {
                var btn = FindT(p[0]);
                if (btn == null) continue;
                foreach (var comp in btn.GetComponents<PortalTeleport>())
                {
                    var backing = UdonSharpEditor.UdonSharpEditorUtility.GetBackingUdonBehaviour(comp);
                    Object.DestroyImmediate(comp);
                    if (backing != null) Object.DestroyImmediate(backing);
                }
                foreach (var ub in btn.GetComponents<VRC.Udon.UdonBehaviour>()) Object.DestroyImmediate(ub);
            }

            // 1) 프로그램 애셋 없으면 만들고 컴파일 → 한 번 더 누르게 안내(컴파일 완료 후 연결)
            var progAsset = UdonSharp.UdonSharpProgramAsset.GetProgramAssetForClass(typeof(PortalTeleport));
            if (progAsset == null)
            {
                const string csPath = "Assets/CheeseSportsArena/Udon/PortalTeleport.cs";
                const string aPath = "Assets/CheeseSportsArena/Udon/PortalTeleport.asset";
                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(csPath);
                if (mono == null) { Debug.LogWarning("PortalTeleport.cs 를 못 찾음: " + csPath); return; }
                var np = ScriptableObject.CreateInstance<UdonSharp.UdonSharpProgramAsset>();
                np.sourceCsScript = mono;
                AssetDatabase.CreateAsset(np, aPath);
                EditorUtility.SetDirty(np);
                AssetDatabase.SaveAssets();
                UdonSharp.UdonSharpProgramAsset.CompileAllCsPrograms(true);
                AssetDatabase.Refresh();
                Debug.Log("✅ PortalTeleport 프로그램 애셋 생성+컴파일 완료. '🔌 텔레포트 자동 연결'을 한 번 더 눌러 연결하세요.");
                return;
            }

            // 2) 붙이고 destination 연결
            int n = 0;
            foreach (var p in pairs)
            {
                var btn = FindT(p[0]); var arr = FindT(p[1]);
                if (btn == null || arr == null) { Debug.LogWarning($"못 찾음: {p[0]} 또는 {p[1]}"); continue; }
                var pt = UdonSharpEditor.UdonSharpUndo.AddComponent(btn.gameObject, typeof(PortalTeleport)) as PortalTeleport;
                if (pt == null) { Debug.LogWarning($"{p[0]}: PortalTeleport 추가 실패"); continue; }
                pt.destination = arr;
                UdonSharpEditor.UdonSharpEditorUtility.CopyProxyToUdon(pt);
                n++;
            }
            Dirty();
            Debug.Log($"🔌 텔레포트 자동 연결 완료: {n}/4 버튼. 이제 버튼 누르면 이동해요.");
#else
            Debug.LogWarning("UdonSharp를 못 찾았어요(UDONSHARP 미정의). 각 버튼에 PortalTeleport를 수동으로 붙여주세요.");
#endif
        }

        // 마커 색 구슬 제거 → 착지 지점은 유지(빈 오브젝트), 게임에선 안 보임
        void HideMarkers()
        {
            var root = GameObject.Find(RootName);
            if (root == null) { Debug.LogWarning("TeleportSystem이 없어요."); return; }
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Arrival_")) continue;
                var vis = t.Find("Marker");
                if (vis != null) { Undo.DestroyObjectImmediate(vis.gameObject); n++; }
            }
            Dirty();
            Debug.Log($"🙈 마커 구슬 {n}개 숨김(제거). 착지 지점(Arrival_)은 그대로 남아 텔레포트는 정상. 다시 보려면 '📍 모으기'.");
        }

        // 착지 마커를 초록 구슬로 보이게 만들고, Arrival_Team 빼고 한곳에 모음
        void GatherMarkers()
        {
            var root = GameObject.Find(RootName);
            if (root == null) { Debug.LogWarning("TeleportSystem이 없어요 — 먼저 [배치]하세요."); return; }

            Vector3 target = Vector3.zero;
            var sel = Selection.activeGameObject;
            if (sel != null && !sel.name.StartsWith("Arrival_")) target = sel.transform.position;

            int idx = 0, n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Arrival_")) continue;
                EnsureMarkerVisual(t);   // 방별 색(드래프트=노랑/팀=초록/갤러리=파랑)
                Undo.RecordObject(t, "Gather Marker");
                t.position = target + new Vector3((idx % 4) * 0.7f, 0.6f, (idx / 4) * 0.7f);   // 살짝 벌려
                idx++; n++;
            }
            if (sel != null && !sel.name.StartsWith("Arrival_"))
                Debug.Log($"📍 마커 {n}개(Team 포함)를 '{sel.name}' 위치({target})로 모음(+초록구슬).");
            else
                Debug.Log($"📍 마커 {n}개(Team 포함)를 원점으로 모음(+초록구슬). 특정 위치로 모으려면 오브젝트 선택 후 다시 눌러요.");
            Dirty();
        }

        // 방별 마커 색: 드래프트=노랑 / 팀원=초록 / 갤러리=파랑
        Material MarkerMatFor(string arrivalName)
        {
            if (arrivalName.Contains("Gallery")) return MakeMat("MarkerGallery", new Color(0.30f, 0.60f, 1f));  // 파랑
            if (arrivalName.Contains("Team")) return MakeMat("MarkerTeam", new Color(0.20f, 1f, 0.40f));         // 초록
            return MakeMat("MarkerDraft", new Color(1f, 0.82f, 0.25f));                                          // 노랑(드래프트)
        }

        void EnsureMarkerVisual(Transform arrival)
        {
            Material mat = MarkerMatFor(arrival.name);
            var existing = arrival.Find("Marker");
            if (existing != null)   // 이미 있으면 색만 갱신
            {
                var er = existing.GetComponent<Renderer>();
                if (er != null) er.sharedMaterial = mat;
                return;
            }
            var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m.name = "Marker";
            var c = m.GetComponent<Collider>(); if (c != null) DestroyImmediate(c);
            m.transform.SetParent(arrival, false);
            m.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            m.transform.localScale = Vector3.one * 0.35f;
            var r = m.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = mat;
        }

        // Btn_(콜라이더 부모)를 자식 Panel(보이는 색판)의 월드 위치·회전으로 이동, Panel은 로컬0으로.
        // → 색판은 그 자리 그대로, 클릭 판정이 색판과 일치.
        void AlignButtonsToPanels()
        {
            var root = GameObject.Find(RootName);
            if (root == null) { Debug.LogWarning("TeleportSystem이 없어요 — 먼저 [배치]하세요."); return; }
            int nb = 0, nm = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Btn_"))
                {
                    var panel = t.Find("Panel");
                    if (panel == null) continue;
                    Undo.RecordObject(t, "Align"); Undo.RecordObject(panel, "Align");
                    t.position = panel.position; t.rotation = panel.rotation;   // 콜라이더=색판(위치+회전)
                    panel.localPosition = Vector3.zero; panel.localRotation = Quaternion.identity;
                    nb++;
                }
                else if (t.name.StartsWith("Arrival_"))
                {
                    var marker = t.Find("Marker");
                    if (marker == null) continue;
                    Undo.RecordObject(t, "Align"); Undo.RecordObject(marker, "Align");
                    t.position = marker.position;               // 도착지점=구슬(위치만, 회전 유지)
                    marker.localPosition = Vector3.zero;
                    nm++;
                }
            }
            Dirty();
            Debug.Log($"🔧 정렬 완료: 버튼 {nb}개(콜라이더=색판), 마커 {nm}개(도착지점=구슬 위치). 이제 클릭·착지 다 보이는 자리와 일치해요.");
        }

        // 씬에서 선택한 버튼/노드를 90° 회전 (자식도 함께)
        void RotateSelected90()
        {
            int n = 0;
            foreach (var go in Selection.gameObjects)
            {
                var t = go.transform;
                Undo.RecordObject(t, "Rotate 90");
                t.RotateAround(t.position, Vector3.up, 90f);
                n++;
            }
            if (n == 0) Debug.LogWarning("씬에서 버튼(Btn_…) 또는 노드(Node_…)를 선택한 뒤 눌러주세요.");
            else { Dirty(); Debug.Log($"🔄 선택 {n}개 90° 회전 완료."); }
        }

        void AutoFill()
        {
            // 드래프트룸 = 원형 방(GalleryRoom). 없으면 절차적 아레나(CheeseSportsArena)로 폴백.
            if (TryCenterXZ("GalleryRoom", out var d) || TryCenterXZ("CheeseSportsArena", out d)) draftPos = d;
            if (TryCenterXZ("GalleryHall", out var g)) galleryPos = g;   // 갤러리 = 별관 홀
            Debug.Log($"🔎 자동 위치: 드래프트룸={draftPos}, 갤러리={galleryPos}. 팀원룸(박스)은 직접 옮기세요.");
            Repaint();
        }

        static bool TryCenterXZ(string objName, out Vector2 xz)
        {
            xz = Vector2.zero;
            var go = GameObject.Find(objName);
            if (go == null) return false;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { xz = new Vector2(go.transform.position.x, go.transform.position.z); return true; }
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            xz = new Vector2(b.center.x, b.center.z);
            return true;
        }

        void Clear()
        {
            var e = GameObject.Find(RootName);
            if (e != null) { Undo.DestroyObjectImmediate(e); Dirty(); }
        }

        void Build()
        {
            Clear();
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Teleport");
            var T = root.transform;

            Material mTeamDraft = MakeMat("BtnTeamDraft", new Color(1f, 0.82f, 0.25f));      // 팀원↔드래프트 = 노랑
            Material mDraftGallery = MakeMat("BtnDraftGallery", new Color(0.30f, 0.60f, 1f)); // 드래프트↔갤러리 = 파랑

            // 드래프트룸(허브): 팀원룸(노랑)·갤러리(파랑)로 가는 버튼 2개 + 착지 마커 (위치=실배치 기억)
            var draft = MakeNode(T, "Node_DraftRoom", draftPos);
            MakeButton(draft, "Btn_Draft_to_Team", new Vector3(-6.4f, 2.54f, 0f), 0f, mTeamDraft);
            MakeButton(draft, "Btn_Draft_to_Gallery", new Vector3(6.53f, 2.55f, 0f), 0f, mDraftGallery);
            MakeArrival(draft, "Arrival_Draft", new Vector3(0f, 0.8f, 0f));

            // 팀원룸: 드래프트로(노랑) 버튼 1개 + 착지 마커
            var team = MakeNode(T, "Node_TeamRoom", teamPos);
            MakeButton(team, "Btn_Team_to_Draft", new Vector3(0f, 3.15f, 0f), 0f, mTeamDraft);
            MakeArrival(team, "Arrival_Team", new Vector3(0.07f, 1.09f, 1.81f));

            // 갤러리: 드래프트로(파랑) 버튼 1개 + 착지 마커
            var gallery = MakeNode(T, "Node_Gallery", galleryPos);
            MakeButton(gallery, "Btn_Gallery_to_Draft", new Vector3(-0.29f, 3.33f, 0f), 90f, mDraftGallery);
            MakeArrival(gallery, "Arrival_Gallery", new Vector3(6.28f, 0.73f, 0f));

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
            Dirty();
            Debug.Log(
                "🔘 텔레포트 버튼 배치 완료! 팀원룸(Node_TeamRoom)을 박스 방으로 옮기고, 버튼에 PortalTeleport 연결:\n" +
                "  Btn_Team_to_Draft → Arrival_Draft\n" +
                "  Btn_Gallery_to_Draft → Arrival_Draft\n" +
                "  Btn_Draft_to_Team → Arrival_Team\n" +
                "  Btn_Draft_to_Gallery → Arrival_Gallery");
        }

        Transform MakeNode(Transform parent, string name, Vector2 xz)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Node");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(xz.x, 0f, xz.y);
            return go.transform;
        }

        // 벽/바닥 버튼 패널: 색 패널 하나 + 클릭 콜라이더(로컬 +Z가 앞면)
        void MakeButton(Transform node, string name, Vector3 localPos, float yaw, Material color)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Button");
            go.transform.SetParent(node, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Prim(go.transform, "Panel", Vector3.zero, new Vector3(0.5f, 0.7f, 0.08f), color);   // 색 패널 하나

            var col = go.AddComponent<BoxCollider>();   // 클릭(Use)용 — PortalTeleport와 같은 오브젝트
            col.size = new Vector3(0.5f, 0.7f, 0.2f);
            col.center = Vector3.zero;
        }

        void MakeArrival(Transform node, string name, Vector3 localPos)
        {
            var m = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(m, "Arrival");
            m.transform.SetParent(node, false);
            m.transform.localPosition = localPos;                           // 실배치 기억한 착지 위치
            m.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);      // 방을 바라보게
        }

        Transform Prim(Transform parent, string name, Vector3 localPos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var c = go.GetComponent<Collider>(); if (c != null) DestroyImmediate(c);   // 시각용, 콜라이더 제거
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>(); if (r != null && m != null) r.sharedMaterial = m;
            return go.transform;
        }

        Material MakeMat(string name, Color c)
        {
            EnsureFolder(MatFolder);
            string path = $"{MatFolder}/{name}.mat";
            var sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != sh) m.shader = sh;
            m.color = c;
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var p = folder.Split('/'); string cur = p[0];
            for (int i = 1; i < p.Length; i++)
            {
                string nx = cur + "/" + p[i];
                if (!AssetDatabase.IsValidFolder(nx)) AssetDatabase.CreateFolder(cur, p[i]);
                cur = nx;
            }
        }

        void Dirty() => EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
