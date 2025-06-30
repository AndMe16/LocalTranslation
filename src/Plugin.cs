using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FMODSyntax;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LocalTranslation;

// will be replaced by assemblyName if desired
[BepInPlugin("andme123.localtranslation", "LocalTranslation", MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    // Only patch the LevelEditor2 scene
    private const string TargetSceneName = "LevelEditor2";
    internal static ManualLogSource logger;
    internal static readonly Color NormalColor = new(1f, 0.572549f, 0f, 1f);
    internal static readonly Color WarningColor = new(0.988f, 0.27f, 0f, 1f); // light red
    private readonly Dictionary<string, Sprite> _sprites = [];

    private readonly string[] _spritesStr =
        ["Pivot_LastSelected", "Pivot_Average", "Pivot_ToggleOff_NonDefault", "Pivot_ToggleOn_NonDefault"];

    private Sprite[] _allSprites;

    private GameObject _baseButton;

    private GameObject _baseLabel;
    private Harmony _harmony;
    private LEV_RotateFlip _rotateFlip;
    private Image _toggleLocalTranslationImage;

    internal LEV_CustomButton CustomButton;

    internal bool IsModEnabled;
    internal LEV_LevelEditorCentral LevelEditorCentral;
    internal GameObject ToggleLabel;
    internal GameObject ToggleLocalTranslationButton;
    internal bool UseLocalMode;

    internal Transform referenceBlock;

    public static Plugin Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        logger = Logger;

        _harmony = new Harmony("andme123.localtranslation");
        _harmony.PatchAll();

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

    private void Update()
    {
        if (!IsModEnabled) return;

        if (!LevelEditorCentral) return;

        // Check if the user has toggled the local translation mode
        if (Input.GetKeyDown(ModConfig.toggleMode.Value) && !LevelEditorCentral.input.inputLocked && !LevelEditorCentral.gizmos.isGrabbing)
        {
            UseLocalMode = !UseLocalMode;
            Logger.LogInfo($"Local Translation Mode: {(UseLocalMode ? "Enabled" : "Disabled")}");

            SetRotationToLocalMode();
        }

        if (Input.GetKeyDown(ModConfig.setReference.Value) && !LevelEditorCentral.input.inputLocked && !LevelEditorCentral.gizmos.isGrabbing)
        {
            if (LevelEditorCentral.selection.list.Count == 0)
            {
                Logger.LogWarning("No blocks selected for local translation.");
                return;
            }

            referenceBlock = LevelEditorCentral.selection.list[^1].transform;
            Logger.LogInfo(
                $"Reference block for local translation set to: {referenceBlock.name} at position {referenceBlock.position} with rotation {referenceBlock.rotation}");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == TargetSceneName)
        {
            LevelEditorCentral = FindObjectOfType<LEV_LevelEditorCentral>();

            if (!LevelEditorCentral)
            {
                Logger.LogError("LEV_LevelEditorCentral not found in the Level Editor scene.");
                return;
            }

            _baseButton =
                GameObject.Find(
                    "Level Editor Central/Canvas/GameView/Gizmo Mode (true)--------------/_Top Right/Global Rotation Toggle");

            if (!_baseButton)
            {
                Logger.LogError("Base button for toggling global/local mode not found.");
                return;
            }

            _rotateFlip = LevelEditorCentral.GetComponentInChildren<LEV_RotateFlip>();

            if (!_rotateFlip)
            {
                Logger.LogError("LEV_RotateFlip component not found in Level Editor Central.");
                return;
            }

            _baseLabel = GameObject.Find(
                "Level Editor Central/Canvas/GameView/Gizmo Mode (true)--------------/_Top Right/Global Rotation Label");
            if (!_baseLabel)
            {
                Logger.LogError("Base label for toggling global/local mode not found.");
                return;
            }

            CreateToggleLocalModeButton();

            IsModEnabled = true;
            // Logger.LogInfo("Level Editor scene loaded — mod activated.");
        }
        else
        {
            IsModEnabled = false;
            UseLocalMode = false; // Reset local mode when not in Level Editor scene
            ToggleLocalTranslationButton = null;
            _toggleLocalTranslationImage = null;
            CustomButton = null;
            ToggleLabel = null;

            // Logger.LogInfo("Not in the Level Editor scene — mod inactive.");
        }
    }

    private void CreateToggleLocalModeButton()
    {
        if (!ToggleLocalTranslationButton)
            ToggleLocalTranslationButton = Instantiate(_baseButton, _baseButton.transform.parent);

        ToggleLocalTranslationButton.name = "Local Translation Toggle";

        _toggleLocalTranslationImage =
            ToggleLocalTranslationButton.transform.GetChild(0).gameObject.GetComponent<Image>();

        if (!_toggleLocalTranslationImage)
        {
            Logger.LogError("Global Rotation Toggle button image not found.");
            return;
        }

        // Now access the LEV_CustomButton component
        CustomButton = ToggleLocalTranslationButton.GetComponent<LEV_CustomButton>();

        if (CustomButton)
        {
            // Add a click listener
            CustomButton.onClick = new UnityEvent(); // clears all listeners
            CustomButton.onClick.AddListener(() =>
            {
                Logger.LogInfo("Toggling Local Translation Mode.");

                UseLocalMode = !UseLocalMode;

                SetRotationToLocalMode();
            });
        }

        Destroy(ToggleLocalTranslationButton.GetComponent<LEV_HotkeyButton>());

        if (_allSprites == null || _allSprites?.Length == 0)
        {
            _allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var sprite in _allSprites)
                if (_spritesStr.Contains(sprite.name))
                    _sprites.TryAdd(sprite.name, sprite);
        }


        _toggleLocalTranslationImage.sprite =
            !UseLocalMode ? _sprites["Pivot_Average"] : _sprites["Pivot_LastSelected"];


        ToggleLocalTranslationButton.SetActive(false);

        if (!ToggleLabel) ToggleLabel = Instantiate(_baseLabel, _baseLabel.transform.parent);

        ToggleLabel.name = "Local Translation Label";

        ToggleLabel.GetComponent<TextMeshProUGUI>().text = "Global Translation";

        ToggleLabel.SetActive(false);
    }

    internal void SetRotationToLocalMode()
    {
        if (!LevelEditorCentral)
        {
            Logger.LogError("LEV_LevelEditorCentral is not initialized.");
            return;
        }

        var translationGizmos = LevelEditorCentral.gizmos.translationGizmos;
        if (translationGizmos)
        {
            // Set gizmo rotation based on local mode
            if (UseLocalMode)
            {
                var selectionList = LevelEditorCentral.selection.list;
                switch (selectionList.Count)
                {
                    // Logger.LogWarning("No objects selected for local translation mode.");
                    case 0:
                        return;
                    case > 1:
                        // Logger.LogWarning("Multiple objects selected, local translation mode may not work as expected.");
                        break;
                }

                // Get the last selected object's transform
                var selectedTransform = selectionList[^1].transform;
                // Set gizmos to local mode based on the selected object's transform
                translationGizmos.transform.localRotation =
                    selectedTransform.rotation; // Use the selected object's rotation

                _toggleLocalTranslationImage.sprite = _sprites["Pivot_LastSelected"];

                // Logger.LogInfo("TranslationGizmos set to local mode based on selected object.");
            }
            else
            {
                // Set gizmos to world mode
                translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0); // Reset rotation to world space

                _toggleLocalTranslationImage.sprite = _sprites["Pivot_Average"];

                // Logger.LogInfo("TranslationGizmos set to world mode.");
            }
        }
        else
        {
            Logger.LogWarning("TranslationGizmos not found");
        }
    }
}

