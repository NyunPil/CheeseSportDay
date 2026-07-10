// =============================================================
//  🧀 치즈 운동회 - 드래프트룸 가벽 돔 (SkyDomeBuilder)
// -------------------------------------------------------------
//  선택한 오브젝트(드래프트룸)를 감싸는 큰 구를 만들어 가짜 벽으로 씀.
//  · 안쪽에서 보이게 노멀/와인딩 반전
//  · 위 절반 = 하늘 이미지 / 아래 절반 = 바닥 이미지 (Unlit, 빛 영향 X)
//  · 콜라이더 없음 (시각용)
//
//  메뉴  Tools ▸ 🧀 치즈 운동회 ▸ 🌐 드래프트룸 가벽 돔
// =============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseSports
{
    public class SkyDomeBuilder : EditorWindow
    {
        Texture2D bgTex;             // 하늘+바다 통짜 배경(위=하늘, 아래=바다)
        float radiusScale = 1.35f;   // 방 크기 대비 여유
        int longitude = 48;
        int latitude = 32;
        bool addCollider = false;

        const string MatFolder = "Assets/CheeseSportsArena/Materials";
        const string MeshFolder = "Assets/CheeseSportsArena/Meshes";
        const string TexFolder = "Assets/CheeseSportsArena/Textures";
        const string DomeName = "DraftRoom_SkyDome";

        [MenuItem("Tools/🧀 치즈 운동회/🌐 드래프트룸 가벽 돔")]
        static void Open()
        {
            var w = GetWindow<SkyDomeBuilder>("🌐 가벽 돔");
            w.minSize = new Vector2(320, 260);
        }

        void OnEnable()
        {
            // 바다하늘 이미지가 있으면 자동으로 채워둠
            if (bgTex == null)
                bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/바다하늘.png");
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "드래프트룸을 감싸는 가벽 돔을 만듭니다.\n" +
                "1) 배경 이미지(위=하늘/아래=바다 한 장)를 넣고\n" +
                "2) Hierarchy에서 드래프트룸 오브젝트를 선택한 뒤\n" +
                "3) [돔 생성]을 누르세요. (수평선이 돔 가운데에 맞춰짐)", MessageType.Info);

            EditorGUILayout.Space();
            bgTex = (Texture2D)EditorGUILayout.ObjectField("배경 이미지(하늘+바다)", bgTex, typeof(Texture2D), false);

            EditorGUILayout.Space();
            radiusScale = EditorGUILayout.Slider("크기 여유(반지름 배율)", radiusScale, 1.0f, 3.0f);
            longitude = EditorGUILayout.IntSlider("가로 분할(둥근 정도)", longitude, 12, 96);
            latitude = EditorGUILayout.IntSlider("세로 분할", latitude, 8, 64);
            addCollider = EditorGUILayout.Toggle("콜라이더 추가(보통 끔)", addCollider);

            EditorGUILayout.Space();
            var sel = Selection.activeGameObject;
            EditorGUILayout.LabelField("선택된 드래프트룸", sel ? sel.name : "(없음 — 선택하세요)");

            EditorGUILayout.Space();
            GUI.enabled = sel != null;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("🌐 돔 생성 (선택 오브젝트 감싸기)", GUILayout.Height(38))) Build(sel);
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.Space();
            if (GUILayout.Button("기존 돔 삭제", GUILayout.Height(22))) Clear();
        }

        void Clear()
        {
            var e = GameObject.Find(DomeName);
            if (e != null) { Undo.DestroyObjectImmediate(e); Dirty(); Debug.Log("🌐 기존 가벽 돔 삭제."); }
        }

        void Build(GameObject target)
        {
            if (target == null) { Debug.LogWarning("드래프트룸 오브젝트를 먼저 선택하세요."); return; }

            // 선택 오브젝트(자식 포함) 렌더러 바운즈로 중심·반지름 계산
            var rends = target.GetComponentsInChildren<Renderer>(true);
            Vector3 center; float radius;
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                center = b.center;
                radius = b.extents.magnitude * radiusScale;
            }
            else
            {
                center = target.transform.position;
                radius = 15f * radiusScale;
                Debug.LogWarning("선택 오브젝트에 렌더러가 없어 기본 반지름을 씁니다. 생성 후 크기 조절하세요.");
            }

            Clear();

            var mesh = BuildInvertedSphere(longitude, latitude, radius);
            EnsureFolder(MeshFolder);
            string meshPath = AssetDatabase.GenerateUniqueAssetPath($"{MeshFolder}/{DomeName}_Mesh.asset");
            AssetDatabase.CreateAsset(mesh, meshPath);

            var go = new GameObject(DomeName);
            Undo.RegisterCreatedObjectUndo(go, "Create Dome");
            go.transform.position = center;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sharedMaterial = MakeUnlitMat("SkyDome_BG", bgTex, new Color(0.55f, 0.75f, 1f));

            if (addCollider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = go;
            Dirty();
            Debug.Log($"🌐 가벽 돔 생성 완료! 중심={center}, 반지름≈{radius:F1}. " +
                      (bgTex == null ? "※ 배경 이미지를 안 넣어 단색으로 채웠어요. 이미지 넣고 다시 생성하세요." : "배경 이미지가 세로로 이어붙여졌어요(위=하늘/아래=바다)."));
        }

        // 안에서 보이는(반전) 구. 한 장 배경을 세로로 이어붙임(아래→위 = 이미지 아래→위)
        Mesh BuildInvertedSphere(int lon, int lat, float radius)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();

            for (int y = 0; y <= lat; y++)
            {
                float v = (float)y / lat;          // 0 아래(바다) .. 1 위(하늘)
                float phi = (1f - v) * Mathf.PI;   // 위(0) .. 아래(PI)
                float py = Mathf.Cos(phi);
                float pr = Mathf.Sin(phi);
                for (int x = 0; x <= lon; x++)
                {
                    float u = (float)x / lon;
                    float theta = u * Mathf.PI * 2f;
                    var pos = new Vector3(pr * Mathf.Cos(theta), py, pr * Mathf.Sin(theta)) * radius;
                    verts.Add(pos);
                    normals.Add(-pos.normalized);   // 안쪽을 향하게
                    uvs.Add(new Vector2(u, v));      // v를 그대로 → 이미지 아래(바다)=돔 아래, 위(하늘)=돔 위
                }
            }

            var tris = new List<int>();
            int stride = lon + 1;
            for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int i0 = y * stride + x;
                int i1 = i0 + 1;
                int i2 = i0 + stride;
                int i3 = i2 + 1;
                // 안쪽에서 보이게 와인딩 반전
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i2); tris.Add(i1); tris.Add(i3);
            }

            var m = new Mesh { name = DomeName + "_Mesh" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetNormals(normals);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        Material MakeUnlitMat(string name, Texture2D tex, Color fallback)
        {
            EnsureFolder(MatFolder);
            string path = $"{MatFolder}/{name}.mat";
            var sh = Shader.Find(tex != null ? "Unlit/Texture" : "Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != sh) m.shader = sh;
            if (tex != null) m.mainTexture = tex;
            else m.color = fallback;
            EditorUtility.SetDirty(m);
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
