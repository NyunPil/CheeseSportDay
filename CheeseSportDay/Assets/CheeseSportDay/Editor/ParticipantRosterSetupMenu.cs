#if UNITY_EDITOR
using CheeseSportDay.WorldUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseSportDay.Editor
{
    public static class ParticipantRosterSetupMenu
    {
        private const int Columns = 5;
        private const int Rows = 5;
        private const int CardCount = Columns * Rows;
        private const float CanvasScale = 0.005f;

        [MenuItem("Cheese Sport Day/Participant Roster/Create Roster Screen")]
        public static void CreateRosterScreen()
        {
            GameObject root = new GameObject("Participant Roster Screen");
            Undo.RegisterCreatedObjectUndo(root, "Create Participant Roster Screen");
            root.transform.localPosition = new Vector3(0f, 2.15f, 3.94f);
            root.transform.localScale = Vector3.one * CanvasScale;

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1050f, 600f);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            root.AddComponent<GraphicRaycaster>();
            CreateImage(rootRect, "Background", Vector2.zero, new Vector2(1050f, 600f), new Color(0.08f, 0.09f, 0.1f, 1f));

            ParticipantRosterScreen roster = root.AddComponent<ParticipantRosterScreen>();
            roster.gameSkillLabel = "게임실력";
            roster.gameSenseLabel = "게임센스";
            roster.teamworkLabel = "협동력";
            roster.physicalLabel = "피지컬";
            roster.luckLabel = "운";

            ParticipantCardButton[] cardButtons = new ParticipantCardButton[CardCount];
            GameObject[] cardRoots = new GameObject[CardCount];
            Image[] cardBackgrounds = new Image[CardCount];
            Image[] cardPortraits = new Image[CardCount];
            Text[] cardNames = new Text[CardCount];

            float cardWidth = 112f;
            float cardHeight = 90f;
            float spacingX = 10f;
            float spacingY = 10f;
            float gridStartX = -435f;
            float gridStartY = 210f;

            for (int i = 0; i < CardCount; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                Vector2 position = new Vector2(
                    gridStartX + column * (cardWidth + spacingX),
                    gridStartY - row * (cardHeight + spacingY));

                CreateCard(rootRect, roster, i, position, new Vector2(cardWidth, cardHeight), cardButtons, cardRoots, cardBackgrounds, cardPortraits, cardNames);
            }

            GameObject detailRoot = CreateImage(rootRect, "Detail Panel", new Vector2(330f, 20f), new Vector2(330f, 500f), new Color(0.92f, 0.92f, 0.9f, 1f));
            RectTransform detailRect = detailRoot.GetComponent<RectTransform>();

            Text titleText = CreateText(detailRect, "Detail Title", "", 36, new Vector2(0f, 205f), new Vector2(280f, 50f), Color.black, TextAnchor.MiddleCenter);
            Image portrait = CreateImage(detailRect, "Detail Portrait", new Vector2(0f, 70f), new Vector2(220f, 220f), Color.white).GetComponent<Image>();
            Text nameText = CreateText(detailRect, "Detail Name", "", 30, new Vector2(0f, -55f), new Vector2(280f, 40f), Color.black, TextAnchor.MiddleCenter);
            Text bodyText = CreateText(detailRect, "Detail Body", "", 20, new Vector2(0f, -130f), new Vector2(280f, 90f), new Color(0.08f, 0.08f, 0.08f, 1f), TextAnchor.UpperLeft);

            Text skillText = CreateStatText(detailRect, "Game Skill", new Vector2(-250f, -260f), new Color(0.9f, 0.16f, 0.16f, 1f));
            Text senseText = CreateStatText(detailRect, "Game Sense", new Vector2(-125f, -260f), new Color(0.16f, 0.43f, 0.9f, 1f));
            Text teamworkText = CreateStatText(detailRect, "Teamwork", new Vector2(0f, -260f), new Color(0.2f, 0.7f, 0.26f, 1f));
            Text physicalText = CreateStatText(detailRect, "Physical", new Vector2(125f, -260f), new Color(0.55f, 0.22f, 0.85f, 1f));
            Text luckText = CreateStatText(detailRect, "Luck", new Vector2(250f, -260f), new Color(0.9f, 0.82f, 0.18f, 1f));

            Text pageText = CreateText(rootRect, "Page Text", "1 / 1", 20, new Vector2(-190f, -265f), new Vector2(110f, 35f), Color.white, TextAnchor.MiddleCenter);
            CreatePageButton(rootRect, roster, "Previous Page", "<", ParticipantRosterPageAction.Previous, new Vector2(-280f, -265f));
            CreatePageButton(rootRect, roster, "Next Page", ">", ParticipantRosterPageAction.Next, new Vector2(-100f, -265f));

            roster.cardButtons = cardButtons;
            roster.cardRoots = cardRoots;
            roster.cardBackgroundImages = cardBackgrounds;
            roster.cardPortraitImages = cardPortraits;
            roster.cardNameTexts = cardNames;
            roster.pageText = pageText;
            roster.detailRoot = detailRoot;
            roster.detailPortraitImage = portrait;
            roster.detailNameText = nameText;
            roster.detailTitleText = titleText;
            roster.detailBodyText = bodyText;
            roster.gameSkillText = skillText;
            roster.gameSenseText = senseText;
            roster.teamworkText = teamworkText;
            roster.physicalText = physicalText;
            roster.luckText = luckText;

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        private static void CreateCard(RectTransform parent, ParticipantRosterScreen roster, int index, Vector2 position, Vector2 size, ParticipantCardButton[] buttons, GameObject[] roots, Image[] backgrounds, Image[] portraits, Text[] names)
        {
            GameObject card = CreateImage(parent, "Participant Card " + (index + 1).ToString("00"), position, size, new Color(0.92f, 0.92f, 0.92f, 1f));
            BoxCollider collider = card.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, size.y, 12f);

            ParticipantCardButton button = card.AddComponent<ParticipantCardButton>();
            button.rosterScreen = roster;
            button.participantIndex = index;

            RectTransform cardRect = card.GetComponent<RectTransform>();
            Image portrait = CreateImage(cardRect, "Portrait", new Vector2(0f, 10f), new Vector2(size.x - 18f, 56f), Color.white).GetComponent<Image>();
            portrait.preserveAspect = true;
            Text name = CreateText(cardRect, "Name", "", 16, new Vector2(0f, -34f), new Vector2(size.x - 12f, 22f), Color.black, TextAnchor.MiddleCenter);

            buttons[index] = button;
            roots[index] = card;
            backgrounds[index] = card.GetComponent<Image>();
            portraits[index] = portrait;
            names[index] = name;
        }

        private static void CreatePageButton(RectTransform parent, ParticipantRosterScreen roster, string name, string label, ParticipantRosterPageAction action, Vector2 position)
        {
            GameObject buttonObject = CreateImage(parent, name, position, new Vector2(60f, 36f), new Color(0.18f, 0.2f, 0.24f, 1f));
            BoxCollider collider = buttonObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(60f, 36f, 12f);

            ParticipantRosterPageButton button = buttonObject.AddComponent<ParticipantRosterPageButton>();
            button.rosterScreen = roster;
            button.pageAction = action;

            CreateText(buttonObject.GetComponent<RectTransform>(), "Label", label, 24, Vector2.zero, new Vector2(60f, 36f), Color.white, TextAnchor.MiddleCenter);
        }

        private static Text CreateStatText(RectTransform parent, string name, Vector2 position, Color color)
        {
            GameObject background = CreateImage(parent, name + " Background", position, new Vector2(112f, 42f), color);
            return CreateText(background.GetComponent<RectTransform>(), name, "", 18, Vector2.zero, new Vector2(104f, 36f), Color.white, TextAnchor.MiddleCenter);
        }

        private static GameObject CreateImage(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(imageObject, "Create Roster UI");
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;

            return imageObject;
        }

        private static Text CreateText(RectTransform parent, string name, string value, int size, Vector2 position, Vector2 textSize, Color color, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(textObject, "Create Roster Text");
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = textSize;

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = size;

            return text;
        }
    }
}
#endif