// SelectDrag
[HarmonyPatch(typeof(LEV_GizmoHandler), "SelectDrag")]
internal class PatchSelectDragLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (Plugin.Instance.UseLocalMode && Plugin.Instance.IsModEnabled) Plugin.Instance.SetRotationToLocalMode();
        if (!Plugin.Instance.IsModEnabled) return;
        Plugin.Instance.ToggleLocalTranslationButton.SetActive(true);
        Plugin.Instance.ToggleLabel.SetActive(true);
    }
}

// SelectRotate
[HarmonyPatch(typeof(LEV_GizmoHandler), "SelectRotate")]
internal class PatchSelectRotateLocalTranslation
{
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (!Plugin.Instance.IsModEnabled) return;
        Plugin.Instance.ToggleLocalTranslationButton.SetActive(false);
        Plugin.Instance.ToggleLabel.SetActive(false);
    }
}

// ResetRotation
[HarmonyPatch(typeof(LEV_GizmoHandler), "ResetRotation")]
internal class PatchResetRotationLocalRotation
{
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (Plugin.Instance.UseLocalMode && Plugin.Instance.IsModEnabled) Plugin.Instance.SetRotationToLocalMode();
    }
}

// Activate
[HarmonyPatch(typeof(LEV_GizmoHandler), "Activate")]
internal class PatchActivateLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (Plugin.Instance.UseLocalMode && Plugin.Instance.IsModEnabled) Plugin.Instance.SetRotationToLocalMode();
    }
}

