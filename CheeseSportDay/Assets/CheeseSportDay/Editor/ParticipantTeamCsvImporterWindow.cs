#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using CheeseSportDay.WorldUI;
using UnityEditor;
using UnityEngine;

namespace CheeseSportDay.Editor
{
    public class ParticipantTeamCsvImporterWindow : EditorWindow
    {
        private ParticipantRosterScreen targetScreen;
        private TextAsset csvAsset;
        private bool autoConvertTexturesToSprites = true;

        [MenuItem("Cheese Sport Day/Participant Roster/Import Team CSV")]
        public static void Open()
        {
            GetWindow<ParticipantTeamCsvImporterWindow>("Team CSV Import");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Participant Team CSV", EditorStyles.boldLabel);
            targetScreen = (ParticipantRosterScreen)EditorGUILayout.ObjectField(
                "Target Roster Screen",
                targetScreen,
                typeof(ParticipantRosterScreen),
                true);
            csvAsset = (TextAsset)EditorGUILayout.ObjectField(
                "Team CSV TextAsset",
                csvAsset,
                typeof(TextAsset),
                false);
            autoConvertTexturesToSprites = EditorGUILayout.Toggle(
                "Auto Convert Images",
                autoConvertTexturesToSprites);

            EditorGUILayout.HelpBox(
                "Recognized columns: teamName/팀명, captainName/팀장, image/팀장사진, teamColor/팀컬러. "
                + "Image values use Resources paths such as TeamCaptains/A. "
                + "Colors use HTML values such as #E63946.",
                MessageType.Info);

            EditorGUI.BeginDisabledGroup(targetScreen == null || csvAsset == null);
            if (GUILayout.Button("Import Team CSV"))
            {
                ImportCsv();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void ImportCsv()
        {
            ParticipantTeamSetupMenu.ConfigureForRoster(targetScreen);
            ParticipantTeamBoardScreen board = targetScreen.teamBoardScreen;
            if (board == null)
            {
                EditorUtility.DisplayDialog(
                    "Team CSV Import",
                    "Unable to create or find the Participant Team Board Screen.",
                    "OK");
                return;
            }

            List<string[]> rows = ParseCsv(csvAsset.text);
            if (rows.Count < 2)
            {
                EditorUtility.DisplayDialog(
                    "Team CSV Import",
                    "CSV must contain a header row and at least one team row.",
                    "OK");
                return;
            }

            string[] headers = rows[0];
            if (headers.Length > 0)
            {
                headers[0] = headers[0].Trim('\uFEFF');
            }

            int teamNameColumn = FindColumn(
                headers,
                "teamname",
                "team",
                "name",
                "팀명",
                "팀",
                "팀이름");
            int captainNameColumn = FindColumn(
                headers,
                "captainname",
                "captain",
                "leadername",
                "leader",
                "팀장",
                "팀장이름");
            int imageColumn = FindColumn(
                headers,
                "image",
                "imagepath",
                "portrait",
                "portraitpath",
                "captainimage",
                "leaderimage",
                "이미지",
                "사진",
                "팀장사진",
                "팀장이미지");
            int colorColumn = FindColumn(
                headers,
                "teamcolor",
                "color",
                "colour",
                "팀컬러",
                "팀색",
                "색상");

            if (captainNameColumn < 0)
            {
                EditorUtility.DisplayDialog(
                    "Team CSV Import",
                    "No captain name column found. Add captainName or 팀장.",
                    "OK");
                return;
            }

            List<string> teamNames = new List<string>();
            List<string> captainNames = new List<string>();
            List<Sprite> captainPortraits = new List<Sprite>();
            List<Color> teamColors = new List<Color>();
            List<string> warnings = new List<string>();

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] row = rows[rowIndex];
                string captainName = GetCell(row, captainNameColumn).Trim();
                if (string.IsNullOrEmpty(captainName))
                {
                    continue;
                }

                string teamName = GetCell(row, teamNameColumn).Trim();
                if (string.IsNullOrEmpty(teamName))
                {
                    teamName = captainName + " 팀";
                }

                teamNames.Add(teamName);
                captainNames.Add(captainName);
                captainPortraits.Add(LoadPortrait(GetCell(row, imageColumn), warnings));
                teamColors.Add(ParseColor(GetCell(row, colorColumn), rowIndex + 1, warnings));
            }

            if (captainNames.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Team CSV Import",
                    "No valid team rows were found.",
                    "OK");
                return;
            }

            Undo.RecordObject(board, "Import Participant Team CSV");
            board.rosterScreen = targetScreen;
            board.teamNames = teamNames.ToArray();
            board.teamCaptainNames = captainNames.ToArray();
            board.teamCaptainPortraits = captainPortraits.ToArray();
            board.teamColors = teamColors.ToArray();

            AssignCaptainNamesToButtons(captainNames, warnings);

            int participantCount = targetScreen.GetParticipantCount();
            board.participantTeamIndices = new int[participantCount];
            for (int i = 0; i < board.participantTeamIndices.Length; i++)
            {
                board.participantTeamIndices[i] = -1;
            }

            ParticipantTeamSetupMenu.RebuildTeamPresentation(targetScreen);
            board.RefreshAllViews();

