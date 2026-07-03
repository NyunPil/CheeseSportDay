#if UNITY_EDITOR
using CheeseSportDay.Interactions;
using UdonSharp;
using UdonSharp.Compiler;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

namespace CheeseSportDay.Editor
{
    public static class InteractionSetupMenu
    {
        private const string ChairScriptPath = "Assets/CheeseSportDay/Interactions/InteractableChair.cs";
        private const string ChairAssetPath = "Assets/CheeseSportDay/Interactions/InteractableChair.asset";
        private const string AnimatorButtonScriptPath = "Assets/CheeseSportDay/Interactions/AnimatorBoolToggleButton.cs";
        private const string AnimatorButtonAssetPath = "Assets/CheeseSportDay/Interactions/AnimatorBoolToggleButton.asset";

        [InitializeOnLoadMethod]
        private static void QueueProgramAssetSetup()
        {
            EditorApplication.delayCall += EnsureProgramAssets;
        }

        [MenuItem("Cheese Sport Day/Interactions/Setup Selected Chair")]
        public static void SetupSelectedChair()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Chair Setup", "Select one or more chair objects first.", "OK");
                return;
            }

            EnsureProgramAssets();

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Selected Chairs");

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                SetupChair(selectedObjects[i]);
            }

            Undo.CollapseUndoOperations(undoGroup);
            Selection.objects = selectedObjects;
        }

        private static void SetupChair(GameObject selected)
        {
            if (selected == null)
            {
                return;
            }

            EnsureCollider(selected);

            VRCStation station = selected.GetComponent<VRCStation>();
            if (station == null)
            {
                station = Undo.AddComponent<VRCStation>(selected);
            }

            Transform seatPoint = GetOrCreatePoint(selected.transform, "Seat Point", new Vector3(0f, 0.5f, 0f));
            Transform exitPoint = GetOrCreatePoint(selected.transform, "Exit Point", new Vector3(0f, 0f, 0.8f));

            Undo.RecordObject(station, "Configure Chair Station");
            station.seated = true;
            station.disableStationExit = false;
            station.canUseStationFromStation = true;
            station.PlayerMobility = VRCStation.Mobility.Immobilize;
            station.stationEnterPlayerLocation = seatPoint;
            station.stationExitPlayerLocation = exitPoint;

            InteractableChair chair = selected.GetComponent<InteractableChair>();
            if (chair == null)
            {
                chair = Undo.AddComponent<InteractableChair>(selected);
            }

            Undo.RecordObject(chair, "Configure Interactable Chair");
            chair.station = station;
            chair.interactAgainToExit = true;

            EditorUtility.SetDirty(station);
            EditorUtility.SetDirty(chair);
        }

        [MenuItem("Cheese Sport Day/Interactions/Add Animator Bool Toggle To Selected")]
        public static void AddAnimatorBoolToggleToSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Animator Toggle Setup", "Select a button object first.", "OK");
                return;
            }

            EnsureProgramAssets();
            EnsureCollider(selected);

            AnimatorBoolToggleButton button = selected.GetComponent<AnimatorBoolToggleButton>();
            if (button == null)
            {
                button = Undo.AddComponent<AnimatorBoolToggleButton>(selected);
            }

            Undo.RecordObject(button, "Configure Animator Bool Toggle");
            if (button.targetAnimator == null)
            {
                button.targetAnimator = selected.GetComponent<Animator>();
            }

            if (string.IsNullOrEmpty(button.boolParameter))
            {
                button.boolParameter = "IsActive";
            }

            button.syncForEveryone = true;
            EditorUtility.SetDirty(button);
            Selection.activeGameObject = selected;
            EditorGUIUtility.PingObject(selected);
        }

        public static void EnsureProgramAssets()
        {
            bool created = false;
            created |= CreateProgramAssetIfMissing(ChairScriptPath, ChairAssetPath);
            created |= CreateProgramAssetIfMissing(AnimatorButtonScriptPath, AnimatorButtonAssetPath);

            if (created)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            if (created || NeedsCompile(ChairAssetPath) || NeedsCompile(AnimatorButtonAssetPath))
            {
                UdonSharpCompilerV1.CompileSync();
                AssetDatabase.SaveAssets();
            }
        }

        private static void EnsureCollider(GameObject target)
        {
            if (target.GetComponent<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(target);
            }
        }

        private static Transform GetOrCreatePoint(Transform parent, string name, Vector3 localPosition)
        {
            Transform point = parent.Find(name);
            bool created = point == null;

            if (created)
            {
                GameObject pointObject = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(pointObject, "Create " + name);
                pointObject.transform.SetParent(parent, false);
                pointObject.transform.localPosition = localPosition;
                point = pointObject.transform;
            }

            Undo.RecordObject(point, "Straighten " + name);
            float yaw = created ? parent.eulerAngles.y : point.eulerAngles.y;
            point.rotation = Quaternion.Euler(0f, yaw, 0f);
            EditorUtility.SetDirty(point);
            return point;
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
                return false;
            }

            UdonSharpProgramAsset programAsset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            programAsset.sourceCsScript = sourceScript;
            AssetDatabase.CreateAsset(programAsset, assetPath);
            return true;
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
    }
}
#endif
