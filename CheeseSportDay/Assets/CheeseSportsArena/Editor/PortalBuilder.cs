// =============================================================
//  🧀 치즈 운동회 - 포탈 배치기 (PortalBuilder)
// -------------------------------------------------------------
//  드래프트장 ↔ 갤러리를 오가는 "빛나는 원형 포탈" 2개 +
//  도착 마커 2개를 자동 생성. (같은 월드 안 순간이동)
//
//  [배치] 후 각 포탈에 PortalTeleport(UdonSharp)만 붙여주면 됨:
//    · Portal_ToGallery  → destination = Arrival_AtGallery
//    · Portal_ToArena    → destination = Arrival_AtArena
//
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
        // 드래프트장(원점) 쪽 포탈
        Vector2 arenaPortalPos = new Vector2(16f, 0f);
        float arenaYaw = -90f;   // 원점(플레이어)을 바라보도록
        // 갤러리(별관, 기본 X+40) 쪽 포탈
        Vector2 galleryPortalPos = new Vector2(26f, 0f);
        float galleryYaw = 90f;

        float portalY = 1.4f;        // 디스크 중심 높이
        float portalDiameter = 2.4f; // 원형 지름(m)
        float arrivalOffset = 2.2f;  // 도착 포탈 앞으로 착지하는 거리

        Color glowColor = new Color(0.30f, 0.75f, 1f);   // 빛나는 파란빛
        Color rimColor = new Color(1f, 0.82f, 0.25f);    // 치즈 옐로우 테두리

        const string RootName = "PortalSystem";
        const string MatFolder = "Assets/CheeseSportsArena/Materials";

        [MenuItem("Tools/🧀 치즈 운동회/포탈 배치기")]
        static void Open()
        {
            var w = GetWindow<PortalBuilder>("🌀 포탈 배치기");
            w.minSize = new Vector2(320, 430);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "드래프트장 ↔ 갤러리 포탈 2개 + 도착 마커 2개를 만듭니다.\n" +
                "[배치] 후 각 포탈에 PortalTeleport 컴포넌트를 붙이고\n" +
                "  · Portal_ToGallery → destination = Arrival_AtGallery\n" +
                "  · Portal_ToArena  → destination = Arrival_AtArena\n" +
                "만 연결하면 끝!", MessageType.Info);

            EditorGUILayout.LabelField("드래프트장 쪽 포탈", EditorStyles.boldLabel);
            arenaPortalPos = EditorGUILayout.Vector2Field("위치 (X, Z)", arenaPortalPos);
            arenaYaw = EditorGUILayout.Slider("바라보는 방향(Y)", arenaYaw, -180f, 180f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("갤러리 쪽 포탈", EditorStyles.boldLabel);
            galleryPortalPos = EditorGUILayout.Vector2Field("위치 (X, Z)", galleryPortalPos);
            galleryYaw = EditorGUILayout.Slider("바라보는 방향(Y)", galleryYaw, -180f, 180f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("공통", EditorStyles.boldLabel);
            portalY = EditorGUILayout.Slider("높이(중심 Y)", portalY, 0.5f, 3f);
            portalDiameter = EditorGUILayout.Slider("지름(m)", portalDiameter, 1f, 5f);
            arrivalOffset = EditorGUILayout.Slider("착지 거리", arrivalOffset, 0.5f, 5f);
            glowColor = EditorGUILayout.ColorField("포탈 빛 색", glowColor);
            rimColor = EditorGUILayout.ColorField("테두리 색", rimColor);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            if (GUILayout.Button("🌀 배치", GUILayout.Height(36))) Build();
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("삭제", GUILayout.Height(36), GUILayout.Width(80))) Clear();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
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
            Undo.RegisterCreatedObjectUndo(root, "Portals");
            var T = root.transform;

            Material glow = MakeMat("PortalGlow", glowColor);
            Material rim = MakeMat("PortalRim", rimColor);

            // 두 포탈 생성
            Transform pGallery = BuildPortal(T, "Portal_ToGallery",
                new Vector3(arenaPortalPos.x, portalY, arenaPortalPos.y), arenaYaw, glow, rim);
            Transform pArena = BuildPortal(T, "Portal_ToArena",
                new Vector3(galleryPortalPos.x, portalY, galleryPortalPos.y), galleryYaw, glow, rim);

            // 도착 마커 (각 포탈 앞, 바닥 높이)
            Transform aGallery = MakeArrival(T, "Arrival_AtGallery", pArena, arrivalOffset);
            Transform aArena = MakeArrival(T, "Arrival_AtArena", pGallery, arrivalOffset);

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
            Dirty();
            Debug.Log(
                "🌀 포탈 배치 완료!\n" +
                "다음만 연결하세요:\n" +
                "  1) Portal_ToGallery 에 PortalTeleport 추가 → destination = Arrival_AtGallery\n" +
                "  2) Portal_ToArena   에 PortalTeleport 추가 → destination = Arrival_AtArena");
        }

        Transform BuildPortal(Transform parent, string name, Vector3 pos, float yaw, Material glow, Material rim)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Portal");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // 테두리(살짝 크고 뒤쪽)
            var rimDisc = MakeDisc("Rim", go.transform, portalDiameter * 1.14f, rim);
            rimDisc.localPosition = new Vector3(0f, 0f, -0.02f);
            // 빛나는 안쪽 디스크(앞쪽)
            MakeDisc("Glow", go.transform, portalDiameter, glow);

            // 클릭(Use)용 콜라이더 — UdonBehaviour와 같은 오브젝트에
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(portalDiameter, portalDiameter, 0.2f);
            col.center = Vector3.zero;

            return go.transform;
        }

        // 세워진 얇은 원판(디스크). 로컬 +Z가 앞면.
        Transform MakeDisc(string name, Transform parent, float diameter, Material mat)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            // 실린더 콜라이더 제거(디스크는 시각용)
            var cc = disc.GetComponent<Collider>();
            if (cc != null) DestroyImmediate(cc);
            disc.transform.SetParent(parent, false);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // 눕혀 세우기 → 원면이 Z를 향함
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localScale = new Vector3(diameter, 0.03f, diameter); // Y=두께
            var r = disc.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return disc.transform;
        }

        Transform MakeArrival(Transform parent, string name, Transform destPortal, float offset)
        {
            var m = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(m, "Arrival");
            m.transform.SetParent(parent, false);
            Vector3 p = destPortal.position + destPortal.forward * offset;
            p.y = 0f; // 바닥(발 위치)
            m.transform.position = p;
            m.transform.rotation = destPortal.rotation; // 방을 바라보게
            return m.transform;
        }

        Material MakeMat(string name, Color c)
        {
            EnsureFolder(MatFolder);
            string path = $"{MatFolder}/{name}.mat";
            var sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
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
