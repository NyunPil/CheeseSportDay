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
        private const string TeamBoardScriptPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamBoardScreen.cs";
        private const string TeamBoardAssetPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamBoardScreen.asset";
        private const string TeamButtonScriptPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamSelectButton.cs";
        private const string TeamButtonAssetPath = "Assets/CheeseSportDay/WorldUI/ParticipantTeamSelectButton.asset";

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

            if (created)
            {
                AssetDatabase.SaveAssets();
                UdonSharpCompilerV1.CompileSync();
            }
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
            roster.teamLabel = "팀";
            roster.unassignedTeamLabel = "미배정";
            roster.detailTeamText = CreateOrConfigureTeamControls(roster);

            if (roster.teamBoardScreen == null)
            {
                roster.teamBoardScreen = CreateTeamBoardScreen(roster);
            }
            else
            {
                Undo.RecordObject(roster.teamBoardScreen, "Configure Participant Team Board");
                roster.teamBoardScreen.rosterScreen = roster;
                EditorUtility.SetDirty(roster.teamBoardScreen);
                PrefabUtility.RecordPrefabInstancePropertyModifications(roster.teamBoardScreen);
            }

            EditorUtility.SetDirty(roster);
            PrefabUtility.RecordPrefabInstancePropertyModifications(roster);
            AssetDatabase.SaveAssets();
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

                RectTransform controlsRect = controls.GetComponent<RectTransform>();
                controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
                controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
                controlsRect.anchoredPosition = new Vector2(0f, -205f);
                controlsRect.sizeDelta = new Vector2(300f, 90f);
            }
            else
            {
                controls = existing.gameObject;
            }

            RectTransform parent = controls.GetComponent<RectTransform>();
            Text currentTeamText = GetOrCreateText(
                parent,
                "Current Team",
                "팀: 미배정",
                18,
                new Vector2(0f, 28f),
                new Vector2(280f, 28f),
                new Color(0.08f, 0.08f, 0.08f, 1f),
                TextAnchor.MiddleCenter);

            CreateOrConfigureTeamButton(parent, roster, "Assign Red Team", "레드 팀", 0, new Vector2(-96f, -15f), new Color(0.82f, 0.18f, 0.18f, 1f));
            CreateOrConfigureTeamButton(parent, roster, "Assign Blue Team", "블루 팀", 1, new Vector2(0f, -15f), new Color(0.15f, 0.4f, 0.85f, 1f));
            CreateOrConfigureTeamButton(parent, roster, "Clear Team", "배정 해제", -1, new Vector2(96f, -15f), new Color(0.28f, 0.3f, 0.34f, 1f));
            return currentTeamText;
        }

        private static void CreateOrConfigureTeamButton(RectTransform parent, ParticipantRosterScreen roster, string name, string label, int teamIndex, Vector2 position, Color color)
        {
            Transform existing = parent.Find(name);
            GameObject buttonObject;

            if (existing == null)
            {
                buttonObject = CreateImage(parent, name, position, new Vector2(88f, 34f), color);
                BoxCollider collider = buttonObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(88f, 34f, 12f);
                GetOrCreateText(buttonObject.GetComponent<RectTransform>(), "Label", label, 15, Vector2.zero, new Vector2(82f, 30f), Color.white, TextAnchor.MiddleCenter);
            }
            else
            {
                buttonObject = existing.gameObject;
            }

            ParticipantTeamSelectButton button = buttonObject.GetComponent<ParticipantTeamSelectButton>();
            if (button == null)
            {
                button = buttonObject.AddComponent<ParticipantTeamSelectButton>();
            }

            button.rosterScreen = roster;
            button.teamIndex = teamIndex;
            EditorUtility.SetDirty(button);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
        }

        private static ParticipantTeamBoardScreen CreateTeamBoardScreen(ParticipantRosterScreen roster)
        {
            GameObject root = new GameObject("Participant Team Board Screen");
            Undo.RegisterCreatedObjectUndo(root, "Create Participant Team Board Screen");

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(700f, 600f);
            root.transform.position = roster.transform.position + roster.transform.right * 5.8f;
            root.transform.rotation = roster.transform.rotation;
            root.transform.localScale = roster.transform.lossyScale;

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            root.AddComponent<GraphicRaycaster>();

            CreateImage(rootRect, "Background", Vector2.zero, new Vector2(700f, 600f), new Color(0.07f, 0.08f, 0.1f, 1f));
            GetOrCreateText(rootRect, "Title", "팀 현황", 34, new Vector2(0f, 255f), new Vector2(640f, 55f), Color.white, TextAnchor.MiddleCenter);

            Text[] teamNameTexts = new Text[2];
            Text[] teamMemberTexts = new Text[2];
            CreateTeamColumn(rootRect, 0, "레드 팀", new Vector2(-170f, -10f), new Color(0.42f, 0.09f, 0.09f, 1f), teamNameTexts, teamMemberTexts);
            CreateTeamColumn(rootRect, 1, "블루 팀", new Vector2(170f, -10f), new Color(0.07f, 0.18f, 0.42f, 1f), teamNameTexts, teamMemberTexts);

            ParticipantTeamBoardScreen board = root.AddComponent<ParticipantTeamBoardScreen>();
            board.rosterScreen = roster;
            board.teamNames = new[] { "레드 팀", "블루 팀" };
            board.teamNameTexts = teamNameTexts;
            board.teamMemberTexts = teamMemberTexts;
            board.emptyTeamText = "배정된 참가자가 없습니다.";
            return board;
        }

        private static void CreateTeamColumn(RectTransform parent, int index, string teamName, Vector2 position, Color color, Text[] nameTexts, Text[] memberTexts)
        {
            GameObject column = CreateImage(parent, teamName, position, new Vector2(300f, 460f), color);
            RectTransform columnRect = column.GetComponent<RectTransform>();
            nameTexts[index] = GetOrCreateText(columnRect, "Team Name", teamName, 28, new Vector2(0f, 190f), new Vector2(260f, 45f), Color.white, TextAnchor.MiddleCenter);
            memberTexts[index] = GetOrCreateText(columnRect, "Members", "배정된 참가자가 없습니다.", 22, new Vector2(0f, -20f), new Vector2(260f, 340f), Color.white, TextAnchor.UpperCenter);
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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
            return text;
        }
    }
}
#endif