// GizmoJustGotClicked
[HarmonyPatch(typeof(LEV_GizmoHandler), "GizmoJustGotClicked")]
internal class PatchGizmoJustGotClickedLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (Plugin.Instance.UseLocalMode && Plugin.Instance.IsModEnabled)
            // Reset the last mouse position to the current mouse position
            PatchDragGizmoLocalTranslation.lastAxisPoint = null;
    }
}

// CreateNewBlock
[HarmonyPatch(typeof(LEV_GizmoHandler), "CreateNewBlock")]
internal class PatchCreatingNewBlockLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalMode || !Plugin.Instance.IsModEnabled) return;
        // Reset translationGizmos rotation
        __instance.translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
}

// Deactivate
[HarmonyPatch(typeof(LEV_GizmoHandler), "Deactivate")]
internal class PatchDeactivateLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalMode || !Plugin.Instance.IsModEnabled) return;
        // Reset translationGizmos rotation
        __instance.translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
}

// AddThisBlock
[HarmonyPatch(typeof(LEV_Selection), "AddThisBlock")]
internal class PatchAddThisBlockLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_Selection __instance)
    {
        if (!Plugin.Instance.UseLocalMode || !Plugin.Instance.IsModEnabled) return;
        // Reset translationGizmos rotation
        __instance.central.gizmos.translationGizmos.transform.localRotation =
            __instance.central.selection.list[^1].transform.localRotation;
    }
}

// DragGizmo
[HarmonyPatch(typeof(LEV_GizmoHandler), "DragGizmo")]
internal class PatchDragGizmoLocalTranslation
{
    private const float MaxDistance = 1500f;
    internal static Vector3? lastAxisPoint;
    private static Vector3? _initialDragOffset = Vector3.zero;

