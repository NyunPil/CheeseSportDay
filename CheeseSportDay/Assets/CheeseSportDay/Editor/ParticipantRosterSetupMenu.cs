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
        private const float CardWidth = 112f;
        private const float CardHeight = 90f;
        private const string CardPrefabPath = "Assets/CheeseSportDay/Prefab/Participant Card.prefab";

        [MenuItem("Cheese Sport Day/Participant Roster/Create or Select Card Prefab")]
        public static void CreateOrSelectCardPrefab()
        {
            GameObject cardPrefab = GetOrCreateCardPrefab();
            if (cardPrefab == null)
            {
                return;
            }

            Selection.activeObject = cardPrefab;
            EditorGUIUtility.PingObject(cardPrefab);
        }

        [MenuItem("Cheese Sport Day/Participant Roster/Collect Cards On Selected Screen")]
        public static void CollectCardsOnSelectedScreen()
        {
            GameObject selectedObject = Selection.activeGameObject;
            ParticipantRosterScreen roster = selectedObject == null
                ? null
                : selectedObject.GetComponentInParent<ParticipantRosterScreen>();

            if (roster == null)
            {
                EditorUtility.DisplayDialog(
                    "Participant Roster",
                    "Select the Participant Roster Screen or one of its children first.",
                    "OK");
                return;
            }

            GameObject cardPrefab = GetOrCreateCardPrefab();
            if (!ValidateCardPrefab(cardPrefab))
            {
                return;
            }

            ParticipantCard[] participantCards = roster.GetComponentsInChildren<ParticipantCard>(true);
            if (participantCards.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Participant Roster",
                    "No ParticipantCard components were found under the selected screen.",
                    "OK");
                return;
            }

            Undo.RecordObject(roster, "Collect Participant Cards");
            roster.participantCards = participantCards;
            EditorUtility.SetDirty(roster);
            PrefabUtility.RecordPrefabInstancePropertyModifications(roster);

            for (int i = 0; i < participantCards.Length; i++)
            {
                ParticipantCard participantCard = participantCards[i];
                Undo.RecordObject(participantCard, "Bind Participant Card");
                participantCard.rosterScreen = roster;
                participantCard.participantIndex = i;
                EditorUtility.SetDirty(participantCard);
                PrefabUtility.RecordPrefabInstancePropertyModifications(participantCard);
            }

            roster.RefreshAll();
            EditorUtility.DisplayDialog(
                "Participant Roster",
                "Collected " + participantCards.Length.ToString() + " participant cards.",
                "OK");
        }


        private static GameObject GetOrCreateCardPrefab()
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            if (existingPrefab != null)
            {
                ConfigureExistingCardPrefab();
                return AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            }

            if (!AssetDatabase.IsValidFolder("Assets/CheeseSportDay/Prefab"))
            {
                AssetDatabase.CreateFolder("Assets/CheeseSportDay", "Prefab");
            }

            GameObject card = new GameObject("Participant Card", typeof(RectTransform), typeof(Image));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

            Image background = card.GetComponent<Image>();
            background.color = new Color(0.92f, 0.92f, 0.92f, 1f);

            BoxCollider collider = card.AddComponent<BoxCollider>();
            collider.size = new Vector3(CardWidth, CardHeight, 12f);

            ParticipantCard participantCard = card.AddComponent<ParticipantCard>();

            Image portrait = CreateImage(
                cardRect,
                "Portrait",
                new Vector2(0f, 10f),
                new Vector2(CardWidth - 18f, 56f),
                Color.white,
                false).GetComponent<Image>();
            portrait.preserveAspect = true;

            Text nameText = CreateText(
                cardRect,
                "Name",
                "Participant",
                16,
                new Vector2(0f, -34f),
                new Vector2(CardWidth - 12f, 22f),
                Color.black,
                TextAnchor.MiddleCenter,
                false);

            participantCard.backgroundImage = background;
            participantCard.portraitImage = portrait;
            participantCard.nameText = nameText;

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(card, CardPrefabPath);
            UnityEngine.Object.DestroyImmediate(card);
            AssetDatabase.SaveAssets();

            if (savedPrefab == null)
            {
                Debug.LogError("Failed to create participant card prefab at " + CardPrefabPath);
            }

            return savedPrefab;
        }

        private static void ConfigureExistingCardPrefab()
        {
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                ParticipantCard participantCard = prefabContents.GetComponent<ParticipantCard>();
                if (participantCard == null)
                {
                    return;
                }

                if (participantCard.backgroundImage == null)
                {
                    participantCard.backgroundImage = prefabContents.GetComponent<Image>();
                }

                if (participantCard.portraitImage == null)
                {
                    Transform portrait = prefabContents.transform.Find("Portrait");
                    if (portrait != null)
                    {
                        participantCard.portraitImage = portrait.GetComponent<Image>();
                    }
                }

                if (participantCard.nameText == null)
                {
                    Transform name = prefabContents.transform.Find("Name");
                    if (name != null)
                    {
                        participantCard.nameText = name.GetComponent<Text>();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(prefabContents, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static bool ValidateCardPrefab(GameObject cardPrefab)
        {
            ParticipantCard participantCard = cardPrefab == null ? null : cardPrefab.GetComponent<ParticipantCard>();
            bool isValid = cardPrefab != null
                && cardPrefab.GetComponent<RectTransform>() != null
                && cardPrefab.GetComponent<BoxCollider>() != null
                && participantCard != null
                && participantCard.backgroundImage != null
                && participantCard.portraitImage != null
                && participantCard.nameText != null;

            if (!isValid)
            {
                EditorUtility.DisplayDialog(
                    "Invalid Participant Card Prefab",
                    "The prefab must contain a BoxCollider and ParticipantCard with Background, Portrait, and Name references.",
                    "OK");
            }

            return isValid;
        }

        private static void CreateCard(GameObject cardPrefab, RectTransform parent, ParticipantRosterScreen roster, int index, Vector2 position, ParticipantCard[] participantCards)
        {
            GameObject cardObject = PrefabUtility.InstantiatePrefab(cardPrefab, parent) as GameObject;
            Undo.RegisterCreatedObjectUndo(cardObject, "Create Participant Card");

            cardObject.name = "Participant Card " + (index + 1).ToString("00");

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = position;
            cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

            BoxCollider collider = cardObject.GetComponent<BoxCollider>();
            collider.size = new Vector3(CardWidth, CardHeight, 12f);

            ParticipantCard participantCard = cardObject.GetComponent<ParticipantCard>();
            participantCard.rosterScreen = roster;
            participantCard.participantIndex = index;
            participantCards[index] = participantCard;
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

        private static GameObject CreateImage(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, bool registerUndo = true)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(imageObject, "Create Roster UI");
            }

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

        private static Text CreateText(RectTransform parent, string name, string value, int size, Vector2 position, Vector2 textSize, Color color, TextAnchor alignment, bool registerUndo = true)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(textObject, "Create Roster Text");
            }

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