            EditorUtility.SetDirty(board);
            EditorUtility.SetDirty(targetScreen);
            PrefabUtility.RecordPrefabInstancePropertyModifications(board);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetScreen);
            AssetDatabase.SaveAssets();

            string message = "Imported " + captainNames.Count + " teams.";
            if (warnings.Count > 0)
            {
                message += "\n\nWarnings:\n" + string.Join("\n", warnings.ToArray());
            }

            EditorUtility.DisplayDialog("Team CSV Import", message, "OK");
        }

        private void AssignCaptainNamesToButtons(List<string> captainNames, List<string> warnings)
        {
            WorldScreenButton[] buttons = targetScreen.teamButtons;
            int buttonCount = buttons == null ? 0 : buttons.Length;

            if (buttonCount < captainNames.Count)
            {
                warnings.Add(
                    "Only " + buttonCount.ToString()
                    + " team buttons are assigned for " + captainNames.Count.ToString()
                    + " captains.");
            }

            for (int i = 0; i < buttonCount; i++)
            {
                WorldScreenButton button = buttons[i];
                if (button == null)
                {
                    warnings.Add("Team button element " + i.ToString() + " is empty.");
                    continue;
                }

                Undo.RecordObject(button, "Assign Team Captain Name");
                button.captainName = i < captainNames.Count ? captainNames[i] : "";
                EditorUtility.SetDirty(button);
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            }
        }

        private Sprite LoadPortrait(string rawPath, List<string> warnings)
        {
            string resourcesPath = NormalizeResourcesPath(rawPath);
            if (string.IsNullOrEmpty(resourcesPath))
            {
                return null;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcesPath);
            if (sprite != null)
            {
                return sprite;
            }

            if (autoConvertTexturesToSprites)
            {
                string assetPath = FindResourceAssetPath(resourcesPath);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.SaveAndReimport();
                        sprite = Resources.Load<Sprite>(resourcesPath);
                    }
                }
            }

            if (sprite == null)
            {
                warnings.Add("Missing captain sprite: " + rawPath + " -> Resources/" + resourcesPath);
            }

            return sprite;
        }

        private static Color ParseColor(string value, int rowNumber, List<string> warnings)
        {
            string normalized = string.IsNullOrEmpty(value) ? "" : value.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                warnings.Add("Missing team color on row " + rowNumber.ToString() + ". Using gray.");
                return Color.gray;
            }

            if (!normalized.StartsWith("#", StringComparison.Ordinal))
            {
                normalized = "#" + normalized;
            }

            Color parsed;
            if (ColorUtility.TryParseHtmlString(normalized, out parsed))
            {
                parsed.a = 1f;
                return parsed;
            }

            warnings.Add("Invalid team color on row " + rowNumber.ToString() + ": " + value + ". Using gray.");
            return Color.gray;
        }

        private static string NormalizeResourcesPath(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            string path = value.Trim().Trim('"').Replace('\\', '/');
            int resourcesIndex = path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
            if (resourcesIndex >= 0)
            {
                path = path.Substring(resourcesIndex + "/Resources/".Length);
            }
            else if (path.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring("Resources/".Length);
            }

            return StripExtension(path);
        }

        private static string FindResourceAssetPath(string resourcesPath)
        {
            string fileName = resourcesPath;
            int slashIndex = fileName.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                fileName = fileName.Substring(slashIndex + 1);
            }

            string[] guids = AssetDatabase.FindAssets(fileName);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                int resourcesIndex = assetPath.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
                if (resourcesIndex < 0)
                {
                    continue;
                }

                string candidate = StripExtension(assetPath.Substring(resourcesIndex + "/Resources/".Length));
                if (string.Equals(candidate, resourcesPath, StringComparison.OrdinalIgnoreCase))
                {
                    return assetPath;
                }
            }

            return "";
        }

        private static string StripExtension(string path)
        {
            int slashIndex = path.LastIndexOf('/');
            int dotIndex = path.LastIndexOf('.');
            if (dotIndex > slashIndex)
            {
                return path.Substring(0, dotIndex);
            }

            return path;
        }

        private static int FindColumn(string[] headers, params string[] aliases)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                string normalizedHeader = NormalizeHeader(headers[i]);
                for (int j = 0; j < aliases.Length; j++)
                {
                    if (normalizedHeader == NormalizeHeader(aliases[j]))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static string NormalizeHeader(string value)
        {
            return string.IsNullOrEmpty(value)
                ? ""
                : value.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "").Replace("/", "");
        }

        private static string GetCell(string[] row, int column)
        {
            if (row == null || column < 0 || column >= row.Length || row[column] == null)
            {
                return "";
            }

            return row[column].Trim();
        }

        private static List<string[]> ParseCsv(string text)
        {
            List<string[]> rows = new List<string[]>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    row.Add(field.ToString());
                    field.Length = 0;
                }
                else if ((c == '\r' || c == '\n') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    row.Add(field.ToString());
                    field.Length = 0;
                    AddCsvRow(rows, row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(c);
                }
            }

            row.Add(field.ToString());
            AddCsvRow(rows, row);
            return rows;
        }

        private static void AddCsvRow(List<string[]> rows, List<string> row)
        {
            bool hasValue = false;
            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrEmpty(row[i]))
                {
                    hasValue = true;
                    break;
                }
            }

            if (hasValue)
            {
                rows.Add(row.ToArray());
            }
        }
    }
}
#endif