    private static readonly List<string> PlaneNames = ["xy", "yz", "xz"];
    private static readonly List<string> AxisNames = ["x", "y", "z"];

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalMode || !Plugin.Instance.IsModEnabled) return true;

        var currentGizmo = __instance.currentGizmo;
        if (!currentGizmo)
        {
            Plugin.logger.LogWarning("No current gizmo found during DragGizmo call.");
            return false;
        }

        var selection = Plugin.Instance.LevelEditorCentral?.selection;

        if (!selection)
        {
            Plugin.logger.LogWarning("No selection found during DragGizmo call.");
            return false;
        }

        var selectionList = selection.list;
        var lastSelected = selectionList[^1];
        var selectionMiddlePivot = Plugin.Instance.LevelEditorCentral?.gizmos?.motherGizmo?.transform.position;

        if (selectionList.Count == 0)
        {
            Plugin.logger.LogWarning("No objects selected for local translation.");
            return false;
        }

        if (currentGizmo.name.Contains("R")) return true;

        var gizmoName = currentGizmo.name.Replace("Gizmo", "").ToLower();

        if (!Camera.main) return false;

        var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        var dragPlane = GetPlaneFromGizmo(lastSelected, mouseRay, gizmoName);

        if (dragPlane == null) return false;

        // Axis gizmos (X, Y, Z)

        if (AxisNames.Any(gizmoName.Equals))
        {
            if (!dragPlane.Value.Plane.Raycast(mouseRay, out var enter) || !(enter <= MaxDistance)) return false;

            var axis = GetTranslationVector(lastSelected, gizmoName);

            if (axis == null)
            {
                Plugin.logger.LogWarning($"Gizmo {gizmoName} does not have a valid translation vector.");
                return false;
            }

            var hitPoint = mouseRay.GetPoint(enter);

            // On initial click, store offset between pivot and where mouse hit the plane
            if (!lastAxisPoint.HasValue)
            {
                lastAxisPoint = selectionMiddlePivot;

                // Store offset for the rest of the drag
                if (selectionMiddlePivot != null) _initialDragOffset = hitPoint - selectionMiddlePivot.Value;
                return false; // Don't apply movement on initial click
            }

            if (_initialDragOffset == null) return false;

            var localOffset = hitPoint - _initialDragOffset.Value;

            var closestPoint = Vector3.Project(localOffset, axis.Value);

            var moveDir = closestPoint - lastAxisPoint.Value;

            var gridStep = gizmoName.Contains("y") ? __instance.gridY : __instance.gridXZ;
            var distance = Vector3.Dot(moveDir, axis.Value);
            var snappedDistance = gridStep > 0f ? Mathf.Round(distance / gridStep) * gridStep : distance;
            var snappedMove = axis.Value * snappedDistance;

            // Apply movement
            foreach (var obj in selectionList)
                obj.transform.position += snappedMove;

            __instance.motherGizmo.transform.position += snappedMove;

            // Advance lastAxisPoint by how far we actually moved
            lastAxisPoint = lastAxisPoint.Value + snappedMove;

            if (__instance.motherGizmo.transform.position != __instance.rememberTranslation && gridStep > 0f)
            {
                AudioEvents.MenuHover1.Play(null);
                __instance.rememberTranslation = __instance.motherGizmo.transform.position;
            }

            __instance.central.validation.BreakLock(false, null, "Gizmo11", false);

            return false;
        }

        // Plane gizmos (XY, YZ, XZ)

        if (!PlaneNames.Any(gizmoName.Contains)) return false;
        {
            if (dragPlane.Value.Plane.Raycast(mouseRay, out var enter) && enter <= MaxDistance)
            {
                Vector3[] axes = [dragPlane.Value.Axis1.normalized, dragPlane.Value.Axis2.normalized];

                var hitPoint = mouseRay.GetPoint(enter);

                // On initial click, store offset between pivot and where mouse hit the plane
                if (!lastAxisPoint.HasValue)
                {
                    lastAxisPoint = selectionMiddlePivot;

                    // Store offset for the rest of the drag
                    if (selectionMiddlePivot != null) _initialDragOffset = hitPoint - selectionMiddlePivot.Value;
                    return false; // Don't apply movement on initial click
                }

                if (_initialDragOffset == null) return false;

                var localOffset = hitPoint - _initialDragOffset.Value;

                var rawMove = localOffset - lastAxisPoint.Value;

                var gotSnapped = false;

                var snappedMove = Vector3.zero;

                for (var i = 0; i < axes.Length; i++)
                {
                    var axis = axes[i];

                    // Determine a grid step based on the axis and gizmo name
                    var gridStep =
                        (gizmoName.Contains("xy") && i == 1) || // Y-axis in XY plane
                        (gizmoName.Contains("yz") && i == 0) // Y-axis in YZ plane
                            ? __instance.gridY
                            : __instance.gridXZ;

                    var moveAmount = Vector3.Dot(rawMove, axis);
                    var snapped = gridStep > 0f ? Mathf.Round(moveAmount / gridStep) * gridStep : moveAmount;

                    gotSnapped = (snapped != moveAmount) || gotSnapped;

                    snappedMove += axis * snapped;
                }

                foreach (var obj in selectionList)
                    obj.transform.position += snappedMove;

                __instance.motherGizmo.transform.position += snappedMove;

                lastAxisPoint = lastAxisPoint.Value + snappedMove;

                if (__instance.motherGizmo.transform.position != __instance.rememberTranslation && gotSnapped)
                {
                    AudioEvents.MenuHover1.Play(null);
                    __instance.rememberTranslation = __instance.motherGizmo.transform.position;
                }

                return false;
            }
            return false;
        }
    }

    private static Vector3? GetTranslationVector(BlockProperties reference, string name)
    {
        if (name.Contains("x") && !name.Contains("y") && !name.Contains("z"))
            return reference.transform.right;
        if (name.Contains("y") && !name.Contains("x") && !name.Contains("z"))
            return reference.transform.up;
        if (name.Contains("z") && !name.Contains("x") && !name.Contains("y"))
            return reference.transform.forward;

        return null;
    }

    private static DragPlane? GetPlaneFromGizmo(BlockProperties reference, Ray mouseRay, string name)
    {
        var ray = mouseRay; // Use the mouse ray for intersection checks

        // Plane handles (XY, XZ, YZ)
        if (name.Contains("xy"))
            return new DragPlane(new Plane(reference.transform.forward, reference.transform.position),
                reference.transform.right, reference.transform.up);
        if (name.Contains("xz"))
            return new DragPlane(new Plane(reference.transform.up, reference.transform.position),
                reference.transform.right, reference.transform.forward);
        if (name.Contains("yz"))
            return new DragPlane(new Plane(reference.transform.right, reference.transform.position),
                reference.transform.up, reference.transform.forward);

        // Axis handles (X, Y, Z)
        Vector3 axis;
        switch (name)
        {
            case "x":
                axis = reference.transform.right;
                break;
            case "y":
                axis = reference.transform.up;
                break;
            case "z":
                axis = reference.transform.forward;
                break;
            default:
                Plugin.logger.LogWarning($"Unknown gizmo name: {name}");
                return null;
        }

        // Use vector from camera to object as a stable basis
        if (!Camera.main) return null;

        var toObject = (reference.transform.position - Camera.main.transform.position).normalized;

        // Make a plane perpendicular to axis and facing the camera
        var planeNormal = Vector3.Cross(axis, toObject.normalized).normalized;
        if (planeNormal == Vector3.zero)
        {
            Plugin.logger.LogWarning($"Plane normal is zero for gizmo {name}. Cannot create drag plane.");
            return null;
        }

        planeNormal = Vector3.Cross(planeNormal, axis).normalized;

        var dot = Mathf.Abs(Vector3.Dot(ray.direction.normalized, planeNormal));
        if (dot < 0.1f)
            // Plugin.logger.LogWarning($"Plane is nearly parallel to mouse ray (dot = {dot}) gizmo {name}. Skipping drag.");
            return null;

        return new DragPlane(new Plane(planeNormal, reference.transform.position), Vector3.zero, Vector3.zero);
    }


    private struct DragPlane(Plane plane, Vector3 axis1, Vector3 axis2)
    {
        public readonly Plane Plane = plane;
        public readonly Vector3 Axis1 = axis1; // Local "right" axis
        public readonly Vector3 Axis2 = axis2; // Local "up" axis
    }
}

