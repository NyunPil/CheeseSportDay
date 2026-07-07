#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CheeseSportDay.Editor
{
    public static class CameraTvSetupMenu
    {
        private const string AssetFolder = "Assets/CheeseSportDay/TV";
        private const string DisplayLayerName = "CameraFeedDisplay";

        [MenuItem("Cheese Sport Day/Create Camera TV System")]
        public static void CreateCameraTvSystem()
        {
            EnsureAssetFolder();

            int displayLayer = GetOrCreateDisplayLayer();
            RenderTexture renderTexture = CreateRenderTexture();
            Material screenMaterial = CreateScreenMaterial(renderTexture);
            Material bodyMaterial = CreateBodyMaterial();

            GameObject root = new GameObject("Camera TV System");
            Undo.RegisterCreatedObjectUndo(root, "Create Camera TV System");
            root.transform.position = GetSpawnPosition();

            Camera worldCamera = CreateWorldCamera(root.transform, renderTexture, displayLayer);
            GameObject television = CreateTelevision(
                root.transform,
                screenMaterial,
                bodyMaterial,
                displayLayer);

            worldCamera.transform.localPosition = new Vector3(0f, 2f, -5f);
            worldCamera.transform.localRotation = Quaternion.identity;
            television.transform.localPosition = new Vector3(0f, 2f, 0f);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            AssetDatabase.SaveAssets();
        }

        private static Camera CreateWorldCamera(
            Transform parent,
            RenderTexture renderTexture,
            int displayLayer)
        {
            GameObject cameraObject = new GameObject("World Camera", typeof(Camera));
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create World Camera");
            cameraObject.transform.SetParent(parent, false);

            Camera worldCamera = cameraObject.GetComponent<Camera>();
            worldCamera.targetTexture = renderTexture;
            worldCamera.fieldOfView = 60f;
            worldCamera.nearClipPlane = 0.05f;
            worldCamera.farClipPlane = 1000f;
            worldCamera.allowHDR = false;
            worldCamera.allowMSAA = false;
            worldCamera.useOcclusionCulling = true;
            worldCamera.stereoTargetEye = StereoTargetEyeMask.None;

            if (displayLayer >= 0)
            {
                worldCamera.cullingMask &= ~(1 << displayLayer);
            }

            return worldCamera;
        }

        private static GameObject CreateTelevision(
            Transform parent,
            Material screenMaterial,
            Material bodyMaterial,
            int displayLayer)
        {
            GameObject television = new GameObject("TV");
            Undo.RegisterCreatedObjectUndo(television, "Create TV");
            television.transform.SetParent(parent, false);

            GameObject body = CreateCube(
                television.transform,
                "TV Body",
                Vector3.zero,
                new Vector3(3.9f, 2.25f, 0.16f),
                bodyMaterial);

            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(screen, "Create TV Screen");
            screen.name = "TV Screen";
            screen.transform.SetParent(television.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0f, -0.086f);
            screen.transform.localScale = new Vector3(3.6f, 2.025f, 1f);
            screen.GetComponent<MeshRenderer>().sharedMaterial = screenMaterial;

            Collider screenCollider = screen.GetComponent<Collider>();
            if (screenCollider != null)
            {
                Undo.DestroyObjectImmediate(screenCollider);
            }

            CreateCube(
                television.transform,
                "TV Stand",
                new Vector3(0f, -1.38f, 0.02f),
                new Vector3(0.25f, 0.52f, 0.2f),
                bodyMaterial);
            CreateCube(
                television.transform,
                "TV Base",
                new Vector3(0f, -1.68f, 0.02f),
                new Vector3(1.45f, 0.12f, 0.72f),
                bodyMaterial);

            SetLayerRecursively(television, displayLayer);
            EditorUtility.SetDirty(body);
            return television;
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(cube, "Create " + name);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;

            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static RenderTexture CreateRenderTexture()
        {
            RenderTexture renderTexture = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32)
            {
                name = "World Camera Feed",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };

            string path = AssetDatabase.GenerateUniqueAssetPath(
                AssetFolder + "/World Camera Feed.renderTexture");
            AssetDatabase.CreateAsset(renderTexture, path);
            return renderTexture;
        }

        private static Material CreateScreenMaterial(RenderTexture renderTexture)
        {
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
            {
                name = "World Camera Feed Material",
                mainTexture = renderTexture
            };

            string path = AssetDatabase.GenerateUniqueAssetPath(
                AssetFolder + "/World Camera Feed Material.mat");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateBodyMaterial()
        {
            Material material = new Material(Shader.Find("Standard"))
            {
                name = "Camera TV Body Material",
                color = new Color(0.04f, 0.045f, 0.05f, 1f)
            };

            string path = AssetDatabase.GenerateUniqueAssetPath(
                AssetFolder + "/Camera TV Body Material.mat");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static void EnsureAssetFolder()
        {
            if (!AssetDatabase.IsValidFolder(AssetFolder))
            {
                AssetDatabase.CreateFolder("Assets/CheeseSportDay", "TV");
            }
        }

        private static int GetOrCreateDisplayLayer()
        {
            int existingLayer = LayerMask.NameToLayer(DisplayLayerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
            {
                return 2;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int layerIndex = 8; layerIndex < 32; layerIndex++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(layerIndex);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = DisplayLayerName;
                tagManager.ApplyModifiedProperties();
                return layerIndex;
            }

            Debug.LogWarning(
                "No empty user layer was available. The TV uses Ignore Raycast for camera exclusion.");
            return 2;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null || layer < 0)
            {
                return;
            }

            target.layer = layer;
            for (int i = 0; i < target.transform.childCount; i++)
            {
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
            }
        }

        private static Vector3 GetSpawnPosition()
        {
            if (Selection.activeTransform != null)
            {
                return Selection.activeTransform.position;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            return sceneView == null ? Vector3.zero : sceneView.pivot;
        }
    }
}
#endif
