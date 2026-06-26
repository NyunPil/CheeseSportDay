#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using CheeseSportDay.WorldUI;
using UnityEditor;
using UnityEngine;

namespace CheeseSportDay.Editor
{
    public class ParticipantRosterCsvImporterWindow : EditorWindow
    {
        private ParticipantRosterScreen targetScreen;
        private TextAsset csvAsset;
        private bool autoConvertTexturesToSprites = true;

        [MenuItem("Cheese Sport Day/Participant Roster/Import CSV")]
        public static void Open()
        {
            GetWindow<ParticipantRosterCsvImporterWindow>("Roster CSV Import");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Participant Roster CSV", EditorStyles.boldLabel);
            targetScreen = (ParticipantRosterScreen)EditorGUILayout.ObjectField("Target Screen", targetScreen, typeof(ParticipantRosterScreen), true);
            csvAsset = (TextAsset)EditorGUILayout.ObjectField("CSV TextAsset", csvAsset, typeof(TextAsset), false);
            autoConvertTexturesToSprites = EditorGUILayout.Toggle("Auto Convert Images", autoConvertTexturesToSprites);

            EditorGUILayout.HelpBox(
                "Recognized columns: name/이름, image/이미지, title/종목, detail/설명, gameSkill/게임실력, gameSense/게임센스, teamwork/협동력, physical/피지컬, luck/운. Image values should be Resources paths such as Participants/Alice.",
                MessageType.Info);

            EditorGUI.BeginDisabledGroup(targetScreen == null || csvAsset == null);
            if (GUILayout.Button("Import CSV To Screen"))
            {
                ImportCsv();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void ImportCsv()
        {
            List<string[]> rows = ParseCsv(csvAsset.text);
            if (rows.Count < 2)
            {
                EditorUtility.DisplayDialog("CSV Import", "CSV must contain a header row and at least one participant row.", "OK");
                return;
            }

            string[] headers = rows[0];
            if (headers.Length > 0)
            {
                headers[0] = headers[0].Trim('\uFEFF');
            }

            int nameColumn = FindColumn(headers, "name", "displayname", "participant", "nickname", "이름", "닉네임", "참가자");
            int imageColumn = FindColumn(headers, "image", "imagepath", "portrait", "portraitpath", "avatar", "resources", "이미지", "사진", "프로필", "이미지경로");
            int titleColumn = FindColumn(headers, "title", "category", "game", "event", "role", "종목", "분류", "게임", "타이틀", "역할");
            int detailColumn = FindColumn(headers, "detail", "description", "note", "memo", "comment", "설명", "비고", "메모", "특이사항");
            int skillColumn = FindColumn(headers, "gameskill", "skill", "ability", "게임실력", "실력");
            int senseColumn = FindColumn(headers, "gamesense", "sense", "awareness", "게임센스", "센스");
            int teamworkColumn = FindColumn(headers, "teamwork", "cooperation", "collaboration", "협동력", "협력");
            int physicalColumn = FindColumn(headers, "physical", "stamina", "피지컬", "체력");
            int luckColumn = FindColumn(headers, "luck", "fortune", "운", "운빨");

            if (nameColumn < 0)
            {
                EditorUtility.DisplayDialog("CSV Import", "No participant name column found. Add a name or 이름 column.", "OK");
                return;
            }

            bool[] knownColumns = BuildKnownColumnMap(headers.Length, nameColumn, imageColumn, titleColumn, detailColumn, skillColumn, senseColumn, teamworkColumn, physicalColumn, luckColumn);
            List<string> names = new List<string>();
            List<string> titles = new List<string>();
            List<string> details = new List<string>();
            List<Sprite> portraits = new List<Sprite>();
            List<int> skillValues = new List<int>();
            List<int> senseValues = new List<int>();
            List<int> teamworkValues = new List<int>();
            List<int> physicalValues = new List<int>();
            List<int> luckValues = new List<int>();
            List<string> warnings = new List<string>();

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] row = rows[rowIndex];
                string name = GetCell(row, nameColumn).Trim();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                names.Add(name);
                titles.Add(GetCell(row, titleColumn));
                details.Add(BuildDetailText(headers, row, knownColumns, detailColumn));
                portraits.Add(LoadPortrait(GetCell(row, imageColumn), warnings));
                skillValues.Add(ParseStat(GetCell(row, skillColumn)));
                senseValues.Add(ParseStat(GetCell(row, senseColumn)));
                teamworkValues.Add(ParseStat(GetCell(row, teamworkColumn)));
                physicalValues.Add(ParseStat(GetCell(row, physicalColumn)));
                luckValues.Add(ParseStat(GetCell(row, luckColumn)));
            }

            Undo.RecordObject(targetScreen, "Import Participant Roster CSV");
            targetScreen.participantNames = names.ToArray();
            targetScreen.participantTitles = titles.ToArray();
            targetScreen.participantDetails = details.ToArray();
            targetScreen.participantPortraits = portraits.ToArray();
            targetScreen.gameSkillValues = skillValues.ToArray();
            targetScreen.gameSenseValues = senseValues.ToArray();
            targetScreen.teamworkValues = teamworkValues.ToArray();
            targetScreen.physicalValues = physicalValues.ToArray();
            targetScreen.luckValues = luckValues.ToArray();
            ApplyStatLabels(headers, targetScreen, skillColumn, senseColumn, teamworkColumn, physicalColumn, luckColumn);
            targetScreen.RefreshAll();

            EditorUtility.SetDirty(targetScreen);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetScreen);
            AssetDatabase.SaveAssets();

            string message = "Imported " + names.Count + " participants.";
            if (warnings.Count > 0)
            {
                message += "\n\nWarnings:\n" + string.Join("\n", warnings.ToArray());
            }

            EditorUtility.DisplayDialog("CSV Import", message, "OK");
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
                warnings.Add("Missing sprite: " + rawPath + " -> Resources/" + resourcesPath);
            }

            return sprite;
        }

        private static void ApplyStatLabels(string[] headers, ParticipantRosterScreen screen, int skillColumn, int senseColumn, int teamworkColumn, int physicalColumn, int luckColumn)
        {
            if (skillColumn >= 0) screen.gameSkillLabel = headers[skillColumn];
            if (senseColumn >= 0) screen.gameSenseLabel = headers[senseColumn];
            if (teamworkColumn >= 0) screen.teamworkLabel = headers[teamworkColumn];
            if (physicalColumn >= 0) screen.physicalLabel = headers[physicalColumn];
            if (luckColumn >= 0) screen.luckLabel = headers[luckColumn];
        }

        private static string BuildDetailText(string[] headers, string[] row, bool[] knownColumns, int detailColumn)
        {
            StringBuilder builder = new StringBuilder();
            string directDetail = GetCell(row, detailColumn);
            if (!string.IsNullOrEmpty(directDetail))
            {
                builder.Append(directDetail);
            }

            for (int i = 0; i < headers.Length; i++)
            {
                if (i < knownColumns.Length && knownColumns[i])
                {
                    continue;
                }

                string value = GetCell(row, i);
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(headers[i]);
                builder.Append(": ");
                builder.Append(value);
            }

            return builder.ToString();
        }

        private static bool[] BuildKnownColumnMap(int length, params int[] indices)
        {
            bool[] result = new bool[length];
            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];
                if (index >= 0 && index < result.Length)
                {
                    result[index] = true;
                }
            }

            return result;
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

        private static int ParseStat(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            StringBuilder digits = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsDigit(c) || c == '-')
                {
                    digits.Append(c);
                }
            }

            int result;
            return int.TryParse(digits.ToString(), out result) ? result : 0;
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