[HarmonyPatch(typeof(LEV_MotherGizmoFlipper), "Update")]
public class PatchLevMotherGizmoFlipperUpdateLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_MotherGizmoFlipper __instance)
    {
        if (!Plugin.Instance.UseLocalMode || !Plugin.Instance.IsModEnabled) return true;

        if (__instance.central.gizmos.isDragging)
            return false;

        var t = __instance.t;
        var camTransform = __instance.central.cam.cameraTransform;
        var xyzFlip = __instance.xyzFlip;

        var localCamPos = t.InverseTransformPoint(camTransform.position);
        xyzFlip.localScale = new Vector3(
            Mathf.Sign(localCamPos.x),
            Mathf.Sign(localCamPos.y),
            Mathf.Sign(localCamPos.z)
        );

        return false; // Skip original Update logic
    }
}

[HarmonyPatch(typeof(LEV_GizmoHandler), "DisableGizmosOnDistance")]
public static class PatchDisableGizmosOnDistanceLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalMode || !Plugin.Instance.IsModEnabled) return true;

        if (Camera.main)
        {
            var camTransform = Camera.main.transform;
            var gizmoRoot = __instance.translationGizmos.transform;

            // Calculate a view direction in local gizmo space
            var localViewDir = (gizmoRoot.InverseTransformPoint(camTransform.position) -
                                gizmoRoot.InverseTransformPoint(gizmoRoot.position)).normalized;

            // Thresholds
            const float axisDotThreshold = 0.98f; // axis disappears if the view is almost parallel to axis
            const float planeDotThreshold = 0.05f; // plane disappears if view is nearly perpendicular to the plane

            // Axis gizmos: disable if camera is looking *along* the axis
            SetGizmoActive(__instance.Xgizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.right)) < axisDotThreshold);
            SetGizmoActive(__instance.Ygizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.up)) < axisDotThreshold);
            SetGizmoActive(__instance.Zgizmo, Mathf.Abs(Vector3.Dot(localViewDir, Vector3.forward)) < axisDotThreshold);

            // Plane gizmos: disable if the camera is looking *edge-on* to the plane (aligned with the plane's normal)
            SetGizmoActive(__instance.XYgizmo,
                Mathf.Abs(Vector3.Dot(localViewDir, Vector3.forward)) > planeDotThreshold); // Z normal
            SetGizmoActive(__instance.YZgizmo,
                Mathf.Abs(Vector3.Dot(localViewDir, Vector3.right)) > planeDotThreshold); // X normal
            SetGizmoActive(__instance.XZgizmo,
                Mathf.Abs(Vector3.Dot(localViewDir, Vector3.up)) > planeDotThreshold); // Y normal
        }

        // Keep rotation gizmo logic as-is, based on an original distance system
        __instance.DisableOrNotIndividualGizmo(__instance.RXgizmo, __instance.RXdist);
        __instance.DisableOrNotIndividualGizmo(__instance.RYgizmo, __instance.RYdist);
        __instance.DisableOrNotIndividualGizmo(__instance.RZgizmo, __instance.RZdist);

        return false; // skip original method
    }

    private static void SetGizmoActive(LEV_SingleGizmo gizmo, bool active)
    {
        if (!gizmo) return;

        switch (active)
        {
            case true when !gizmo.gameObject.activeSelf:
                gizmo.gameObject.SetActive(true);
                break;
            case false when gizmo.gameObject.activeSelf:
                gizmo.gameObject.SetActive(false);
                break;
        }
    }
}

