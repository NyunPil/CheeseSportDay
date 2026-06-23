// =============================================================
//  🧀 치즈 운동회 - 드래프트 아레나 빌더 (Cheese Sports Arena Builder)
// -------------------------------------------------------------
//  유니티 상단 메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 아레나 빌더 열기
//  창이 뜨면 숫자만 만지고 [맵 생성] 버튼 한 번이면 끝.
//
//  - 정면 대형 스크린 (드래프트 화면)
//  - 그 앞 팀장 좌석 (둥근 앞열, 팀이름 라벨 + 빨간 "뺏어오기" 버튼)
//  - 양옆 드래프트 현황 패널 4개
//  - 뒤쪽으로 단(段) 올라가는 원형 경기장식 관중석 (팀원/관객이 같이 참여하는 느낌)
//  - 바깥 원형 벽 + 입구 + 조명 + 스폰 위치
//
//  이 파일은 순수 유니티 기능만 사용합니다(빌드용). VRChat 컴포넌트(스폰/의자
//  스테이션/Udon)는 생성 후 붙이면 됩니다 — 자세한 건 README.md 참고.
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
        // ---------- 설정값 (창에서 조절 가능) ----------
        [Header("팀 구성")]
        int teamCount = 5;                 // 팀(=팀장) 수. 4로 줄이면 자동 재배치
        int membersPerTeam = 4;            // 팀장 포함 인원 (라벨용)

        [Header("팀장 앞열")]
        float leaderRadius = 4.5f;         // 팀장 앞열 반지름
        float leaderArcDeg = 130f;         // 팀장 앞열이 펼쳐지는 각도

        [Header("관중석(원형 경기장)")]
        int audienceTiers = 4;             // 관중석 단 수
        float audienceArcDeg = 210f;       // 관중석이 감싸는 각도 (클수록 원형에 가까움)
        float tierStartRadius = 8f;        // 첫 단 반지름
        float tierDepth = 2.3f;            // 단 사이 간격(깊이)
        float tierStep = 0.95f;            // 단 하나 올라갈 때 높이
        float seatSpacing = 1.7f;          // 관중 좌석 간격

        [Header("스크린")]
        float screenDistance = 12f;        // 중심에서 스크린까지 거리
        float screenWidth = 9f;
        float screenHeight = 5f;
        float screenCenterY = 3.6f;

        [Header("기타")]
        bool buildOuterWall = true;
        bool buildLights = true;

        static readonly string[] DefaultTeamNames = { "열무", "느루", "호두", "반부", "오늘" };

        const string RootName = "CheeseSportsArena";
        const string MatFolder = "Assets/CheeseSportsArena/Materials";

        Dictionary<string, Material> _mats;

        [MenuItem("Tools/🧀 치즈 운동회/아레나 빌더 열기")]
        static void Open()
        {
            var w = GetWindow<CheeseArenaBuilder>("🧀 아레나 빌더");
            w.minSize = new Vector2(320, 520);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("🧀 치즈 운동회 드래프트 아레나", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("값을 정하고 [맵 생성]을 누르세요.\n다시 누르면 깨끗이 지우고 새로 만듭니다.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("팀 구성", EditorStyles.boldLabel);
            teamCount = EditorGUILayout.IntSlider("팀 수", teamCount, 2, 6);
            membersPerTeam = EditorGUILayout.IntSlider("팀당 인원(팀장 포함)", membersPerTeam, 2, 6);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("팀장 앞열", EditorStyles.boldLabel);
            leaderRadius = EditorGUILayout.Slider("앞열 반지름", leaderRadius, 2.5f, 8f);
            leaderArcDeg = EditorGUILayout.Slider("앞열 펼침 각도", leaderArcDeg, 60f, 200f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("관중석 (원형 경기장)", EditorStyles.boldLabel);
            audienceTiers = EditorGUILayout.IntSlider("단 수", audienceTiers, 1, 8);
            audienceArcDeg = EditorGUILayout.Slider("감싸는 각도", audienceArcDeg, 90f, 320f);
            tierStartRadius = EditorGUILayout.Slider("첫 단 반지름", tierStartRadius, 6f, 14f);
            tierDepth = EditorGUILayout.Slider("단 간격", tierDepth, 1.6f, 4f);
            tierStep = EditorGUILayout.Slider("단 높이", tierStep, 0.5f, 1.6f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("스크린", EditorStyles.boldLabel);
            screenDistance = EditorGUILayout.Slider("스크린 거리", screenDistance, 8f, 18f);
            screenWidth = EditorGUILayout.Slider("스크린 너비", screenWidth, 5f, 16f);
            screenHeight = EditorGUILayout.Slider("스크린 높이", screenHeight, 3f, 9f);

            EditorGUILayout.Space();
            buildOuterWall = EditorGUILayout.Toggle("바깥 원형 벽", buildOuterWall);
            buildLights = EditorGUILayout.Toggle("조명 생성", buildLights);

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
        //  생성 / 삭제
        // =========================================================
        void Clear()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                MarkDirty();
            }
        }

        // 배치/CLI 검증용 진입점: Unity.exe -executeMethod CheeseSports.CheeseArenaBuilder.BuildHeadless
        public static void BuildHeadless()
        {
            var w = CreateInstance<CheeseArenaBuilder>();
            w.Build();
            DestroyImmediate(w);
        }

        void Build()
        {
            Clear();
            _mats = new Dictionary<string, Material>();
            EnsureFolder(MatFolder);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Cheese Arena");
            var T = root.transform;

            float outerRadius = tierStartRadius + audienceTiers * tierDepth + 3f;

            BuildFloor(T, outerRadius);
            BuildScreen(T);
            BuildDraftPanels(T);
            BuildLeaderRow(T);
            BuildAudience(T);
            if (buildOuterWall) BuildOuterWall(T, outerRadius);
            if (buildLights) BuildLighting(T, outerRadius);
            BuildSpawn(T, outerRadius);

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
            MarkDirty();
            Debug.Log("🧀 치즈 운동회 아레나 생성 완료!");
        }

        // 모든 좌석/관중이 바라보는 초점 = 스크린 중앙
        Vector3 FocusPoint => new Vector3(0f, screenCenterY * 0.6f, screenDistance);

        // =========================================================
        //  바닥
        // =========================================================
        void BuildFloor(Transform parent, float outerRadius)
        {
            var floor = Cyl(parent, "Floor", new Vector3(0, -0.05f, screenDistance * 0.25f),
                new Vector3(outerRadius * 2.2f, 0.1f, outerRadius * 2.2f), Mat("Floor", new Color(0.96f, 0.93f, 0.84f)));
            // 중앙 무대(스테이지) 살짝 강조
            Cyl(parent, "CenterStage", new Vector3(0, 0.06f, screenDistance * 0.45f),
                new Vector3(7f, 0.12f, 7f), Mat("Stage", new Color(1f, 0.86f, 0.42f)));
        }

        // =========================================================
        //  스크린 (드래프트 메인 화면)
        // =========================================================
        void BuildScreen(Transform parent)
        {
            var screenRoot = new GameObject("MainScreen").transform;
            screenRoot.SetParent(parent, false);
            var accent = Mat("Accent", new Color(0.16f, 0.16f, 0.23f));

            // 프레임
            Box(screenRoot, "Frame", new Vector3(0, screenCenterY, screenDistance),
                new Vector3(screenWidth + 0.7f, screenHeight + 0.7f, 0.4f), accent);
            // 디스플레이 (양면 보이게 얇은 박스 + 발광)
            Box(screenRoot, "Display", new Vector3(0, screenCenterY, screenDistance - 0.25f),
                new Vector3(screenWidth, screenHeight, 0.08f),
                MatEmissive("ScreenDisplay", new Color(0.62f, 0.80f, 1f), 1.2f));
            // 받침 다리
            Box(screenRoot, "Stand_L", new Vector3(-screenWidth * 0.32f, (screenCenterY - screenHeight * 0.5f) * 0.5f, screenDistance),
                new Vector3(0.3f, screenCenterY - screenHeight * 0.5f, 0.3f), accent);
            Box(screenRoot, "Stand_R", new Vector3(screenWidth * 0.32f, (screenCenterY - screenHeight * 0.5f) * 0.5f, screenDistance),
                new Vector3(0.3f, screenCenterY - screenHeight * 0.5f, 0.3f), accent);

            Label(screenRoot, "DRAFT", new Vector3(0, screenCenterY + screenHeight * 0.5f + 0.6f, screenDistance - 0.3f),
                "🧀 DRAFT 🧀", 0.5f, new Color(0.2f, 0.2f, 0.28f));
        }

        // =========================================================
        //  드래프트 현황 패널 (양쪽 2개씩 = 4개)
        // =========================================================
        void BuildDraftPanels(Transform parent)
        {
            var panelRoot = new GameObject("DraftPanels").transform;
            panelRoot.SetParent(parent, false);
            var accent = Mat("Accent", new Color(0.16f, 0.16f, 0.23f));
            var disp = MatEmissive("PanelDisplay", new Color(0.95f, 0.97f, 0.7f), 0.4f);

            // 팀 수만큼 현황패널 1개씩 — 메인 스크린을 가리지 않게 양옆으로 분산.
            // 왼쪽 ceil(N/2)개, 오른쪽 나머지 (5팀이면 왼3 / 오2). 모두 관객을 바라봄.
            float gap = 3.0f;
            float screenHalf = screenWidth * 0.5f + 1.6f;
            float z = screenDistance;
            int leftCount = Mathf.CeilToInt(teamCount / 2f);
            for (int i = 0; i < teamCount; i++)
            {
                float x;
                if (i < leftCount)                       // 왼쪽: 바깥→안쪽 순서로 (열무가 가장 왼쪽)
                    x = -(screenHalf + (leftCount - 1 - i) * gap);
                else                                     // 오른쪽: 안쪽→바깥 순서로
                    x = screenHalf + (i - leftCount) * gap;

                var p = new GameObject($"Panel_{i + 1}").transform;
                p.SetParent(panelRoot, false);
                p.position = new Vector3(x, 0, z);
                // 관객(뒤쪽, -z)을 향하게
                p.rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));

                Box(p, "Post", new Vector3(0, 1.0f, 0), new Vector3(0.18f, 2.0f, 0.18f), accent);
                Box(p, "Board", new Vector3(0, 2.4f, 0), new Vector3(2.7f, 1.5f, 0.1f), disp);

                string team = i < DefaultTeamNames.Length ? DefaultTeamNames[i] : $"팀{i + 1}";
                // 팀 이름 (보드 위) — 관객 방향(local +z = world -z)에 표기
                Label(p, "TeamName", new Vector3(0, 3.35f, 0.07f), team, 0.30f, new Color(0.2f, 0.2f, 0.28f));
                // 로스터 자리(팀장 + 팀원 슬롯) — Udon에서 픽 결과로 채움
                int slots = Mathf.Max(1, membersPerTeam);
                string roster = "[ " + team + " ]";
                for (int s = 0; s < slots; s++) roster += "\n· ____";
                Label(p, "Roster", new Vector3(0, 2.4f, 0.07f), roster, 0.14f, new Color(0.28f, 0.28f, 0.34f));
            }
        }

        // =========================================================
        //  팀장 앞열 (둥근 앞열 + 팀이름 + 뺏어오기 버튼)
        // =========================================================
        void BuildLeaderRow(Transform parent)
        {
            var row = new GameObject("LeaderSeats").transform;
            row.SetParent(parent, false);
            var gold = Mat("LeaderSeat", new Color(1f, 0.84f, 0.20f));
            var accent = Mat("Accent", new Color(0.16f, 0.16f, 0.23f));
            var red = MatEmissive("StealRed", new Color(0.92f, 0.16f, 0.16f), 0.8f);

            for (int i = 0; i < teamCount; i++)
            {
                float a = Mathf.Deg2Rad * Lerp(-leaderArcDeg * 0.5f, leaderArcDeg * 0.5f, teamCount, i);
                // 팀장 앞열을 둥근 v(U) 모양으로: 가운데가 스크린에서 멀고, 양 끝이 스크린에 가까움.
                // z = R*(1 - cos) → a=0(가운데)일 때 최소, 양 끝일 때 커짐
                Vector3 pos = new Vector3(Mathf.Sin(a) * leaderRadius, 0, leaderRadius * (1f - Mathf.Cos(a)) + 1.5f);
                string team = i < DefaultTeamNames.Length ? DefaultTeamNames[i] : $"팀{i + 1}";

                var seat = BuildChair(row, $"Leader_{team}", pos, gold, accent, 1.3f);

                // 팀 이름 라벨 (의자 위에 떠있게)
                Label(seat, "Name", new Vector3(0, 1.9f, -0.1f), team, 0.32f, new Color(0.2f, 0.2f, 0.28f));

                // 뺏어오기 빨간 버튼 — 팀장 "앞"(스크린 쪽 = 로컬 +z)에 podium으로 배치
                Box(seat, "StealButtonBase", new Vector3(0, 0.45f, 0.95f), new Vector3(0.5f, 0.9f, 0.5f), accent);
                Cyl(seat, "StealButton", new Vector3(0, 0.93f, 0.95f), new Vector3(0.34f, 0.07f, 0.34f), red);
            }
        }

        // =========================================================
        //  관중석 (원형 경기장식, 단별로 올라감)
        // =========================================================
        void BuildAudience(Transform parent)
        {
            var aud = new GameObject("AudienceStands").transform;
            aud.SetParent(parent, false);
            var seatMat = Mat("Seat", new Color(0.97f, 0.95f, 0.90f));
            var accent = Mat("Accent", new Color(0.16f, 0.16f, 0.23f));
            var stepMat = Mat("Step", new Color(0.90f, 0.86f, 0.76f));

            float arcRad = Mathf.Deg2Rad * audienceArcDeg;
            int seatNo = 0;

            for (int t = 0; t < audienceTiers; t++)
            {
                float radius = tierStartRadius + t * tierDepth;
                float y = (t + 1) * tierStep;
                int seatsThisTier = Mathf.Max(3, Mathf.RoundToInt((radius * arcRad) / seatSpacing));

                for (int s = 0; s < seatsThisTier; s++)
                {
                    // 관중은 뒤쪽(중심 기준 -z)에서 스크린(+z)을 바라봄.
                    // a=180° → 뒤쪽(z 음수), 좌우로 펼쳐지며 스크린을 감쌈
                    float a = Mathf.Deg2Rad * Lerp(180f - audienceArcDeg * 0.5f, 180f + audienceArcDeg * 0.5f, seatsThisTier, s);
                    Vector3 pos = new Vector3(Mathf.Sin(a) * radius, y, Mathf.Cos(a) * radius);

                    // 단(계단) 받침
                    Box(aud, $"Riser_{t}_{s}", new Vector3(pos.x, y * 0.5f, pos.z),
                        new Vector3(seatSpacing * 1.02f, y, tierDepth * 0.98f),
                        AngleY(pos), stepMat);

                    // 좌석
                    BuildChair(aud, $"Seat_{++seatNo}", pos, seatMat, accent, 1f);
                }
            }
            Debug.Log($"🧀 관중석 좌석 {seatNo}석 생성 (팀원 정원과 별개로 여유 배치)");
        }

        // =========================================================
        //  바깥 원형 벽 (+ 입구 틈)
        // =========================================================
        void BuildOuterWall(Transform parent, float outerRadius)
        {
            var wallRoot = new GameObject("OuterWall").transform;
            wallRoot.SetParent(parent, false);
            var wallMat = Mat("Wall", new Color(1f, 0.80f, 0.30f));
            var rimMat = Mat("Accent", new Color(0.16f, 0.16f, 0.23f));

            int segs = 28;
            float r = outerRadius + 1.5f;
            float wallH = 5.5f;
            Vector3 center = new Vector3(0, 0, screenDistance * 0.25f);

            for (int i = 0; i < segs; i++)
            {
                // 스크린 쪽(앞, i가 segs 근처) 2칸은 입구로 비움
                if (i == 0 || i == 1) continue;
                float a = Mathf.Deg2Rad * (i * 360f / segs);
                Vector3 pos = center + new Vector3(Mathf.Sin(a) * r, wallH * 0.5f, Mathf.Cos(a) * r);
                float segLen = (2f * Mathf.PI * r) / segs + 0.2f;
                var rot = Quaternion.LookRotation(new Vector3(pos.x - center.x, 0, pos.z - center.z));
                Box(wallRoot, $"Wall_{i}", pos, new Vector3(segLen, wallH, 0.3f), rot, wallMat);
                // 윗 테두리
                Box(wallRoot, $"Rim_{i}", pos + new Vector3(0, wallH * 0.5f + 0.15f, 0), new Vector3(segLen, 0.3f, 0.45f), rot, rimMat);
            }
        }

        // =========================================================
        //  조명
        // =========================================================
        void BuildLighting(Transform parent, float outerRadius)
        {
            var lights = new GameObject("Lighting").transform;
            lights.SetParent(parent, false);

            var sun = new GameObject("Directional");
            sun.transform.SetParent(lights, false);
            sun.transform.rotation = Quaternion.Euler(55f, -30f, 0);
            var dl = sun.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.intensity = 1.0f;
            dl.color = new Color(1f, 0.98f, 0.92f);

            // 스크린 글로우
            AddPoint(lights, "ScreenGlow", new Vector3(0, screenCenterY, screenDistance - 1.5f), new Color(0.7f, 0.85f, 1f), 1.5f, 14f);
            // 중앙 무대 스포트
            AddPoint(lights, "StageGlow", new Vector3(0, 6f, screenDistance * 0.4f), new Color(1f, 0.92f, 0.7f), 1.2f, outerRadius);
        }

        void AddPoint(Transform parent, string name, Vector3 pos, Color c, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c; l.intensity = intensity; l.range = range;
        }

        // =========================================================
        //  스폰 위치
        // =========================================================
        void BuildSpawn(Transform parent, float outerRadius)
        {
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(parent, false);
            spawn.transform.position = new Vector3(0, 0.05f, -(tierStartRadius + audienceTiers * tierDepth + 1f));
            spawn.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1)); // 스크린 바라봄
            // 표식
            Cyl(spawn.transform, "Marker", new Vector3(0, 0.03f, 0), new Vector3(1.2f, 0.05f, 1.2f),
                MatEmissive("SpawnMark", new Color(0.4f, 1f, 0.6f), 0.5f));
        }

        // =========================================================
        //  의자 만들기 (로컬 +Z 방향을 바라보도록 제작)
        // =========================================================
        Transform BuildChair(Transform parent, string name, Vector3 worldPos, Material seatMat, Material accentMat, float scale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = worldPos;
            Vector3 dir = FocusPoint - worldPos; dir.y = 0;
            if (dir.sqrMagnitude > 0.001f) root.rotation = Quaternion.LookRotation(dir);

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
        Transform Box(Transform parent, string name, Vector3 localPos, Vector3 size, Quaternion rot, Material m)
            => Prim(parent, name, PrimitiveType.Cube, localPos, size, rot, m);
        Transform Cyl(Transform parent, string name, Vector3 localPos, Vector3 size, Material m)
            => Prim(parent, name, PrimitiveType.Cylinder, localPos, size, Quaternion.identity, m);

        Transform Prim(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 size, Quaternion localRot, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            // 실린더 기본 높이는 2 → y스케일 보정
            if (type == PrimitiveType.Cylinder)
                go.transform.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
            else
                go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            if (r != null && m != null) r.sharedMaterial = m;
            return go.transform;
        }

        void Label(Transform parent, string name, Vector3 localPos, string text, float size, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = size;
            tm.fontSize = 60;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = c;
        }

        // 받침대를 좌석 방향에 맞춰 살짝 회전
        Quaternion AngleY(Vector3 pos)
        {
            Vector3 dir = FocusPoint - pos; dir.y = 0;
            return dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : Quaternion.identity;
        }

        // i번째를 count개 구간에 고르게 (1개면 중앙)
        float Lerp(float from, float to, int count, int i)
            => count <= 1 ? (from + to) * 0.5f : Mathf.Lerp(from, to, i / (float)(count - 1));

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

            var shader = Shader.Find("Standard");
            var m = new Material(shader) { color = c };
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
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        void MarkDirty()
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
