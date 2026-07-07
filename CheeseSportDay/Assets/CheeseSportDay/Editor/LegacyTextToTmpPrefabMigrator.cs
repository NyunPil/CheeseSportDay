#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace CheeseSportDay.Editor
{
    public static class LegacyTextToTmpPrefabMigrator
    {
        public const string FontAssetPath = "Assets/CheeseSportDay/Fonts/NotoSansKR SDF.asset";

        private const string SourceFontPath = "Assets/CheeseSportDay/Fonts/NotoSansKR-Variable.ttf";
        private const string PrefabFolder = "Assets/CheeseSportDay/Prefab";
        private const string ReferenceMapPath = "Assets/CheeseSportDay/Editor/LegacyTextReferenceMap.txt";

        private static bool isRunning;

        [InitializeOnLoadMethod]
        private static void QueueMigration()
        {
            AssetDatabase.importPackageCompleted -= OnPackageImported;
            AssetDatabase.importPackageCompleted += OnPackageImported;
            EditorApplication.delayCall += RunMigration;
        }

        [MenuItem("Cheese Sport Day/Migration/Convert Prefab Legacy Text To TMP")]
        public static void RunMigration()
        {
            if (isRunning || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunMigration;
                return;
            }

            if (!HasLegacyText())
            {
                return;
            }

            if (TMP_Settings.instance == null)
            {
                ImportTmpEssentialResources();
                return;
            }

            TMP_FontAsset fontAsset = GetOrCreateFontAsset();
            if (fontAsset == null)
            {
                EditorApplication.delayCall += RunMigration;
                return;
            }

            isRunning = true;
            try
            {
                Dictionary<string, List<ReferenceEntry>> references = LoadReferenceMap();
                string[] prefabPaths = GetPrefabPaths();
                for (int i = 0; i < prefabPaths.Length; i++)
                {
                    MigratePrefab(prefabPaths[i], fontAsset, references);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UdonSharpCompilerV1.CompileSync();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                isRunning = false;
            }
        }

        public static TMP_FontAsset GetOrCreateFontAsset()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
            {
                return existing;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
            {
                return null;
            }

            fontAsset.name = "NotoSansKR SDF";
            fontAsset.atlasTextures[0].name = "NotoSansKR SDF Atlas";
            fontAsset.material.name = "NotoSansKR SDF Material";

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void ImportTmpEssentialResources()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (packageInfo == null)
            {
                Debug.LogError("Unable to locate the TextMeshPro package.");
                return;
            }

            string packagePath = Path.Combine(
                packageInfo.resolvedPath,
                "Package Resources",
                "TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(packagePath, false);
        }

        private static void OnPackageImported(string packageName)
        {
            if (packageName.IndexOf("TMP Essential Resources", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                EditorApplication.delayCall += RunMigration;
            }
        }

        private static bool HasLegacyText()
        {
            string[] prefabPaths = GetPrefabPaths();
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab != null && prefab.GetComponentInChildren<Text>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] GetPrefabPaths()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            Array.Sort(paths, StringComparer.Ordinal);
            return paths;
        }

        private static void MigratePrefab(
            string prefabPath,
            TMP_FontAsset fontAsset,
            Dictionary<string, List<ReferenceEntry>> referenceMap)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            bool changed = false;
            try
            {
                Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);

                for (int i = 0; i < legacyTexts.Length; i++)
                {
                    Text legacyText = legacyTexts[i];
                    if (PrefabUtility.IsPartOfPrefabInstance(legacyText.gameObject))
                    {
                        continue;
                    }

                    LegacyTextSettings settings = new LegacyTextSettings(legacyText);
                    GameObject textObject = legacyText.gameObject;
                    UnityEngine.Object.DestroyImmediate(legacyText, true);
                    TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
                    CopyTextSettings(settings, tmp, fontAsset);
                    changed = true;
                }

                List<ReferenceEntry> entries;
                if (referenceMap.TryGetValue(prefabPath, out entries))
                {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        ReferenceEntry entry = entries[i];
                        Transform textTransform = FindTransform(root.transform, entry.textPath);
                        TextMeshProUGUI targetText = textTransform == null
                            ? null
                            : textTransform.GetComponent<TextMeshProUGUI>();
                        Component targetComponent = FindComponent(
                            root.transform,
                            entry.componentPath,
                            entry.componentType);

                        if (targetComponent == null
                            || targetText == null
                            || PrefabUtility.IsPartOfPrefabInstance(targetComponent.gameObject))
                        {
                            continue;
                        }

                        SerializedObject serializedObject = new SerializedObject(targetComponent);
                        SerializedProperty property = serializedObject.FindProperty(entry.propertyPath);
                        if (property == null)
                        {
                            continue;
                        }

                        property.objectReferenceValue = targetText;
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }


                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CopyTextSettings(
            LegacyTextSettings source,
            TextMeshProUGUI target,
            TMP_FontAsset fontAsset)
        {
            target.font = fontAsset;
            target.text = source.text;
            target.color = source.color;
            target.fontSize = source.fontSize;
            target.fontStyle = ConvertFontStyle(source.fontStyle);
            target.alignment = ConvertAlignment(source.alignment);
            target.enableAutoSizing = source.resizeTextForBestFit;
            target.fontSizeMin = Mathf.Max(1f, source.resizeTextMinSize);
            target.fontSizeMax = Mathf.Max(target.fontSizeMin, source.resizeTextMaxSize);
            target.enableWordWrapping = source.horizontalOverflow != HorizontalWrapMode.Overflow;
            target.overflowMode = source.verticalOverflow == VerticalWrapMode.Overflow
                ? TextOverflowModes.Overflow
                : TextOverflowModes.Truncate;
            target.richText = source.supportRichText;
            target.lineSpacing = (source.lineSpacing - 1f) * 100f;
            target.raycastTarget = source.raycastTarget;
            target.maskable = source.maskable;
            target.enabled = source.enabled;
        }

        private static FontStyles ConvertFontStyle(FontStyle style)
        {
            if (style == FontStyle.Bold)
            {
                return FontStyles.Bold;
            }

            if (style == FontStyle.Italic)
            {
                return FontStyles.Italic;
            }

            if (style == FontStyle.BoldAndItalic)
            {
                return FontStyles.Bold | FontStyles.Italic;
            }

            return FontStyles.Normal;
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static Dictionary<string, List<ReferenceEntry>> LoadReferenceMap()
        {
            Dictionary<string, List<ReferenceEntry>> result =
                new Dictionary<string, List<ReferenceEntry>>(StringComparer.Ordinal);
            if (!File.Exists(ReferenceMapPath))
            {
                return result;
            }

            string[] lines = File.ReadAllLines(ReferenceMapPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] values = lines[i].Split('\t');
                if (values.Length != 5)
                {
                    continue;
                }

                ReferenceEntry entry = new ReferenceEntry
                {
                    prefabPath = values[0],
                    componentPath = values[1],
                    componentType = values[2],
                    propertyPath = values[3],
                    textPath = values[4]
                };

                List<ReferenceEntry> entries;
                if (!result.TryGetValue(entry.prefabPath, out entries))
                {
                    entries = new List<ReferenceEntry>();
                    result.Add(entry.prefabPath, entries);
                }

                entries.Add(entry);
            }

            return result;
        }

        private static Component FindComponent(
            Transform root,
            string transformPath,
            string componentType)
        {
            Transform target = FindTransform(root, transformPath);
            if (target == null)
            {
                return null;
            }

            Component[] components = target.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null && component.GetType().FullName == componentType)
                {
                    return component;
                }
            }

            return null;
        }

        private static Transform FindTransform(Transform root, string path)
        {
            return path == "." ? root : root.Find(path);
        }

        private static string GetTransformPath(Transform root, Transform target)
        {
            if (target == root)
            {
                return ".";
            }

            List<string> parts = new List<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private class ReferenceEntry
        {
            public string prefabPath;
            public string componentPath;
            public string componentType;
            public string propertyPath;
            public string textPath;
        }

        private class LegacyTextSettings
        {
            public readonly string text;
            public readonly Color color;
            public readonly int fontSize;
            public readonly FontStyle fontStyle;
            public readonly TextAnchor alignment;
            public readonly bool resizeTextForBestFit;
            public readonly int resizeTextMinSize;
            public readonly int resizeTextMaxSize;
            public readonly HorizontalWrapMode horizontalOverflow;
            public readonly VerticalWrapMode verticalOverflow;
            public readonly bool supportRichText;
            public readonly float lineSpacing;
            public readonly bool raycastTarget;
            public readonly bool maskable;
            public readonly bool enabled;

            public LegacyTextSettings(Text source)
            {
                text = source.text;
                color = source.color;
                fontSize = source.fontSize;
                fontStyle = source.fontStyle;
                alignment = source.alignment;
                resizeTextForBestFit = source.resizeTextForBestFit;
                resizeTextMinSize = source.resizeTextMinSize;
                resizeTextMaxSize = source.resizeTextMaxSize;
                horizontalOverflow = source.horizontalOverflow;
                verticalOverflow = source.verticalOverflow;
                supportRichText = source.supportRichText;
                lineSpacing = source.lineSpacing;
                raycastTarget = source.raycastTarget;
                maskable = source.maskable;
                enabled = source.enabled;
            }
        }
    }
}
#endif