[HarmonyPatch(typeof(LEV_CustomButton), "SetToDefaultColor")]
public static class PatchSetToDefaultColorLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_CustomButton __instance)
    {
        if (!Plugin.Instance.UseLocalMode || !Plugin.Instance.IsModEnabled) return;

        // Only apply the warning logic to the specific button you care about
        if (__instance != Plugin.Instance.CustomButton)
            return;

        var selectionList = Plugin.Instance.LevelEditorCentral?.selection?.list;

        switch (selectionList)
        {
            case { Count: 0 }:
            // Get the last selected object's transform
            case null:
                return; // No objects selected
        }

        var selectedBlock = selectionList[^1];

        if (!selectedBlock || !Plugin.Instance.CustomButton) return;

        // Compare rotation against global alignment
        var blockRot = selectedBlock.transform.rotation;
        var isAligned = Quaternion.Angle(blockRot, Quaternion.identity) < 1f;

        Plugin.Instance.CustomButton.buttonImage.color = isAligned ? Plugin.NormalColor : Plugin.WarningColor;
    }
}

// SetMotherPosition
[HarmonyPatch(typeof(LEV_GizmoHandler), "SetMotherPosition")]
public static class PatchSetMotherPositionLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (Plugin.Instance.UseLocalMode && Plugin.Instance.IsModEnabled)
            // Reset the last mouse position to the current mouse position
            PatchDragGizmoLocalTranslation.lastAxisPoint = null;
    }
}

// GrabGizmo
[HarmonyPatch(typeof(LEV_GizmoHandler), nameof(LEV_GizmoHandler.GrabGizmo))]
public static class PatchGrabGizmoLocalTranslation
{
    internal static Vector3? lastAxisPoint;
    private static readonly float MaxDistance = 1500;

    static bool Prefix(LEV_GizmoHandler __instance)
    {

        if (!Plugin.Instance.UseLocalMode || !Plugin.Instance.IsModEnabled) return true;

        // Safety check
        if (!Plugin.Instance.referenceBlock) return true;

        // Get the first selected block
        Transform selectedTransform = Plugin.Instance.referenceBlock;
        Vector3 planeNormal = selectedTransform.up;
        Vector3 planeOrigin = selectedTransform.position;
        Vector3[] PlaneAxes = [selectedTransform.right, selectedTransform.forward];

        // Create the custom drag plane
        Plane dragPlane = new Plane(planeNormal, planeOrigin);

        // Cast a ray from the mouse to the plane
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!dragPlane.Raycast(mouseRay, out var enter) || !(enter <= MaxDistance)) return false;

        var hitPoint = mouseRay.GetPoint(enter);

        // On initial click, store offset between pivot and where mouse hit the plane
        if (__instance.newGizmo)
        {
            __instance.central.selection.TranslatePositions(selectedTransform.position - __instance.central.selection.list[^1].transform.position);

            __instance.SetMotherPosition(selectedTransform.position);

            lastAxisPoint = selectedTransform.position;

            // Match rotation to the selected block
            // Perform rotation
            Quaternion currentRotation = __instance.central.selection.list[^1].transform.rotation;
            Quaternion targetRotation = selectedTransform.rotation;
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);

