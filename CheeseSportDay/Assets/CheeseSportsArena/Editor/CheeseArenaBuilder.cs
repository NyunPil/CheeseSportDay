// =============================================================
//  🧀 치즈 운동회 - 드래프트 아레나 빌더 (오디토리움 버전)
// -------------------------------------------------------------
//  유니티 상단 메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 아레나 빌더 열기
//
//  구성:
//   - 정면 단(스테이지)
//   - 관중석: 좌 18(6×3) + 우 18(6×3), 가운데 통로, 뒤로 갈수록 단 상승
//   - 사각 오디토리움 벽 + 조명 + 스폰
//   ※ 스크린 / 팀장석 / 뺏기 버튼 / 현황패널은 제거됨 (관중석 의자만)
//  컬러: 화이트 베이스 + 옐로우/블루 포인트. (월드 내 텍스트 라벨 없음)
//
//  순수 유니티 기능만 사용(빌드용). VRChat 컴포넌트(스폰/스테이션/Udon)는 생성 후 부착.
// =============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseSports
{
    public class CheeseArenaBuilder : EditorWindow
    {
        // 관중석 (한 블록 = 6열 × 3행 = 18, 좌우 두 블록)
        int audColsPerSide = 6;
        int audRows = 3;
        float seatSpacingX = 1.5f;
        float rowDepth = 1.9f;
        float rowStep = 0.55f;      // 한 행 올라갈 때 높이
        float aisleHalf = 1.6f;     // 가운데 통로 반폭
        float audienceFrontZ = 6f;  // 맨 앞열 z (스테이지 앞)

        // 스테이지
        float stageFrontZ = 8f;
        float stageBackZ = 14f;
        float stageHeight = 1.0f;
        float stageHalfWidth = 10f;

        // 스크린 자리(조명·좌석 방향 기준점으로만 사용)
        float screenZ = 14f;
        float screenCenterY = 4f;

        // 방(사각 오디토리움)
        float roomHalfWidth = 13f;
        float roomBackZ = -2f;
        float roomFrontZ = 15.5f;
        float wallHeight = 7.5f;

        bool buildWalls = true;
        bool buildCeiling = true;
        bool buildLights = true;

        const string RootName = "CheeseSportsArena";
        const string MatFolder = "Assets/CheeseSportsArena/Materials";

        Dictionary<string, Material> _mats;

        [MenuItem("Tools/🧀 치즈 운동회/아레나 빌더 열기")]
        static void Open()
        {
            var w = GetWindow<CheeseArenaBuilder>("🧀 아레나 빌더");
            w.minSize = new Vector2(320, 540);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("🧀 치즈 운동회 (오디토리움)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("값 정하고 [맵 생성]. 다시 누르면 지우고 새로 만듭니다.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("관중석 (블록당 6×3 = 18)", EditorStyles.boldLabel);
            audColsPerSide = EditorGUILayout.IntSlider("가로(열) / 블록", audColsPerSide, 2, 10);
            audRows = EditorGUILayout.IntSlider("세로(행) / 블록", audRows, 1, 8);
            seatSpacingX = EditorGUILayout.Slider("좌석 가로 간격", seatSpacingX, 1.1f, 2.2f);
            rowDepth = EditorGUILayout.Slider("행 간격", rowDepth, 1.4f, 3f);
            rowStep = EditorGUILayout.Slider("행 단 높이", rowStep, 0.2f, 1.0f);
            aisleHalf = EditorGUILayout.Slider("가운데 통로 반폭", aisleHalf, 0.8f, 4f);

            EditorGUILayout.Space();
            buildWalls = EditorGUILayout.Toggle("벽", buildWalls);
            buildCeiling = EditorGUILayout.Toggle("천장+트러스", buildCeiling);
            buildLights = EditorGUILayout.Toggle("조명", buildLights);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            if (GUILayout.Button("🧀 맵 생성", GUILayout.Height(40))) Build();
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("맵 삭제", GUILayout.Height(40), GUILayout.Width(90))) Clear();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        // =========================================================
        Vector3 FocusPoint => new Vector3(0f, 2.5f, screenZ);   // 모든 좌석이 바라보는 곳

        void Clear()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null) { Undo.DestroyObjectImmediate(existing); MarkDirty(); }
        }

        void Build()
        {
            Clear();
            _mats = new Dictionary<string, Material>();
            EnsureFolder(MatFolder);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Cheese Auditorium");
            var T = root.transform;

            BuildShell(T);
            BuildStage(T);
            BuildAudience(T);
            if (buildLights) BuildLighting(T);
            BuildSpawn(T);

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
            MarkDirty();
            Debug.Log("🧀 오디토리움 생성 완료!");
        }

        // ---------- 바닥 / 벽 / 천장 ----------
        void BuildShell(Transform parent)
        {
            var shell = new GameObject("Shell").transform;
            shell.SetParent(parent, false);
            var floorMat = Mat("Floor", new Color(0.95f, 0.95f, 0.93f));
            var wallMat = Mat("Wall", new Color(0.98f, 0.98f, 0.96f));
            var accent = Mat("Accent", new Color(0.16f, 0.16f, 0.23f));

            float midZ = (roomFrontZ + roomBackZ) * 0.5f;
            float depth = roomFrontZ - roomBackZ;

            Box(shell, "Floor", new Vector3(0, -0.05f, midZ), new Vector3(roomHalfWidth * 2, 0.1f, depth), floorMat);
            // 가운데 통로 러너(블루 포인트)
            Box(shell, "AisleRunner", new Vector3(0, 0.01f, midZ), new Vector3(aisleHalf * 1.6f, 0.02f, depth), Mat("Blue", new Color(0.18f, 0.60f, 0.90f)));

            if (buildWalls)
            {
                Box(shell, "Wall_Back", new Vector3(0, wallHeight * 0.5f, roomBackZ), new Vector3(roomHalfWidth * 2, wallHeight, 0.3f), wallMat);
                Box(shell, "Wall_Front", new Vector3(0, wallHeight * 0.5f, roomFrontZ), new Vector3(roomHalfWidth * 2, wallHeight, 0.3f), wallMat);
                Box(shell, "Wall_Left", new Vector3(-roomHalfWidth, wallHeight * 0.5f, midZ), new Vector3(0.3f, wallHeight, depth), wallMat);
                Box(shell, "Wall_Right", new Vector3(roomHalfWidth, wallHeight * 0.5f, midZ), new Vector3(0.3f, wallHeight, depth), wallMat);
            }
            if (buildCeiling)
            {
                Box(shell, "Ceiling", new Vector3(0, wallHeight, midZ), new Vector3(roomHalfWidth * 2, 0.3f, depth), wallMat);
                // 무대 위 트러스(다크 포인트)
                Box(shell, "Truss", new Vector3(0, wallHeight - 0.6f, stageFrontZ + 0.5f), new Vector3(roomHalfWidth * 1.7f, 0.5f, 0.5f), accent);
            }
        }

        // ---------- 스테이지 ----------
        void BuildStage(Transform parent)
        {
            var stage = new GameObject("Stage").transform;
            stage.SetParent(parent, false);
            var top = Mat("StageTop", new Color(0.96f, 0.96f, 0.94f));
            var blue = Mat("Blue", new Color(0.18f, 0.60f, 0.90f));

            float midZ = (stageFrontZ + stageBackZ) * 0.5f;
            float d = stageBackZ - stageFrontZ;
            Box(stage, "Platform", new Vector3(0, stageHeight * 0.5f, midZ), new Vector3(stageHalfWidth * 2, stageHeight, d), top);
            // 앞 계단 2단
            Box(stage, "Step1", new Vector3(0, stageHeight * 0.33f, stageFrontZ - 0.5f), new Vector3(stageHalfWidth * 2, stageHeight * 0.66f, 1f), top);
            Box(stage, "Step2", new Vector3(0, stageHeight * 0.16f, stageFrontZ - 1.3f), new Vector3(stageHalfWidth * 2, stageHeight * 0.33f, 0.7f), top);
            // 앞면 블루 라인
            Box(stage, "FrontTrim", new Vector3(0, stageHeight * 0.5f, stageFrontZ + 0.02f), new Vector3(stageHalfWidth * 2, 0.18f, 0.05f), blue);
        }

        // ---------- 관중석 (좌 18 / 우 18, 6×3, 단형) ----------
        void BuildAudience(Transform parent)
        {
            var aud = new GameObject("AudienceSeats").transform;
            aud.SetParent(parent, false);
            var seatMat = Mat("AudienceSeat", new Color(0.20f, 0.62f, 0.92f));
            var accent = Mat("Accent", new Color(0.16f, 0.16f, 0.23f));
            var stepMat = Mat("Step", new Color(0.90f, 0.90f, 0.88f));

            int seatNo = 0;
            // side: -1 = 좌, +1 = 우
            for (int s = 0; s < 2; s++)
            {
                float side = (s == 0) ? -1f : 1f;
                for (int r = 0; r < audRows; r++)
                {
                    float z = audienceFrontZ - r * rowDepth;
                    float y = r * rowStep;
                    for (int c = 0; c < audColsPerSide; c++)
                    {
                        float x = side * (aisleHalf + 0.7f + c * seatSpacingX);
                        Vector3 pos = new Vector3(x, y, z);
                        if (y > 0.05f)
                            Box(aud, $"Riser_{s}_{r}_{c}", new Vector3(x, y * 0.5f, z),
                                new Vector3(seatSpacingX * 1.02f, y, rowDepth * 0.98f), stepMat);
                        BuildChair(aud, $"Seat_{++seatNo}", pos, seatMat, accent, 1f);
                    }
                }
            }
            Debug.Log($"🧀 관중석 {seatNo}석 (좌 {seatNo / 2} / 우 {seatNo / 2})");
        }

        // ---------- 조명 ----------
        void BuildLighting(Transform parent)
        {
            var lights = new GameObject("Lighting").transform;
            lights.SetParent(parent, false);

            var sun = new GameObject("Directional");
            sun.transform.SetParent(lights, false);
            sun.transform.rotation = Quaternion.Euler(55f, 200f, 0);
            var dl = sun.AddComponent<Light>();
            dl.type = LightType.Directional; dl.intensity = 0.9f; dl.color = new Color(1f, 0.99f, 0.96f);

            AddPoint(lights, "ScreenGlow", new Vector3(0, screenCenterY, screenZ - 2f), new Color(0.7f, 0.85f, 1f), 1.3f, 16f);
            AddPoint(lights, "StageGlow", new Vector3(0, wallHeight - 1f, (stageFrontZ + stageBackZ) * 0.5f), new Color(1f, 0.97f, 0.85f), 1.1f, 16f);
            AddPoint(lights, "HouseGlow", new Vector3(0, wallHeight - 1f, audienceFrontZ - audRows * rowDepth * 0.5f), new Color(1f, 1f, 0.98f), 0.9f, 20f);
        }

        void AddPoint(Transform parent, string name, Vector3 pos, Color c, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range;
        }

        // ---------- 스폰 ----------
        void BuildSpawn(Transform parent)
        {
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(parent, false);
            float z = audienceFrontZ - audRows * rowDepth - 1.5f;
            spawn.transform.localPosition = new Vector3(0, 0.05f, z);
            spawn.transform.localRotation = Quaternion.LookRotation(new Vector3(0, 0, 1)); // 무대 봄
            Cyl(spawn.transform, "Marker", new Vector3(0, 0.03f, 0), new Vector3(1.2f, 0.05f, 1.2f),
                MatEmissive("SpawnMark", new Color(0.4f, 1f, 0.6f), 0.5f));
        }

        // =========================================================
        //  의자 (로컬 +Z 방향을 바라보게 제작 → FocusPoint 향해 회전)
        // =========================================================
        Transform BuildChair(Transform parent, string name, Vector3 worldPos, Material seatMat, Material accentMat, float scale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = worldPos;
            Vector3 dir = FocusPoint - worldPos; dir.y = 0;
            if (dir.sqrMagnitude > 0.001f) root.localRotation = Quaternion.LookRotation(dir);

            float k = scale;
            Box(root, "Seat", new Vector3(0, 0.45f * k, 0), new Vector3(0.55f * k, 0.12f * k, 0.55f * k), seatMat);
            Box(root, "Back", new Vector3(0, 0.78f * k, -0.22f * k), new Vector3(0.55f * k, 0.5f * k, 0.1f * k), seatMat);
            Box(root, "Pedestal", new Vector3(0, 0.22f * k, 0), new Vector3(0.18f * k, 0.45f * k, 0.18f * k), accentMat);
            return root;
        }

        // =========================================================
        //  프리미티브 헬퍼
        // =========================================================
        Transform Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material m)
            => Prim(parent, name, PrimitiveType.Cube, localPos, size, Quaternion.identity, m);
        Transform Cyl(Transform parent, string name, Vector3 localPos, Vector3 size, Material m)
            => Prim(parent, name, PrimitiveType.Cylinder, localPos, size, Quaternion.identity, m);

        Transform Prim(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 size, Quaternion localRot, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            if (type == PrimitiveType.Cylinder)
                go.transform.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
            else
                go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            if (r != null && m != null) r.sharedMaterial = m;
            return go.transform;
        }

        // =========================================================
        //  머티리얼
        // =========================================================
        Material Mat(string name, Color c) => GetMat(name, c, 0f, Color.black);
        Material MatEmissive(string name, Color c, float intensity) => GetMat(name, c, intensity, c);

        Material GetMat(string name, Color c, float emission, Color emColor)
        {
            if (_mats == null) _mats = new Dictionary<string, Material>();
            if (_mats.TryGetValue(name, out var cached)) return cached;

            string path = $"{MatFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { _mats[name] = existing; return existing; }

            var m = new Material(Shader.Find("Standard")) { color = c };
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emColor * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(m, path);
            _mats[name] = m;
            return m;
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

        void MarkDirty() => EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
