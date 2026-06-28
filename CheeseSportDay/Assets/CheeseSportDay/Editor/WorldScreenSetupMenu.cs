#if UNITY_EDITOR
using CheeseSportDay.WorldUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseSportDay.Editor
{
    public static class WorldScreenSetupMenu
    {
        private const float CanvasScale = 0.005f;

        [MenuItem("Cheese Sport Day/Create World Screen Prototype")]
        public static void CreateWorldScreenPrototype()
        {
            GameObject root = new GameObject("World Screen Prototype");
            Undo.RegisterCreatedObjectUndo(root, "Create World Screen Prototype");

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(frame, "Create Screen Frame");
            frame.name = "Screen Frame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0f, 2.1f, 4f);
            frame.transform.localScale = new Vector3(4.2f, 2.45f, 0.08f);

            GameObject canvasObject = new GameObject("World Screen Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create World Screen Canvas");
            canvasObject.transform.SetParent(root.transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, 2.1f, 3.94f);
            canvasObject.transform.localScale = Vector3.one * CanvasScale;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800f, 450f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            GameObject idleView = CreatePanel(canvasRect, "Idle View", new Color(0.07f, 0.08f, 0.1f, 1f));
            CreateText(idleView.GetComponent<RectTransform>(), "Idle Title", "Cheese Sport Day", 44, new Vector2(0f, 70f));
            CreateText(idleView.GetComponent<RectTransform>(), "Idle Hint", "Press the world button to open the team UI.", 24, new Vector2(0f, 10f));

            GameObject activeView = CreatePanel(canvasRect, "Team UI View", new Color(0.1f, 0.12f, 0.14f, 1f));
            activeView.SetActive(false);
            CreateText(activeView.GetComponent<RectTransform>(), "Team UI Title", "Team Setup", 42, new Vector2(0f, 115f));
            CreateText(activeView.GetComponent<RectTransform>(), "Team UI Body", "This is the first screen UI state.", 26, new Vector2(0f, 45f));
            CreateText(activeView.GetComponent<RectTransform>(), "Team A Placeholder", "Team A", 32, new Vector2(-180f, -70f));
            CreateText(activeView.GetComponent<RectTransform>(), "Team B Placeholder", "Team B", 32, new Vector2(180f, -70f));

            ScreenPanelController controller = root.AddComponent<ScreenPanelController>();
            controller.idleView = idleView;
            controller.activeView = activeView;
            controller.showActiveViewOnStart = false;
            controller.syncForEveryone = true;

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(button, "Create Screen Open Button");
            button.name = "Open Screen Button";
            button.transform.SetParent(root.transform, false);
            button.transform.localPosition = new Vector3(0f, 0.85f, 3.35f);
            button.transform.localScale = new Vector3(0.55f, 0.18f, 0.32f);

            WorldScreenButton screenButton = button.AddComponent<WorldScreenButton>();
            screenButton.screenController = controller;

            GameObject label = new GameObject("Button Label", typeof(TextMesh));
            Undo.RegisterCreatedObjectUndo(label, "Create Button Label");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.1f, 3.35f);

            TextMesh labelText = label.GetComponent<TextMesh>();
            labelText.text = "OPEN";
            labelText.anchor = TextAnchor.MiddleCenter;
            labelText.alignment = TextAlignment.Center;
            labelText.characterSize = 0.12f;
            labelText.color = Color.white;

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        private static GameObject CreatePanel(RectTransform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panel, "Create Screen Panel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = color;

            return panel;
        }

        private static Text CreateText(RectTransform parent, string name, string value, int size, Vector2 anchoredPosition)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(textObject, "Create Screen Text");
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(680f, 70f);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return text;
        }
    }
}
#endif
