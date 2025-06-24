using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
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
        readonly Dictionary<string, Sprite> sprites = [];
        internal static readonly Color NormalColor = new(1f, 0.572549f, 0f, 1f);
        internal static readonly Color WarningColor = new(0.988f, 0.27f, 0f, 1f); // light red

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
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                try
                {
                    OnSceneLoaded(scene, mode);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"OnSceneLoaded exception: {ex}");
                }
            };

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
                toggleLocalTranslationButton = null;
                toggleLocalTranslationImage = null;
                customButton = null;
                toggleLabel = null;

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
                customButton.onClick.AddListener(() =>
                {
                    Logger.LogInfo("Toggling Local Translation Mode.");

                    useLocalMode = !useLocalMode;

                    SetRotationToLocalMode();
                });
            }

            Destroy(toggleLocalTranslationButton.GetComponent<LEV_HotkeyButton>());

            if (allSprites == null || allSprites?.Length == 0)
            {
                allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var sprite in allSprites)
                {
                    if (spritesStr.Contains(sprite.name))
                    {
                        if (!sprites.ContainsKey(sprite.name))
                        {
                            sprites.Add(sprite.name, sprite);
                        }

                    }
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
            if (__instance is null)
            {
                throw new ArgumentNullException(nameof(__instance));
            }

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
            if (__instance is null)
            {
                throw new ArgumentNullException(nameof(__instance));
            }

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
            if (__instance is null)
            {
                throw new ArgumentNullException(nameof(__instance));
            }

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
            if (__instance is null)
            {
                throw new ArgumentNullException(nameof(__instance));
            }

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
            if (__instance is null)
            {
                throw new ArgumentNullException(nameof(__instance));
            }

            if (Plugin.Instance.useLocalMode && Plugin.Instance.isModEnabled)
            {
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
            __instance.central.gizmos.translationGizmos.transform.localRotation = __instance.central.selection.list[^1].transform.localRotation;
        }
    }

    // DragGizmo
    [HarmonyPatch(typeof(LEV_GizmoHandler), "DragGizmo")]
    class Patch_DragGizmo_LocalTranslation
    {
        internal static Vector3? lastAxisPoint;
        static Vector3? initialDragOffset = Vector3.zero;

        static readonly List<string> planeNames = ["xy", "yz", "xz"];
        static readonly List<string> axisNames = ["x", "y", "z"];

        static readonly float maxDistance = 1500f;

        static bool Prefix(LEV_GizmoHandler __instance)
        {
            if (!Plugin.Instance.useLocalMode || !Plugin.Instance.isModEnabled) return true;

            var currentGizmo = __instance.currentGizmo;
            if (currentGizmo == null)
            {
                Plugin.logger.LogWarning("No current gizmo found during DragGizmo call.");
                return false;
            }

            var selection = Plugin.Instance.levelEditorCentral?.selection;

            if (selection == null)
            {
                Plugin.logger.LogWarning("No selection found during DragGizmo call.");
                return false;
            }

            var selectionList = selection?.list;
            var lastSelected = selectionList[^1];
            var selectionMiddlePivot = Plugin.Instance.levelEditorCentral?.gizmos?.motherGizmo?.transform?.position;

            if (selectionList.Count == 0)
            {
                Plugin.logger.LogWarning("No objects selected for local translation.");
                return false;
            }

            if (currentGizmo.name.Contains("R"))
            {
                return true;
            }

            string gizmoName = currentGizmo.name.Replace("Gizmo", "").ToLower();

            Transform gizmoTransform = currentGizmo.transform;

            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            DragPlane? dragPlane = GetPlaneFromGizmo(lastSelected, mouseRay, gizmoName);

            if (dragPlane == null)
            {
                //Plugin.logger.LogWarning($"Gizmo {gizmoTransform.name} does not match any DragPlane.");
                return false;
            }

            // Axis gizmos (X, Y, Z)

            if (axisNames.Any(gizmoName.Equals))
            {
                if (dragPlane.Value.Plane.Raycast(mouseRay, out float enter) && enter <= maxDistance)
                {
                    Vector3? axis = GetTranslationVector(lastSelected, gizmoName);

                    if (axis == null)
                    {
                        Plugin.logger.LogWarning($"Gizmo {gizmoName} does not have a valid translation vector.");
                        return false;
                    }

                    Vector3 hitPoint = mouseRay.GetPoint(enter);

                    // On initial click, store offset between pivot and where mouse hit the plane
                    if (!lastAxisPoint.HasValue)
                    {
                        lastAxisPoint = selectionMiddlePivot;

                        // Store offset for the rest of the drag
                        initialDragOffset = hitPoint - selectionMiddlePivot.Value;
                        return false; // Don't apply movement on initial click
                    }

                    Vector3 localOffset = hitPoint - initialDragOffset.Value;

                    Vector3 closestPoint = Vector3.Project(localOffset, axis.Value);

                    Vector3 moveDir = closestPoint - lastAxisPoint.Value;

                    float gridStep = gizmoName.Contains("y") ? __instance.gridY : __instance.gridXZ;
                    float distance = Vector3.Dot(moveDir, axis.Value);
                    float snappedDistance = gridStep > 0f ? Mathf.Round(distance / gridStep) * gridStep : distance;
                    Vector3 snappedMove = axis.Value * snappedDistance;

                    // Apply movement
                    foreach (var obj in selectionList)
                        obj.transform.position += snappedMove;

                    __instance.motherGizmo.transform.position += snappedMove;

                    // Advance lastAxisPoint by how far we actually moved
                    lastAxisPoint = lastAxisPoint.Value + snappedMove;

                    return false;
                }
                return false;
            }

            // Plane gizmos (XY, YZ, XZ)

            else if (planeNames.Any(gizmoName.Contains))
            {
                if (dragPlane.Value.Plane.Raycast(mouseRay, out float enter) && enter <= maxDistance)
                {
                    Vector3[] axes = [dragPlane.Value.Axis1.normalized, dragPlane.Value.Axis2.normalized];

                    Vector3 hitPoint = mouseRay.GetPoint(enter);

                    // On initial click, store offset between pivot and where mouse hit the plane
                    if (!lastAxisPoint.HasValue)
                    {
                        lastAxisPoint = selectionMiddlePivot;

                        // Store offset for the rest of the drag
                        initialDragOffset = hitPoint - selectionMiddlePivot.Value;
                        return false; // Don't apply movement on initial click
                    }

                    Vector3 localOffset = hitPoint - initialDragOffset.Value;

                    Vector3 rawMove = localOffset - lastAxisPoint.Value;

                    Vector3 snappedMove = Vector3.zero;

                    for (int i = 0; i < axes.Length; i++)
                    {
                        Vector3 axis = axes[i];

                        // Determine grid step based on the axis and gizmo name
                        float gridStep =
                            (gizmoName.Contains("xy") && i == 1) ||     // Y axis in XY plane
                            (gizmoName.Contains("yz") && i == 0)     // Y axis in YZ plane
                            ? __instance.gridY
                            : __instance.gridXZ;

                        float moveAmount = Vector3.Dot(rawMove, axis);
                        float snapped = gridStep > 0f ? Mathf.Round(moveAmount / gridStep) * gridStep : moveAmount;
                        snappedMove += axis * snapped;
                    }

                    foreach (var obj in selectionList)
                        obj.transform.position += snappedMove;

                    __instance.motherGizmo.transform.position += snappedMove;

                    lastAxisPoint = lastAxisPoint.Value + snappedMove;

                    return false;
                }
                Plugin.logger.LogWarning($"Mouse ray did not hit the plane for gizmo {gizmoName} (enter = {enter}). Skipping drag.");
                return false;
            }
            return false;

        }

        static Vector3? GetTranslationVector(BlockProperties reference, string name)
        {

            if (name.Contains("x") && !name.Contains("y") && !name.Contains("z"))
                return reference.transform.right;
            if (name.Contains("y") && !name.Contains("x") && !name.Contains("z"))
                return reference.transform.up;
            if (name.Contains("z") && !name.Contains("x") && !name.Contains("y"))
                return reference.transform.forward;

            return null;
        }

        static DragPlane? GetPlaneFromGizmo(BlockProperties reference, Ray mouseRay, string name)
        {
            Ray ray = mouseRay; // Use the mouse ray for intersection checks

            // Plane handles (XY, XZ, YZ)
            if (name.Contains("xy"))
                return new DragPlane(new Plane(reference.transform.forward, reference.transform.position), reference.transform.right, reference.transform.up);
            if (name.Contains("xz"))
                return new DragPlane(new Plane(reference.transform.up, reference.transform.position), reference.transform.right, reference.transform.forward);
            if (name.Contains("yz"))
                return new DragPlane(new Plane(reference.transform.right, reference.transform.position), reference.transform.up, reference.transform.forward);

            // Axis handles (X, Y, Z)
            Vector3 axis;
            if (name == "x") axis = reference.transform.right;
            else if (name == "y") axis = reference.transform.up;
            else if (name == "z") axis = reference.transform.forward;
            else
            {
                Plugin.logger.LogWarning($"Unknown gizmo name: {name}");
                return null;
            }

            // Use vector from camera to object as a stable basis
            Vector3 toObject = (reference.transform.position - Camera.main.transform.position).normalized;

            // Make a plane perpendicular to axis and facing the camera
            Vector3 planeNormal = Vector3.Cross(axis, toObject.normalized).normalized;
            if (planeNormal == Vector3.zero)
            {
                Plugin.logger.LogWarning($"Plane normal is zero for gizmo {name}. Cannot create drag plane.");
                return null;
            }

            planeNormal = Vector3.Cross(planeNormal, axis).normalized;

            float dot = Mathf.Abs(Vector3.Dot(ray.direction.normalized, planeNormal));
            if (dot < 0.1f)
            {
                // Plugin.logger.LogWarning($"Plane is nearly parallel to mouse ray (dot = {dot}) gizmo {name}. Skipping drag.");
                return null;
            }

            return new DragPlane(new Plane(planeNormal, reference.transform.position), Vector3.zero, Vector3.zero);
        }



        public struct DragPlane(Plane plane, Vector3 axis1, Vector3 axis2)
        {
            public Plane Plane = plane;
            public Vector3 Axis1 = axis1; // Local "right" axis
            public Vector3 Axis2 = axis2; // Local "up" axis
        }
    }

    [HarmonyPatch(typeof(LEV_MotherGizmoFlipper), "Update")]
    public class Patch_LEV_MotherGizmoFlipperUpdate_LocalTranslation
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

    [HarmonyPatch(typeof(LEV_GizmoHandler), "DisableGizmosOnDistance")]
    public static class Patch_DisableGizmosOnDistance_LocalTranslation
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
    public static class Patch_SetToDefaultColor_LocalTranslation
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

    // SetMotherPosition
    [HarmonyPatch(typeof(LEV_GizmoHandler), "SetMotherPosition")]
    public static class Patch_SetMotherPosition_LocalTranslation
    {
        static void Postfix(LEV_GizmoHandler __instance)
        {
            if (__instance is null)
            {
                throw new ArgumentNullException(nameof(__instance));
            }

            if (Plugin.Instance.useLocalMode && Plugin.Instance.isModEnabled)
            {
                // Reset the last mouse position to the current mouse position
                Patch_DragGizmo_LocalTranslation.lastAxisPoint = null;

            }
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
