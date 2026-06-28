#if UNITY_EDITOR
using CheeseSportDay.WorldUI;
using UdonSharp;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseSportDay.Editor
{
    public static class ParticipantTeamSetupMenu
    {
        private const int MemberSlotsPerTeam = 16;
        private const string TeamBoardScriptPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamBoardScreen.cs";
        private const string TeamBoardAssetPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamBoardScreen.asset";
        private const string TeamButtonScriptPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamSelectButton.cs";
        private const string TeamButtonAssetPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamSelectButton.asset";
        private const string TeamColumnScriptPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamColumn.cs";
        private const string TeamColumnAssetPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamColumn.asset";

        [InitializeOnLoadMethod]
        private static void QueueProgramAssetSetup()
        {
            EditorApplication.delayCall += EnsureTeamProgramAssets;
        }

        [MenuItem("Cheese Sport Day/Participant Roster/Add Team Assignment To Selected Screen")]
        public static void AddTeamAssignmentToSelectedScreen()
        {
            GameObject selectedObject = Selection.activeGameObject;
            ParticipantRosterScreen roster = selectedObject == null
                ? null
                : selectedObject.GetComponentInParent<ParticipantRosterScreen>();

            if (roster == null)
            {
                EditorUtility.DisplayDialog(
                    "Participant Teams",
                    "Select the Participant Roster Screen or one of its children first.",
                    "OK");
                return;
            }

            ConfigureForRoster(roster);
            Selection.activeGameObject = roster.gameObject;
            EditorGUIUtility.PingObject(roster.gameObject);
        }

        public static void EnsureTeamProgramAssets()
        {
            bool created = false;
            created |= CreateProgramAssetIfMissing(TeamBoardScriptPath, TeamBoardAssetPath);
            created |= CreateProgramAssetIfMissing(TeamButtonScriptPath, TeamButtonAssetPath);
            created |= CreateProgramAssetIfMissing(TeamColumnScriptPath, TeamColumnAssetPath);

            if (created)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            bool needsCompile = created
                || NeedsCompile(TeamBoardAssetPath)
                || NeedsCompile(TeamButtonAssetPath)
                || NeedsCompile(TeamColumnAssetPath);

            if (needsCompile)
            {
                UdonSharpCompilerV1.CompileSync();
                AssetDatabase.SaveAssets();
            }
        }

        private static bool NeedsCompile(string assetPath)
        {
            UdonSharpProgramAsset programAsset = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(assetPath);
            if (programAsset == null)
            {
                return false;
            }

            SerializedObject serializedAsset = new SerializedObject(programAsset);
            SerializedProperty serializedProgram = serializedAsset.FindProperty("serializedUdonProgramAsset");
            return serializedProgram == null || serializedProgram.objectReferenceValue == null;
        }

        public static void ConfigureForRoster(ParticipantRosterScreen roster)
        {
            if (roster == null)
            {
                return;
            }

            EnsureTeamProgramAssets();

            if (roster.detailRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Participant Teams",
                    "The selected roster does not have a Detail Root assigned.",
                    "OK");
                return;
            }

            Undo.RecordObject(roster, "Configure Participant Teams");
            roster.selectFirstParticipantOnStart = false;

            if (roster.teamBoardScreen == null)
            {
                roster.teamBoardScreen = CreateTeamBoardScreen(roster);
            }

            ParticipantTeamBoardScreen board = roster.teamBoardScreen;
            Undo.RecordObject(board, "Configure Participant Team Board");
            board.rosterScreen = roster;
            EnsureDefaultTeamData(board);

            RebuildTeamPresentation(roster);
        }

        public static void RebuildTeamPresentation(ParticipantRosterScreen roster)
        {
            if (roster == null || roster.detailRoot == null || roster.teamBoardScreen == null)
            {
                return;
            }

            EnsureTeamProgramAssets();

            Undo.RecordObject(roster, "Rebuild Participant Team UI");
            Undo.RecordObject(roster.teamBoardScreen, "Rebuild Participant Team Board");

            RebuildTeamBoardColumns(roster.teamBoardScreen);
            roster.teamBoardScreen.rosterScreen = roster;
            roster.teamBoardScreen.RefreshAllViews();

            EditorUtility.SetDirty(roster);
            EditorUtility.SetDirty(roster.teamBoardScreen);
            PrefabUtility.RecordPrefabInstancePropertyModifications(roster);
            PrefabUtility.RecordPrefabInstancePropertyModifications(roster.teamBoardScreen);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureDefaultTeamData(ParticipantTeamBoardScreen board)
        {
            if (board.GetTeamCount() > 0)
            {
                return;
            }

            board.teamNames = new[] { "레드 팀", "블루 팀" };
            board.teamCaptainNames = new[] { "레드 팀장", "블루 팀장" };
            board.teamCaptainPortraits = new Sprite[2];
            board.teamColors = new[]
            {
                new Color(0.82f, 0.18f, 0.18f, 1f),
                new Color(0.15f, 0.4f, 0.85f, 1f)
            };
        }

        private static bool CreateProgramAssetIfMissing(string scriptPath, string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(assetPath) != null)
            {
                return false;
            }

            MonoScript sourceScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (sourceScript == null)
            {
                Debug.LogError("Unable to find UdonSharp source script at " + scriptPath);
                return false;
            }

            UdonSharpProgramAsset programAsset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            programAsset.sourceCsScript = sourceScript;
            AssetDatabase.CreateAsset(programAsset, assetPath);
            return true;
        }

        private static Text CreateOrConfigureTeamControls(ParticipantRosterScreen roster)
        {
            RectTransform detailRect = roster.detailRoot.GetComponent<RectTransform>();
            Transform existing = detailRect.Find("Team Assignment");
            GameObject controls;

            if (existing == null)
            {
                controls = new GameObject("Team Assignment", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(controls, "Create Team Assignment Controls");
                controls.transform.SetParent(detailRect, false);
            }
            else
            {
                controls = existing.gameObject;
            }

            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
            controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
            controlsRect.anchoredPosition = new Vector2(0f, -195f);

            for (int i = controlsRect.childCount - 1; i >= 0; i--)
            {
                Transform child = controlsRect.GetChild(i);
                if (child.name != "Current Team")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            ParticipantTeamBoardScreen board = roster.teamBoardScreen;
            int teamCount = board.GetTeamCount();
            int buttonCount = teamCount + 1;
            int rowCount = Mathf.Max(1, (buttonCount + 2) / 3);
            controlsRect.sizeDelta = new Vector2(300f, 55f + rowCount * 40f);

            Text currentTeamText = GetOrCreateText(
                controlsRect,
                "Current Team",
                "팀: 미배정",
                18,
                new Vector2(0f, 38f),
                new Vector2(280f, 28f),
                new Color(0.08f, 0.08f, 0.08f, 1f),
                TextAnchor.MiddleCenter);

            for (int teamIndex = 0; teamIndex < teamCount; teamIndex++)
            {
                Vector2 position = GetButtonPosition(teamIndex, buttonCount);
                CreateTeamButton(
                    controlsRect,
                    roster,
                    "Assign Team " + (teamIndex + 1).ToString(),
                    board.GetTeamButtonLabel(teamIndex),
                    teamIndex,
                    position,
                    board.GetTeamColor(teamIndex));
            }

            Vector2 clearPosition = GetButtonPosition(teamCount, buttonCount);
            CreateTeamButton(
                controlsRect,
                roster,
                "Clear Team",
                "배정 해제",
                -1,
                clearPosition,
                new Color(0.28f, 0.3f, 0.34f, 1f));

            return currentTeamText;
        }

        private static Vector2 GetButtonPosition(int buttonIndex, int buttonCount)
        {
            int row = buttonIndex / 3;
            int firstIndexInRow = row * 3;
            int buttonsInRow = Mathf.Min(3, buttonCount - firstIndexInRow);
            int column = buttonIndex - firstIndexInRow;
            float x = (column - (buttonsInRow - 1) * 0.5f) * 96f;
            return new Vector2(x, -5f - row * 40f);
        }

        private static void CreateTeamButton(RectTransform parent, ParticipantRosterScreen roster, string name, string label, int teamIndex, Vector2 position, Color color)
        {
            GameObject buttonObject = CreateImage(parent, name, position, new Vector2(88f, 34f), color);
            BoxCollider collider = buttonObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(88f, 34f, 12f);

            ParticipantTeamSelectButton button = buttonObject.AddComponent<ParticipantTeamSelectButton>();
            button.rosterScreen = roster;
            button.teamIndex = teamIndex;

            Color textColor = GetReadableTextColor(color);
            GetOrCreateText(
                buttonObject.GetComponent<RectTransform>(),
                "Label",
                label,
                15,
                Vector2.zero,
                new Vector2(82f, 30f),
                textColor,
                TextAnchor.MiddleCenter);
        }

        private static ParticipantTeamBoardScreen CreateTeamBoardScreen(ParticipantRosterScreen roster)
        {
            GameObject root = GameObject.Find("Participant Team Board Screen");
            if (root != null && root.GetComponent<ParticipantTeamBoardScreen>() != null)
            {
                root = null;
            }

            if (root == null)
            {
                root = new GameObject("Participant Team Board Screen");
                Undo.RegisterCreatedObjectUndo(root, "Create Participant Team Board Screen");
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = root.AddComponent<RectTransform>();
            }
            rootRect.sizeDelta = new Vector2(700f, 600f);
            root.transform.position = roster.transform.position + roster.transform.right * 5.8f;
            root.transform.rotation = roster.transform.rotation;
            root.transform.localScale = roster.transform.lossyScale;

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = root.AddComponent<CanvasScaler>();
            }
            scaler.dynamicPixelsPerUnit = 10f;

            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.AddComponent<GraphicRaycaster>();
            }

            GetOrCreateImage(rootRect, "Background", Vector2.zero, new Vector2(700f, 600f), new Color(0.07f, 0.08f, 0.1f, 1f));
            GetOrCreateText(rootRect, "Title", "팀 현황", 34, new Vector2(0f, 255f), new Vector2(640f, 55f), Color.white, TextAnchor.MiddleCenter);

            ParticipantTeamBoardScreen board = root.GetComponent<ParticipantTeamBoardScreen>();
            if (board == null)
            {
                board = root.AddComponent<ParticipantTeamBoardScreen>();
            }
            board.rosterScreen = roster;
            board.teamNames = new[] { "레드 팀", "블루 팀" };
            board.teamCaptainNames = new[] { "레드 팀장", "블루 팀장" };
            board.teamCaptainPortraits = new Sprite[2];
            board.teamColors = new[]
            {
                new Color(0.82f, 0.18f, 0.18f, 1f),
                new Color(0.15f, 0.4f, 0.85f, 1f)
            };
            return board;
        }

        private static void RebuildTeamBoardColumns(ParticipantTeamBoardScreen board)
        {
            RectTransform rootRect = board.GetComponent<RectTransform>();
            int teamCount = board.GetTeamCount();
            float boardWidth = Mathf.Max(700f, teamCount * 320f + 40f);
            rootRect.sizeDelta = new Vector2(boardWidth, 600f);

            Transform oldRedColumn = rootRect.Find("레드 팀");
            if (oldRedColumn != null)
            {
                Undo.DestroyObjectImmediate(oldRedColumn.gameObject);
            }

            Transform oldBlueColumn = rootRect.Find("블루 팀");
            if (oldBlueColumn != null)
            {
                Undo.DestroyObjectImmediate(oldBlueColumn.gameObject);
            }

            Image background = GetOrCreateImage(
                rootRect,
                "Background",
                Vector2.zero,
                new Vector2(boardWidth, 600f),
                new Color(0.07f, 0.08f, 0.1f, 1f));
            background.rectTransform.sizeDelta = new Vector2(boardWidth, 600f);

            Text title = GetOrCreateText(
                rootRect,
                "Title",
                "팀 현황",
                34,
                new Vector2(0f, 255f),
                new Vector2(boardWidth - 60f, 55f),
                Color.white,
                TextAnchor.MiddleCenter);
            title.rectTransform.sizeDelta = new Vector2(boardWidth - 60f, 55f);

            Transform existingColumns = rootRect.Find("Team Columns");
            GameObject columnsObject;
            if (existingColumns == null)
            {
                columnsObject = new GameObject("Team Columns", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(columnsObject, "Create Team Columns");
                columnsObject.transform.SetParent(rootRect, false);
            }
            else
            {
                columnsObject = existingColumns.gameObject;
            }

            RectTransform columnsRect = columnsObject.GetComponent<RectTransform>();
            columnsRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnsRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnsRect.anchoredPosition = new Vector2(0f, -10f);
            columnsRect.sizeDelta = new Vector2(boardWidth - 20f, 470f);

            for (int i = columnsRect.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(columnsRect.GetChild(i).gameObject);
            }

            ParticipantTeamColumn[] columns = new ParticipantTeamColumn[teamCount];
            for (int teamIndex = 0; teamIndex < teamCount; teamIndex++)
            {
                float x = (teamIndex - (teamCount - 1) * 0.5f) * 320f;
                columns[teamIndex] = CreateTeamColumn(
                    columnsRect,
                    board.GetTeamName(teamIndex),
                    new Vector2(x, 0f),
                    board.GetTeamColor(teamIndex));
            }

            board.teamColumns = columns;
            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(title);
        }

        private static ParticipantTeamColumn CreateTeamColumn(RectTransform parent, string teamName, Vector2 position, Color teamColor)
        {
            Color panelColor = Color.Lerp(teamColor, Color.black, 0.5f);
            panelColor.a = 1f;

            GameObject columnObject = CreateImage(parent, teamName, position, new Vector2(300f, 460f), panelColor);
            RectTransform columnRect = columnObject.GetComponent<RectTransform>();

            Image header = CreateImage(
                columnRect,
                "Team Color",
                new Vector2(0f, 205f),
                new Vector2(300f, 50f),
                teamColor).GetComponent<Image>();
            Text teamNameText = GetOrCreateText(
                header.rectTransform,
                "Team Name",
                teamName,
                26,
                Vector2.zero,
                new Vector2(270f, 44f),
                GetReadableTextColor(teamColor),
                TextAnchor.MiddleCenter);

            GameObject[] memberRoots = new GameObject[MemberSlotsPerTeam];
            Image[] memberPortraitImages = new Image[MemberSlotsPerTeam];
            Text[] memberNameTexts = new Text[MemberSlotsPerTeam];

            for (int slotIndex = 0; slotIndex < MemberSlotsPerTeam; slotIndex++)
            {
                int gridColumn = slotIndex % 2;
                int gridRow = slotIndex / 2;
                float x = gridColumn == 0 ? -70f : 70f;
                float y = 152f - gridRow * 47f;
                GameObject memberRoot = CreateImage(
                    columnRect,
                    "Member " + (slotIndex + 1).ToString("00"),
                    new Vector2(x, y),
                    new Vector2(132f, 42f),
                    new Color(1f, 1f, 1f, 0.12f));
                RectTransform memberRect = memberRoot.GetComponent<RectTransform>();

                Image portrait = CreateImage(
                    memberRect,
                    "Portrait",
                    new Vector2(-47f, 0f),
                    new Vector2(34f, 34f),
                    Color.white).GetComponent<Image>();
                portrait.preserveAspect = true;

                Text memberName = GetOrCreateText(
                    memberRect,
                    "Name",
                    "",
                    16,
                    new Vector2(20f, 0f),
                    new Vector2(92f, 34f),
                    Color.white,
                    TextAnchor.MiddleLeft);

                memberRoots[slotIndex] = memberRoot;
                memberPortraitImages[slotIndex] = portrait;
                memberNameTexts[slotIndex] = memberName;
            }

            ParticipantTeamColumn column = columnObject.AddComponent<ParticipantTeamColumn>();
            column.teamColorImage = header;
            column.teamNameText = teamNameText;
            return column;
        }

        private static GameObject CreateImage(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(imageObject, "Create Participant Team UI");
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

        private static Image GetOrCreateImage(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Image existingImage = existing.GetComponent<Image>();
                existingImage.color = color;
                return existingImage;
            }

            return CreateImage(parent, name, position, size, color).GetComponent<Image>();
        }

        private static Text GetOrCreateText(RectTransform parent, string name, string value, int fontSize, Vector2 position, Vector2 size, Color color, TextAnchor alignment)
        {
            Transform existing = parent.Find(name);
            Text text;

            if (existing == null)
            {
                GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
                Undo.RegisterCreatedObjectUndo(textObject, "Create Participant Team Text");
                textObject.transform.SetParent(parent, false);

                RectTransform rect = textObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                text = textObject.GetComponent<Text>();
            }
            else
            {
                text = existing.GetComponent<Text>();
            }

            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static Color GetReadableTextColor(Color background)
        {
            float luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
            return luminance > 0.62f ? Color.black : Color.white;
        }
    }
}
#endif