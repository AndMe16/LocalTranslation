using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LocalTranslation.src
{ // will be replaced by assemblyName if desired
    [BepInPlugin("andme123.localtranslation", "LocalTranslation", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource logger;
        private Harmony harmony;

        internal bool isModEnabled = false;
        internal LEV_LevelEditorCentral levelEditorCentral;
        internal bool useLocalMode = false;

        // Only patch the LevelEditor2 scene
        private const string TargetSceneName = "LevelEditor2";

        public static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            logger = Logger;

            harmony = new Harmony("andme123.localtranslation");
            harmony.PatchAll();

            logger.LogInfo("Plugin andme123.localtranslation is loaded!");

            ModConfig.Initialize(Config);
            SceneManager.sceneLoaded += OnSceneLoaded;

        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            harmony?.UnpatchSelf();
            harmony = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == TargetSceneName)
            {
                levelEditorCentral = FindObjectOfType<LEV_LevelEditorCentral>();

                if (levelEditorCentral == null)
                {
                    Logger.LogError("LEV_LevelEditorCentral not found in the Level Editor scene.");
                    return;
                }
                isModEnabled = true;
                // Logger.LogInfo("Level Editor scene loaded — mod activated.");
            }
            else
            {
                isModEnabled = false;
                // Logger.LogInfo("Not in the Level Editor scene — mod inactive.");
            }
        }

        internal void SetRotationToLocalMode()
        {
            if (levelEditorCentral == null)
            {
                Logger.LogError("LEV_LevelEditorCentral is not initialized.");
                return;
            }
            var translationGizmos = levelEditorCentral.gizmos.translationGizmos;
            if (translationGizmos != null)
            {
                // Set gizmos rotation based on local mode
                if (useLocalMode)
                {
                    var selection_list = levelEditorCentral.selection.list;
                    if (selection_list.Count == 0)
                    {
                        // Logger.LogWarning("No objects selected for local translation mode.");
                        return;
                    }
                    else if (selection_list.Count > 1)
                    {
                        // Logger.LogWarning("Multiple objects selected, local translation mode may not work as expected.");
                    }
                    
                    // Get the last selected object's transform
                    var selectedTransform = selection_list[^1].transform;
                    // Set gizmos to local mode based on the selected object's transform
                    translationGizmos.transform.localRotation = selectedTransform.rotation; // Use the selected object's rotation
                    // Logger.LogInfo("TranslationGizmos set to local mode based on selected object.");
                    
                }
                else
                {
                    // Set gizmos to world mode
                    translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0); // Reset rotation to world space
                    // Logger.LogInfo("TranslationGizmos set to world mode.");
                }
            }
            else
            {
                Logger.LogWarning("TranslationGizmos not found");
            }
        }

        private void Update()
        {
            // Check if the user has toggled the local translation mode
            if (Input.GetKeyDown(ModConfig.toggleMode.Value) && isModEnabled)
            {
                useLocalMode = !useLocalMode;
                Logger.LogInfo($"Local Translation Mode: {(useLocalMode ? "Enabled" : "Disabled")}");

                SetRotationToLocalMode();

            }
        }
    }

    // SelectDrag
    [HarmonyPatch(typeof(LEV_GizmoHandler), "SelectDrag")]
    class Patch_SelectDrag_LocalTranslation
    {
        static void Postfix(LEV_GizmoHandler __instance)
        {
            if (Plugin.Instance.useLocalMode && Plugin.Instance.isModEnabled)
            {
                Plugin.Instance.SetRotationToLocalMode();
            }
        }
    }

    // ResetRotation
    [HarmonyPatch(typeof(LEV_GizmoHandler), "ResetRotation")]
    class Patch_SelectDrag_LocalRotation {
        static void Postfix(LEV_GizmoHandler __instance)
        {
            if (Plugin.Instance.useLocalMode && Plugin.Instance.isModEnabled)
            {
                Plugin.Instance.SetRotationToLocalMode();
            }
        }
    }

    // Activate
    [HarmonyPatch(typeof(LEV_GizmoHandler), "Activate")]
    class Patch_Activate_LocalTranslation
    {
        static void Postfix(LEV_GizmoHandler __instance)
        {
            if (Plugin.Instance.useLocalMode && Plugin.Instance.isModEnabled)
            {
                Plugin.Instance.SetRotationToLocalMode();
            }
        }
    }

    // GizmoJustGotClicked
    [HarmonyPatch(typeof(LEV_GizmoHandler), "GizmoJustGotClicked")]
    class Patch_GizmoJustGotClicked_LocalTranslation
    {
        static void Postfix(LEV_GizmoHandler __instance)
        {
            if (Plugin.Instance.useLocalMode && Plugin.Instance.isModEnabled)
            {
                // Plugin.logger.LogInfo($"Starting new drag operation for gizmo {__instance.currentGizmo.name}.");

                // Reset the last mouse position to the current mouse position
                Patch_DragGizmo_LocalTranslation.lastAxisPoint = null;

            }
        }
    }

    // CreateNewBlock
    [HarmonyPatch(typeof(LEV_GizmoHandler), "CreateNewBlock")]
    class Patch_CreatingNewBlock_LocalTranslation
    {
        static void Postfix(LEV_GizmoHandler __instance)
        {
            if (!Plugin.Instance.useLocalMode || !Plugin.Instance.isModEnabled) { return; }
            // Reset translationGizmos rotation
            // Plugin.logger.LogInfo("Creating new block, resetting translation gizmos rotation to world space.");
            __instance.translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0);

        }
    }

    // GoIntoGMode
    [HarmonyPatch(typeof(LEV_GizmoHandler), "Deactivate")]
    class Patch_Deactivate_LocalTranslation
    {
        static void Postfix(LEV_GizmoHandler __instance)
        {
            if (!Plugin.Instance.useLocalMode || !Plugin.Instance.isModEnabled) { return; }
            // Reset translationGizmos rotation
            // Plugin.logger.LogInfo("Deactivating gizmo, resetting translation gizmos rotation to world space.");
            __instance.translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }

    // AddThisBlock
    [HarmonyPatch(typeof(LEV_Selection), "AddThisBlock")]
    class Patch_AddThisBlock_LocalTranslation
    {
        static void Postfix(LEV_Selection __instance)
        {
            if (!Plugin.Instance.useLocalMode || !Plugin.Instance.isModEnabled) { return; }
            // Reset translationGizmos rotation
            // Plugin.logger.LogInfo("Selected new block, resetting translation gizmos rotation to world space.");
            __instance.central.gizmos.translationGizmos.transform.localRotation = __instance.central.selection.list[^1].transform.localRotation;
        }
    }

    // DragGizmo
    [HarmonyPatch(typeof(LEV_GizmoHandler), "DragGizmo")]
    class Patch_DragGizmo_LocalTranslation
    {
        internal static Vector3? lastAxisPoint;

        static readonly List<string> planeNames = ["XY", "YZ", "XZ"];
        static readonly List<string> axisNames = ["X", "Y", "Z"];


        static bool Prefix(LEV_GizmoHandler __instance)
        {
            //Plugin.logger.LogInfo("Local Translation Mode: DragGizmo called.");

            if (!Plugin.Instance.useLocalMode || !Plugin.Instance.isModEnabled) return true;
            
            var currentGizmo = __instance.currentGizmo;
            if (currentGizmo == null)
            {
                Plugin.logger.LogWarning("No current gizmo found during DragGizmo call.");
                return true;
            }

            var selection = Plugin.Instance.levelEditorCentral?.selection;

            if (selection == null)
            {
                Plugin.logger.LogWarning("No selection found during DragGizmo call.");
                return true;
            }

            var selectionList = selection?.list;
            var lastSelected = selectionList[^1];
            var selectionMiddlePivot = Plugin.Instance.levelEditorCentral?.gizmos?.motherGizmo?.transform?.position;

            if (selectionList.Count == 0)
            {
                Plugin.logger.LogWarning("No objects selected for local translation.");
                return true;
            }

            // Plugin.logger.LogInfo($"Current Gizmo: {currentGizmo.name}");

            if (currentGizmo.name.Contains("R"))
            {
                // Plugin.logger.LogInfo("Rotation Gizmo detected, skipping translation handling.");
                return true;
            }

            Transform gizmoTransform = currentGizmo.transform;

            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Plane gizmos (XY, YZ, XZ)
            if (planeNames.Any(gizmoTransform.name.Contains))
            {
                Plane? dragPlane = GetPlaneFromGizmo(gizmoTransform, lastSelected);

                if (dragPlane == null)
                {
                    Plugin.logger.LogWarning($"Gizmo {gizmoTransform.name} does not match any plane.");
                    return true;
                }

                if (dragPlane.Value.Raycast(mouseRay, out float hitDist))
                {
                    Vector3 hitPoint = mouseRay.GetPoint(hitDist);

                    if (!lastAxisPoint.HasValue)
                    {
                        lastAxisPoint = hitPoint;
                        return false;
                    }

                    Vector3 movement = hitPoint - lastAxisPoint.Value;
                    lastAxisPoint = hitPoint;

                    foreach (var obj in selectionList)
                        obj.transform.position += movement;

                    __instance.motherGizmo.transform.position += movement;

                    return false;
                }
            }

            // Axis gizmos (X, Y, Z)
            else if (axisNames.Any(gizmoTransform.name.Contains))
            {
                Vector3? axis = GetTranslationVector(gizmoTransform, lastSelected);
                if (axis == null)
                {
                    Plugin.logger.LogWarning($"Gizmo {gizmoTransform.name} does not match any axis or plane.");
                    return true;

                }
                Ray axisRay = new Ray((Vector3)selectionMiddlePivot, axis.Value);

                Vector3 closestPoint = ClosestPointBetweenLines(axisRay, mouseRay);

                if (!lastAxisPoint.HasValue)
                {
                    lastAxisPoint = closestPoint;
                }

                Vector3 moveDir = closestPoint - lastAxisPoint.Value;

                float gridStep = gizmoTransform.name.Contains("Y") ? __instance.gridY : __instance.gridXZ;
                float distance = moveDir.magnitude;

                // If the movement exceeds the grid step, apply only the largest full step
                if (distance >= gridStep*0.5)
                {
                    Vector3 direction = moveDir.normalized;

                    float snappedDistance = 0;
                    if (gridStep > 0f)
                    {
                        snappedDistance = Mathf.Floor(distance / gridStep) * gridStep;
                    }
                    else
                    {
                        snappedDistance = distance; // No grid snapping, use actual distance
                    }

                    Vector3 snappedMove = direction * snappedDistance;

                    // Apply movement
                    foreach (var obj in selectionList)
                        obj.transform.position += snappedMove;

                    __instance.motherGizmo.transform.position += snappedMove;

                    // Advance lastAxisPoint by how far we actually moved
                    lastAxisPoint = lastAxisPoint.Value + snappedMove;
                }

                return false;
            }
            return true;

        }

        static Vector3? GetTranslationVector(Transform gizmo, BlockProperties reference)
        {
            string name = gizmo.name.Replace("Gizmo", "").ToLower();

            if (name.Contains("x") && !name.Contains("y") && !name.Contains("z"))
                return reference.transform.right;
            if (name.Contains("y") && !name.Contains("x") && !name.Contains("z"))
                return reference.transform.up;
            if (name.Contains("z") && !name.Contains("x") && !name.Contains("y"))
                return reference.transform.forward;

            return null;
        }

        static Plane? GetPlaneFromGizmo(Transform gizmo, BlockProperties reference)
        {
            if (gizmo.name.Contains("XY"))
                return new Plane(reference.transform.forward, reference.transform.position);
            else if (gizmo.name.Contains("XZ"))
                return new Plane(reference.transform.up, reference.transform.position);
            else if (gizmo.name.Contains("YZ"))
                return new Plane(reference.transform.right, reference.transform.position);

            return null; 
        }

        static Vector3 ClosestPointBetweenLines(Ray ray1, Ray ray2)
        {

            Ray ray1_ = ray1;
            Ray ray2_ = ray2;

            Vector3 p1 = ray1_.origin;
            Vector3 d1 = ray1_.direction;
            Vector3 p2 = ray2_.origin;
            Vector3 d2 = ray2_.direction;

            float a = Vector3.Dot(d1, d1);
            float b = Vector3.Dot(d1, d2);
            float e = Vector3.Dot(d2, d2);

            Vector3 r = p1 - p2;
            float c = Vector3.Dot(d1, r);
            float f = Vector3.Dot(d2, r);

            float denominator = a * e - b * b;

            // Check if lines are parallel
            if (Mathf.Abs(denominator) < 0.0001f)
                return p1; // Arbitrary fallback, could return p1 or p2

            float s = (b * f - c * e) / denominator;
            return p1 + d1 * s;
        }

    }

    public class ModConfig : MonoBehaviour
    {
        public static ConfigEntry<KeyCode> toggleMode;

        // Constructor that takes a ConfigFile instance from the main class
        public static void Initialize(ConfigFile config)
        {
            toggleMode = config.Bind("1. Keybinds", "1.1 Toggle Global/Local", KeyCode.L, "Key to Toggle Global/Local translation");
        }
    }


}