            // Convert quaternion delta to axis + angle
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            __instance.central.rotflip.RotateBlocks(axis, angle, __instance.central.selection.list[^1].transform.position);
            __instance.central.gizmos.ResetRotationGizmoRotation();

            __instance.newGizmo = false;

            Plugin.logger.LogInfo($"Local translation initialized with reference block: {selectedTransform.name} " +
                $"at position {selectedTransform.position} with rotation {selectedTransform.rotation}");

        }

        var localOffset = hitPoint;

        var rawMove = localOffset - lastAxisPoint.Value;

        var snappedMove = Vector3.zero; 

        float yScroll = SetYgridStep(__instance);
        if (Mathf.Abs(yScroll) > 0f)
        {
            var scrollOffset = planeNormal * yScroll;
            snappedMove += scrollOffset;

            // Camera.main.transform.position += scrollOffset;
        }

        var xygridStep = __instance.gridXZ;
        

        for (var i = 0; i < PlaneAxes.Length; i++)
        {
            var axis = PlaneAxes[i];

            // Determine a grid step based on the axis and gizmo name
            var moveAmount = Vector3.Dot(rawMove, axis);
            var snapped = xygridStep > 0f ? Mathf.Round(moveAmount / xygridStep) * xygridStep : moveAmount;
            snappedMove += axis * snapped;
        }

        // Apply movement to the selected block

        __instance.motherGizmo.transform.position += snappedMove;
        __instance.dragGizmoOrigin = __instance.motherGizmo.position;
        __instance.central.selection.TranslatePositions(snappedMove);

        if (__instance.motherGizmo.transform.position != __instance.rememberTranslation && xygridStep > 0f)
        {
            AudioEvents.MenuHover1.Play(null);
            __instance.rememberTranslation = __instance.motherGizmo.transform.position;
        }

        lastAxisPoint = lastAxisPoint.Value + snappedMove;

        return false;
    }

    private static float SetYgridStep(LEV_GizmoHandler __instance)
    {
        float yGridStep = (__instance.gridY == 0f) ? __instance.list_gridY[^1] : __instance.gridY;

        if (__instance.central.input.GizmoGridVertical.positiveButtonDown && !__instance.central.input.inputLocked)
        {
            return yGridStep;
        }
        else if (__instance.central.input.GizmoGridVertical.negativeButtonDown && !__instance.central.input.inputLocked)
        {
            return -yGridStep;
        }

        return 0f;
    }

}

// Update Gizmo_Handler
[HarmonyPatch(typeof(LEV_GizmoHandler), "Update")]
public static class PatchGizmoHandlerUpdateLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (__instance.central.selection.list.Count == 0) return;

        if (Plugin.Instance.UseLocalMode && Plugin.Instance.IsModEnabled && __instance.isGrabbing && !__instance.central.selection.list[^1].placeDynamic)
        {
            // If the gridHeightHelper is not active, activate it
            __instance.gridHeightHelper.gameObject.SetActive(true);

            __instance.gridHeightHelper.position = __instance.central.selection.list[^1].transform.position;
            __instance.gridHeightHelper.rotation = __instance.central.selection.list[^1].transform.rotation;

        }
        else if (__instance.central.selection.list.Count == 1 && !__instance.central.selection.list[0].placeDynamic)
        {
            __instance.gridHeightHelper.position = new Vector3(__instance.motherGizmo.position.x, __instance.newBlockHeight - 4f, __instance.motherGizmo.position.z);
            __instance.gridHeightHelper.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}


public class ModConfig : MonoBehaviour
{
    public static ConfigEntry<KeyCode> toggleMode;
    public static ConfigEntry<KeyCode> setReference;


    // Constructor that takes a ConfigFile instance from the main class
    public static void Initialize(ConfigFile config)
    {
        toggleMode = config.Bind("1. Keybinds", "1.1 Toggle Global/Local", KeyCode.L,
            "Key to Toggle Global/Local translation");

        setReference = config.Bind("1. Keybinds", "1.2 Set Reference Block", KeyCode.J,
            "Key to set the reference block");
    }
}