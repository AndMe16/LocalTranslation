using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LocalTranslation.src
{ // will be replaced by assemblyName if desired
    [BepInPlugin("andme123.localtranslation", "LocalTranslation", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource logger;
        private Harmony harmony;

        internal bool isModEnabled = false;
        internal LEV_LevelEditorCentral levelEditorCentral;
        internal LEV_RotateFlip rotateFlip;
        internal bool useLocalMode = false;

        Sprite[] allSprites;

        readonly string[] spritesStr = ["Pivot_LastSelected", "Pivot_Average", "Pivot_ToggleOff_NonDefault", "Pivot_ToggleOn_NonDefault"];
        Dictionary<string,Sprite> sprites = new Dictionary<string, Sprite>();
        internal static readonly Color NormalColor = new Color(1f, 0.572549f, 0f, 1f);
        internal static readonly Color WarningColor = new Color(0.988f, 0.27f, 0f, 1f); // light red

        // Only patch the LevelEditor2 scene
        private const string TargetSceneName = "LevelEditor2";

        GameObject baseButton;
        internal GameObject toggleLocalTranslationButton;
        Image toggleLocalTranslationImage;

        GameObject baseLabel;
        internal GameObject toggleLabel;

        internal LEV_CustomButton customButton;

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

                baseButton = GameObject.Find("Level Editor Central/Canvas/GameView/Gizmo Mode (true)--------------/_Top Right/Global Rotation Toggle");

                if (baseButton == null)
                {
                    Logger.LogError("Base button for toggling global/local mode not found.");
                    return;
                }

                rotateFlip = levelEditorCentral.GetComponentInChildren<LEV_RotateFlip>();

                if (rotateFlip == null)
                {
                    Logger.LogError("LEV_RotateFlip component not found in Level Editor Central.");
                    return;
                }

                baseLabel = GameObject.Find("Level Editor Central/Canvas/GameView/Gizmo Mode (true)--------------/_Top Right/Global Rotation Label");
                if (baseLabel == null)
                {
                    Logger.LogError("Base label for toggling global/local mode not found.");
                    return;
                }

                CreateToggleLocalModeButton();

                isModEnabled = true;
                // Logger.LogInfo("Level Editor scene loaded — mod activated.");
            }
            else
            {
                isModEnabled = false;
                useLocalMode = false; // Reset local mode when not in Level Editor scene
                toggleLocalTranslationButton?.SetActive(false); // Hide the button when not in Level Editor scene
                toggleLabel?.SetActive(false);
                toggleLocalTranslationImage = null; // Reset the image reference
                // Logger.LogInfo("Not in the Level Editor scene — mod inactive.");
            }
        }

        private void CreateToggleLocalModeButton()
        {
            if (toggleLocalTranslationButton == null)
            {
                toggleLocalTranslationButton = GameObject.Instantiate(baseButton, baseButton.transform.parent);
            }

            toggleLocalTranslationButton.name = "Local Translation Toggle";

            toggleLocalTranslationImage = toggleLocalTranslationButton.transform.GetChild(0).gameObject.GetComponent<Image>();

            if (toggleLocalTranslationImage == null)
            {
                Logger.LogError("Global Rotation Toggle button image not found.");
                return;
            }

            // Now access the LEV_CustomButton component
            customButton = toggleLocalTranslationButton.GetComponent<LEV_CustomButton>();

            if (customButton != null)
            {
                // Add a click listener
                customButton.onClick = new UnityEvent(); // clears all listeners
                customButton.onClick.AddListener(() => {
                    Logger.LogInfo("Toggling Local Translation Mode.");

                    useLocalMode = !useLocalMode;

                    SetRotationToLocalMode();
                });
            }

            Destroy(toggleLocalTranslationButton.GetComponent<LEV_HotkeyButton>());

            if (allSprites == null || allSprites?.Length == 0)
            {
                allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            }
            
            foreach (var sprite in allSprites)
            {
                if (spritesStr.Contains(sprite.name))
                {
                    sprites.Add(sprite.name, sprite);
                }
            }

            if (!useLocalMode)
            {
                toggleLocalTranslationImage.sprite = sprites["Pivot_Average"];
            }
            else
            {
                toggleLocalTranslationImage.sprite = sprites["Pivot_LastSelected"];
            }


                toggleLocalTranslationButton.SetActive(false); 

            if (toggleLabel == null)
            {
                toggleLabel = GameObject.Instantiate(baseLabel, baseLabel.transform.parent);
            }

            toggleLabel.name = "Local Translation Label";

            toggleLabel.GetComponent<TextMeshProUGUI>().text = "Global Translation";

            toggleLabel.SetActive(false);

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

                    toggleLocalTranslationImage.sprite = sprites["Pivot_LastSelected"];

                    // Logger.LogInfo("TranslationGizmos set to local mode based on selected object.");

                }
                else
                {
                    // Set gizmos to world mode
                    translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0); // Reset rotation to world space

                    toggleLocalTranslationImage.sprite = sprites["Pivot_Average"]; 

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

            if (!isModEnabled) return;

            // Check if the user has toggled the local translation mode
            if (Input.GetKeyDown(ModConfig.toggleMode.Value))
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
            if (Plugin.Instance.isModEnabled)
            {
                Plugin.Instance.toggleLocalTranslationButton.SetActive(true);
                Plugin.Instance.toggleLabel.SetActive(true);
            }
                
        }
    }

    // SelectRotate
    [HarmonyPatch(typeof(LEV_GizmoHandler), "SelectRotate")]
    class Patch_SelectRotate_LocalTranslation
    {
        static void Postfix(LEV_GizmoHandler __instance)
        {
            if (Plugin.Instance.isModEnabled)
            {
                Plugin.Instance.toggleLocalTranslationButton.SetActive(false);
                Plugin.Instance.toggleLabel.SetActive(false);
            }
        }
    }

    // ResetRotation
    [HarmonyPatch(typeof(LEV_GizmoHandler), "ResetRotation")]
    class Patch_ResetRotation_LocalRotation
    {
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

    // Deactivate
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
                if (distance >= gridStep*0.5f)
                {
                    Vector3 direction = moveDir.normalized;

                    float snappedDistance = 0;
                    if (gridStep > 0f)
                    {
                        snappedDistance = gridStep;
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

        [HarmonyPatch(typeof(LEV_MotherGizmoFlipper), "Update")]
        public class TranslationGizmo_Update_Patch
        {
            static bool Prefix(LEV_MotherGizmoFlipper __instance)
            {
                if (!Plugin.Instance.useLocalMode || !Plugin.Instance.isModEnabled) return true;

                if (__instance.central.gizmos.isDragging)
                    return false;

                Transform t = __instance.t;
                Transform camTransform = __instance.central.cam.cameraTransform;
                Transform xyzFlip = __instance.xyzFlip;

                Vector3 localCamPos = t.InverseTransformPoint(camTransform.position);
                xyzFlip.localScale = new Vector3(
                    Mathf.Sign(localCamPos.x),
                    Mathf.Sign(localCamPos.y),
                    Mathf.Sign(localCamPos.z)
                );

                return false; // Skip original Update logic
            }
        }
    }

    [HarmonyPatch(typeof(LEV_GizmoHandler), "DisableGizmosOnDistance")]
    public static class TranslationGizmo_DisablePatch
    {
        static bool Prefix(LEV_GizmoHandler __instance)
        {
            if (!Plugin.Instance.useLocalMode || !Plugin.Instance.isModEnabled) return true;

            Transform camTransform = Camera.main.transform;
            Transform gizmoRoot = __instance.translationGizmos.transform;

            // Calculate view direction in local gizmo space
            Vector3 localViewDir = (gizmoRoot.InverseTransformPoint(camTransform.position) - gizmoRoot.InverseTransformPoint(gizmoRoot.position)).normalized;

            // Thresholds
            const float axisDotThreshold = 0.98f;     // axis disappears if view is almost parallel to axis
            const float planeDotThreshold = 0.05f;     // plane disappears if view is nearly perpendicular to plane

            // Axis gizmos: disable if camera is looking *along* the axis
            SetGizmoActive(__instance.Xgizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.right)) < axisDotThreshold);
            SetGizmoActive(__instance.Ygizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.up)) < axisDotThreshold);
            SetGizmoActive(__instance.Zgizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.forward)) < axisDotThreshold);

            // Plane gizmos: disable if camera is looking *edge-on* to the plane (aligned with the plane's normal)
            SetGizmoActive(__instance.XYgizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.forward)) > planeDotThreshold); // Z normal
            SetGizmoActive(__instance.YZgizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.right)) > planeDotThreshold);  // X normal
            SetGizmoActive(__instance.XZgizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.up)) > planeDotThreshold);     // Y normal

            // Keep rotation gizmo logic as-is, based on original distance system
            __instance.DisableOrNotIndividualGizmo(__instance.RXgizmo, __instance.RXdist);
            __instance.DisableOrNotIndividualGizmo(__instance.RYgizmo, __instance.RYdist);
            __instance.DisableOrNotIndividualGizmo(__instance.RZgizmo, __instance.RZdist);

            return false; // skip original method
        }

        private static void SetGizmoActive(LEV_SingleGizmo gizmo, bool active)
        {
            if (gizmo == null) return;

            if (active && !gizmo.gameObject.activeSelf)
            {
                gizmo.gameObject.SetActive(true);
            }
            else if (!active && gizmo.gameObject.activeSelf)
            {
                gizmo.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(LEV_CustomButton), "SetToDefaultColor")]
    public static class CustomButton_SetToDefaultColor_Patch
    {
        static void Postfix(LEV_CustomButton __instance)
        {
            if (!Plugin.Instance.useLocalMode || !Plugin.Instance.isModEnabled) return;

            // Only apply the warning logic to the specific button you care about
            if (__instance != Plugin.Instance.customButton)
                return;

            var selection_list = Plugin.Instance.levelEditorCentral?.selection?.list;

            if (selection_list.Count == 0) return; // No objects selected

            // Get the last selected object's transform
            var selectedBlock = selection_list[^1];

            if (selectedBlock == null || Plugin.Instance.customButton == null) return;

            // Compare rotation against global alignment
            Quaternion blockRot = selectedBlock.transform.rotation;
            bool isAligned = Quaternion.Angle(blockRot, Quaternion.identity) < 1f;

            Plugin.Instance.customButton.buttonImage.color = isAligned ? Plugin.NormalColor : Plugin.WarningColor;
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
