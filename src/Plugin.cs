using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FMODSyntax;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.UIElements.StylePropertyAnimationSystem;

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
    internal bool UseLocalTranslationMode;
    internal bool UseLocalGridMode;

    internal GameObject referenceBlockObject;
    private Transform referenceBlock;
    private readonly float sizeOnScreen = 0.17f; // world‐units per unit of distance 
    private readonly float maxReferenceSize = 6f; // maximum size of the reference block in world units

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
        if (Input.GetKeyDown(ModConfig.toggleMode.Value) && !LevelEditorCentral.input.inputLocked && 
            !LevelEditorCentral.gizmos.isGrabbing && LevelEditorCentral.selection.list.Count != 0)
        {
            UseLocalTranslationMode = !UseLocalTranslationMode;
            Logger.LogInfo($"Local Translation Mode: {(UseLocalTranslationMode ? "Enabled" : "Disabled")}");

            SetRotationToLocalMode();
        }


        if (Input.GetKeyDown(ModConfig.setReference.Value) && !LevelEditorCentral.input.inputLocked && !LevelEditorCentral.gizmos.isGrabbing)
        {
            if (LevelEditorCentral.selection.list.Count == 0)
            {
                if (referenceBlockObject != null)
                {
                    PlayerManager.Instance.messenger.Log("[LocTrans] Reference Block removed", 5);
                    Destroy(referenceBlockObject);
                    referenceBlock = null;
                    UseLocalGridMode = false;

                    logger.LogWarning("[LocTrans] Reference Block removed, local grid mode deactivated.");
                }
                else
                {
                    PlayerManager.Instance.messenger.LogCustomColor("[LocTrans] No blocks selected to set as reference", 5, Color.black, new Color(1f, 0.98f, 0.29f, 0.9f));
                }
                return;
            }

            if (referenceBlock != null)
            {
                if(LevelEditorCentral.selection.list[^1].transform == referenceBlock)
                {
                    PlayerManager.Instance.messenger.Log("[LocTrans] Reference Block removed", 5);
                    Destroy(referenceBlockObject);
                    referenceBlock = null;
                    UseLocalGridMode = false;
                    logger.LogWarning("[LocTrans] Reference Block removed, local grid mode deactivated.");
                    return;
                }
            }

            if (!UseLocalTranslationMode)
            {
                UseLocalTranslationMode = true;
                SetRotationToLocalMode();
            }

            UseLocalGridMode = true;

            referenceBlock = LevelEditorCentral.selection.list[^1].transform;

            if (referenceBlockObject == null)
                CreateReferenceBlockObject(referenceBlock);

            referenceBlockObject.SetActive(true);
            
            PlayerManager.Instance.messenger.Log("[LocTrans] Reference Block set, local translation mode activated", 5);
        }
    }

    void CreateReferenceBlockObject(Transform source)
    {
        if (referenceBlockObject == null)
        {
            referenceBlockObject = CreateReferenceGizmo();
        }

        referenceBlockObject.transform.position = source.position;
        referenceBlockObject.transform.rotation = source.rotation;
        referenceBlockObject.SetActive(true);
    }

    GameObject CreateReferenceGizmo()
    {
        GameObject gizmoRoot = new GameObject("ReferenceGizmo");

        void CreateDisc(string name, Vector3 normal, Color color, float scaler)
        {
            // Outline
            var outline = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outline.name = name + "_Outline";
            outline.transform.SetParent(gizmoRoot.transform, false);
            outline.transform.localScale = new Vector3(1.1f*scaler, 0.03f, 1.1f * scaler); // 1 unit + tiny bit
            outline.transform.localPosition = Vector3.zero;
            outline.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            Destroy(outline.GetComponent<Collider>());

            var oMat = outline.GetComponent<Renderer>().material;
            oMat.shader = Shader.Find("Unlit/Color");
            oMat.color = Color.black;

            // Inner
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.transform.SetParent(gizmoRoot.transform, false);
            disc.transform.localScale = new Vector3(1f * scaler, 0.05f, 1f * scaler);
            disc.transform.localPosition = normal * 0.01f;
            disc.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            Destroy(disc.GetComponent<Collider>());

            var dMat = disc.GetComponent<Renderer>().material;
            dMat.shader = Shader.Find("Unlit/Color");
            dMat.color = color;
        }



        // Create one disc for each plane
        CreateDisc("Disc_X", Vector3.right, Color.red, 1f);   // YZ plane
        CreateDisc("Disc_Y", Vector3.up, Color.green, 1.01f);    // XZ plane
        CreateDisc("Disc_Z", Vector3.forward, Color.blue, 1.02f); // XY plane

        gizmoRoot.layer = 14;

        // Also apply the layer to all children (Unity doesn't do this recursively)
        foreach (Transform child in gizmoRoot.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = 14;
        }

        gizmoRoot.SetActive(false);
        return gizmoRoot;
    }

    void LateUpdate()
    {
        if (referenceBlockObject != null && referenceBlock != null)
        {
            // match position & rotation
            referenceBlockObject.transform.position = referenceBlock.position;
            referenceBlockObject.transform.rotation = referenceBlock.rotation;

            // scale so that screen‐space size stays roughly constant
            Camera cam = Camera.main;
            if (cam != null)
            {
                float dist = Vector3.Distance(cam.transform.position, referenceBlockObject.transform.position);
                float uniformScale = Mathf.Min(dist * sizeOnScreen,maxReferenceSize);
                referenceBlockObject.transform.localScale = Vector3.one * uniformScale;
            }
        }
        else
        {
            Destroy(referenceBlockObject?.gameObject);
            referenceBlockObject = null;
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
            UseLocalGridMode = false; // Default to global translation mode
            // Logger.LogInfo("Level Editor scene loaded — mod activated.");
        }
        else
        {
            IsModEnabled = false;
            // UseLocalMode = false; // Reset local mode when not in a Level Editor scene
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

                UseLocalTranslationMode = !UseLocalTranslationMode;

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
            !UseLocalTranslationMode ? _sprites["Pivot_Average"] : _sprites["Pivot_LastSelected"];


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
            if (UseLocalTranslationMode)
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

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled) Plugin.Instance.SetRotationToLocalMode();
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

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled) Plugin.Instance.SetRotationToLocalMode();
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

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled) Plugin.Instance.SetRotationToLocalMode();
    }
}

// ActiveAllGizmos
[HarmonyPatch(typeof(LEV_GizmoHandler), "ActiveAllGizmos")]
internal class PatchActiveAllGizmosLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));
        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled)
            Plugin.Instance.SetRotationToLocalMode();
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

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled)
        {
            // Reset the last mouse position to the current mouse position
            PatchDragGizmoLocalTranslation.originPosition = null;
            PatchDragGizmoLocalTranslation._initialDragOffset = null;
        }
    }
            
}

// GizmoJustGotReleased
[HarmonyPatch(typeof(LEV_GizmoHandler), "GizmoJustGotReleased")]
internal class PatchGizmoJustGotReleasedLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return;
        PatchDragGizmoLocalTranslation.ClearOriginMarker();
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
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return;
        // Reset translationGizmos rotation
        __instance.translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
}


// DragGizmo
[HarmonyPatch(typeof(LEV_GizmoHandler), "DragGizmo")]
internal class PatchDragGizmoLocalTranslation
{
    private const float MaxDistance = 1500f;
    internal static Vector3? _initialDragOffset;
    internal static Vector3? originPosition;
    private static DragPlane? dragPlane;
    private static bool gotSnapped = false;
    private static List<float> gridValues = [];
    private static Vector3? lastSnappedPosition = null;

    private static readonly List<string> PlaneNames = ["xy", "yz", "xz"];
    private static readonly List<string> AxisNames = ["x", "y", "z"];

    private static GameObject _originMarker;

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return true;

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

        if (selectionList.Count == 0)
        {
            Plugin.logger.LogWarning("No objects selected for local translation.");
            return false;
        }

        if (currentGizmo.name.Contains("R")) return true;

        var gizmoName = currentGizmo.name.Replace("Gizmo", "").ToLower();

        if (!Camera.main) return false;

        var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        var motherGizmo = Plugin.Instance.LevelEditorCentral?.gizmos?.motherGizmo;
        var rotationGizmo = Plugin.Instance.LevelEditorCentral?.gizmos?.rotationGizmos.transform;

        if (!originPosition.HasValue)
        {
            originPosition = motherGizmo.position;
            if (originPosition != null)
            {
                // Spawn the visual indicator
                CreateOriginMarker(originPosition.Value);
                dragPlane = GetPlaneFromGizmo(motherGizmo, rotationGizmo, mouseRay, gizmoName);

                gotSnapped = false;


            }            
        }

        if (dragPlane == null) return false;

        if (!dragPlane.Value.Plane.Raycast(mouseRay, out var enter) || !(enter <= MaxDistance)) return false;

        var hitPoint = mouseRay.GetPoint(enter);

        // On initial click, store offset between pivot and where mouse hit the plane
        if (!_initialDragOffset.HasValue)
        {
            _initialDragOffset = hitPoint - originPosition.Value;
            return false;
        }

        var localOffset = hitPoint - _initialDragOffset.Value;

        var snappedMove = Vector3.zero;

        gridValues = [];

        // Axis gizmos (X, Y, Z)

        if (AxisNames.Any(gizmoName.Equals))
        {
            var axis = GetTranslationVector(rotationGizmo, gizmoName);

            if (axis == null)
            {
                Plugin.logger.LogWarning($"Gizmo {gizmoName} does not have a valid translation vector.");
                return false;
            }

            var gridStep = gizmoName.Contains("y") ? __instance.gridY : __instance.gridXZ;

            var closestPoint = Vector3.Project(localOffset, axis.Value);

            var moveDir = closestPoint - originPosition.Value;

            var distance = Vector3.Dot(moveDir, axis.Value);
            var snappedDistance = gridStep > 0f ? Mathf.Round(distance / gridStep) * gridStep : distance;
            snappedMove = axis.Value * snappedDistance;

            gotSnapped = gridStep > 0f;

            gridValues.Add(gridStep);
        }

        // Plane gizmos (XY, YZ, XZ)

        else if (PlaneNames.Any(gizmoName.Equals))
        {
            Vector3[] axes = [dragPlane.Value.Axis1.normalized, dragPlane.Value.Axis2.normalized];

            var rawMove = localOffset - originPosition.Value;

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
                if (gridStep > 0f && snapped != moveAmount)
                {
                    gotSnapped = true;
                    gridValues.Add(gridStep);
                }

                snappedMove += axis * snapped;
            }
        }

        // Apply movement
        foreach (var obj in selectionList)
            obj.transform.position = originPosition.Value + snappedMove + (obj.transform.position - __instance.motherGizmo.position);

        __instance.motherGizmo.position = originPosition.Value + snappedMove;

        __instance.central.validation.BreakLock(false, null, "Gizmo11", false);

        // Play Sound
        float minNonZero = gridValues.Where(v => v > 0).DefaultIfEmpty(float.MaxValue).Min();

        if (gotSnapped && (!lastSnappedPosition.HasValue || Vector3.Distance(__instance.motherGizmo.position, lastSnappedPosition.Value) >= minNonZero))
        {
            AudioEvents.MenuHover1.Play(null);
        }
        lastSnappedPosition = __instance.motherGizmo.position;

        return false;
    }

    private static void CreateOriginMarker(Vector3 position)
    {
        if (_originMarker != null)
        {
            GameObject.Destroy(_originMarker);
        }

        _originMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _originMarker.transform.position = position;
        _originMarker.transform.localScale = Vector3.one * 3f; // Small sphere

        var renderer = _originMarker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(1f, 1f, 0f, 0.7f); // Yellow with alpha
            renderer.material.SetFloat("_Mode", 3); // Transparent mode
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.DisableKeyword("_ALPHATEST_ON");
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            renderer.material.renderQueue = 3000;

            _originMarker.layer = 14; // Set to Ignore Raycast layer
        }

        GameObject.Destroy(_originMarker.GetComponent<Collider>()); // Remove the collider
    }

    internal static void ClearOriginMarker()
    {
        if (_originMarker != null)
        {
            GameObject.Destroy(_originMarker);
            _originMarker = null;
        }

        originPosition = null;
        _initialDragOffset = null;
    }


    private static Vector3? GetTranslationVector(Transform reference, string name)
    {
        if (name.Contains("x") && !name.Contains("y") && !name.Contains("z"))
            return reference.right;
        if (name.Contains("y") && !name.Contains("x") && !name.Contains("z"))
            return reference.up;
        if (name.Contains("z") && !name.Contains("x") && !name.Contains("y"))
            return reference.forward;

        return null;
    }

    private static DragPlane? GetPlaneFromGizmo(Transform motherGizmo, Transform rotationGizmo, Ray mouseRay, string name)
    {
        var ray = mouseRay; // Use the mouse ray for intersection checks

        // Plane handles (XY, XZ, YZ)
        if (name.Contains("xy"))
            return new DragPlane(new Plane(rotationGizmo.forward, motherGizmo.position),
                rotationGizmo.right, rotationGizmo.up);
        if (name.Contains("xz"))
            return new DragPlane(new Plane(rotationGizmo.up, motherGizmo.position),
                rotationGizmo.right, rotationGizmo.forward);
        if (name.Contains("yz"))
            return new DragPlane(new Plane(rotationGizmo.right, motherGizmo.position),
                rotationGizmo.up, rotationGizmo.forward);

        // Axis handles (X, Y, Z)
        Vector3 axis;
        switch (name)
        {
            case "x":
                axis = rotationGizmo.right;
                break;
            case "y":
                axis = rotationGizmo.up;
                break;
            case "z":
                axis = rotationGizmo.forward;
                break;
            default:
                Plugin.logger.LogWarning($"Unknown gizmo name: {name}");
                return null;
        }

        // Use vector from camera to object as a stable basis
        if (!Camera.main) return null;

        var toObject = (motherGizmo.position - Camera.main.transform.position).normalized;

        // Make a plane perpendicular to the axis and facing the camera
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

        return new DragPlane(new Plane(planeNormal, motherGizmo.position), Vector3.zero, Vector3.zero);
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
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return true;

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
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return true;

        if (Camera.main)
        {
            var camTransform = Camera.main.transform;
            var gizmoRoot = __instance.translationGizmos.transform;

            // Calculate a view direction in a local gizmo space
            var localViewDir = (gizmoRoot.InverseTransformPoint(camTransform.position) -
                                gizmoRoot.InverseTransformPoint(gizmoRoot.position)).normalized;

            // Thresholds
            const float axisDotThreshold = 0.98f; // the axis disappears if the view is almost parallel to the axis
            const float planeDotThreshold = 0.05f; // plane disappears if view is nearly perpendicular to the plane

            // Axis gizmos: disable if the camera is looking *along* the axis
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
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return;

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

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled)
        {
            // Reset the last mouse position to the current mouse position
            PatchDragGizmoLocalTranslation.originPosition = null;
            PatchDragGizmoLocalTranslation._initialDragOffset = null;
        }
            
    }
}


// SnapToGridXY
[HarmonyPatch(typeof(LEV_GizmoHandler), "SnapToGridXZ")]
public static class PatchSnapToGridXZLocalTranslation
{
    [UsedImplicitly]
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled)
            return true;

        return LocalGridSnapUtils.SnapToLocalGrid(__instance, snapXZ: true, snapY: false);
    }
}


// SnapToGridY
[HarmonyPatch(typeof(LEV_GizmoHandler), "SnapToGridY")]
public static class PatchSnapToGridYLocalTranslation
{
    [UsedImplicitly]
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled)
            return true;

        return LocalGridSnapUtils.SnapToLocalGrid(__instance, snapXZ: false, snapY: true);
    }
}

// ResetRotation
[HarmonyPatch(typeof(LEV_GizmoHandler), "ResetRotation")]
public static class PatchResetRotationLocalTranslation
{
    [UsedImplicitly]
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled)
            return true;

        var selection = Plugin.Instance.LevelEditorCentral.selection;
        var selectedList = selection.list;

        Transform referenceTransform = Plugin.Instance.referenceBlockObject?.transform;

        if (selectedList.Count == 0 || referenceTransform == null || __instance.isGrabbing)
            return true;

        var before = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBlockListToJSONList(selectedList);

        // Match rotation to the selected block
        // Perform rotation
        Quaternion currentRotation = selectedList[^1].transform.rotation;
        Quaternion targetRotation = referenceTransform.rotation;
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);

        // Convert quaternion delta to axis + angle
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        __instance.central.rotflip.RotateBlocks(axis, angle, selectedList[^1].transform.position);
        __instance.central.gizmos.ResetRotationGizmoRotation();

        var after = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBlockListToJSONList(selectedList);
        var selectionStr = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertSelectionToStringList(selectedList);

        Plugin.Instance.LevelEditorCentral.validation.BreakLock(
            Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBeforeAndAfterListToCollection(before, after, selectedList, selectionStr, selectionStr),
            "Gizmo_LocalSnap"
        );

        return false; // Skip original method
    }
}

public static class LocalGridSnapUtils
{
    public static bool SnapToLocalGrid(LEV_GizmoHandler __instance, bool snapXZ, bool snapY)
    {
        var selection = Plugin.Instance.LevelEditorCentral.selection;
        var selectedList = selection.list;

        if (selectedList.Count == 0 || Plugin.Instance.referenceBlockObject == null || __instance.isGrabbing)
            return true;

        var refTransform = Plugin.Instance.referenceBlockObject.transform;

        float gridXZ = __instance.gridXZ != 0f ? __instance.gridXZ : __instance.list_gridXZ[^1];
        float gridY = __instance.gridY != 0f ? __instance.gridY : __instance.list_gridY[^1];

        var referencePosition = selectedList[^1].transform.position;
        var before = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBlockListToJSONList(selectedList);

        Quaternion rotation = refTransform.rotation;
        Vector3 position = refTransform.position;
        Matrix4x4 worldToLocal = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        Matrix4x4 localToWorld = Matrix4x4.TRS(position, rotation, Vector3.one);

        Vector3 localPos = worldToLocal.MultiplyPoint(referencePosition);

        float snappedX = snapXZ ? Mathf.Round(localPos.x / gridXZ) * gridXZ : localPos.x;
        float snappedY = snapY ? Mathf.Round(localPos.y / gridY) * gridY : localPos.y;
        float snappedZ = snapXZ ? Mathf.Round(localPos.z / gridXZ) * gridXZ : localPos.z;

        Vector3 snappedLocalPos = new(snappedX, snappedY, snappedZ);
        Vector3 snappedWorldPos = localToWorld.MultiplyPoint(snappedLocalPos);

        Vector3 delta = snappedWorldPos - referencePosition;

        Plugin.Instance.LevelEditorCentral.selection.TranslatePositions(delta);
        __instance.SetMotherPosition(__instance.motherGizmo.position + delta);

        var after = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBlockListToJSONList(selectedList);
        var selectionStr = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertSelectionToStringList(selectedList);

        Plugin.Instance.LevelEditorCentral.validation.BreakLock(
            Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBeforeAndAfterListToCollection(before, after, selectedList, selectionStr, selectionStr),
            "Gizmo_LocalSnap"
        );

        return false;
    }
}

// GrabGizmo
[HarmonyPatch(typeof(LEV_GizmoHandler), "GrabGizmo")]
public static class PatchGrabGizmoLocalTranslation
{
    internal static Vector3? lastAxisPoint;
    private static readonly float MaxDistance = 1500;
    private static Vector3 totalScrollOffset = Vector3.zero;

    static bool Prefix(LEV_GizmoHandler __instance)
    {

        if (!Plugin.Instance.UseLocalGridMode || !Plugin.Instance.IsModEnabled)
            return true;

        // Safety check
        if (Plugin.Instance.referenceBlockObject == null)
            return true;

        if (__instance.central.selection.list.Count == 0)
            return true;

        var lastSelectionPosition = __instance.central.selection.list[^1].transform.position;

        if (lastSelectionPosition == null)
        {
            lastSelectionPosition = Plugin.Instance.referenceBlockObject.transform.position;
        }

        // Get the reference transform
        Transform referenceTransform = Plugin.Instance.referenceBlockObject.transform;

        Vector3 planeNormal = referenceTransform.up;
        Vector3 planeOrigin = referenceTransform.position + totalScrollOffset;
        Vector3[] PlaneAxes = [referenceTransform.right, referenceTransform.forward];


        // Create the custom drag plane
        Plane dragPlane = new Plane(planeNormal, planeOrigin);

        // Cast a ray from the mouse to the plane
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 hitPoint;

        // Check if the ray intersects with the drag plane
        if (!dragPlane.Raycast(mouseRay, out var enter))
        {
            hitPoint = lastSelectionPosition;
        }

        else if (!(enter <= MaxDistance))
        {
            enter = MaxDistance;
            hitPoint = mouseRay.GetPoint(enter);
        }

        else
        {
            hitPoint = mouseRay.GetPoint(enter);
        }

        // On initial click, store offset between pivot and where mouse hit the plane
        if (__instance.newGizmo)
        {
            __instance.central.selection.TranslatePositions(referenceTransform.position - lastSelectionPosition);

            __instance.SetMotherPosition(referenceTransform.position);

            lastAxisPoint = referenceTransform.position;

            // Match rotation to the selected block
            // Perform rotation
            Quaternion currentRotation = __instance.central.selection.list[^1].transform.rotation;
            Quaternion targetRotation = referenceTransform.rotation;
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);

            // Convert quaternion delta to axis + angle
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            __instance.central.rotflip.RotateBlocks(axis, angle, __instance.central.selection.list[^1].transform.position);
            __instance.central.gizmos.ResetRotationGizmoRotation();

            totalScrollOffset = Vector3.zero;

            __instance.newGizmo = false;

            Plugin.logger.LogInfo($"Local translation initialized with reference block: {referenceTransform.name} " +
                $"at position {referenceTransform.position} with rotation {referenceTransform.rotation}");

        }

        var localOffset = hitPoint;

        var rawMove = localOffset - lastAxisPoint.Value;

        var snappedMove = Vector3.zero; 

        float yScroll = SetYgridStep(__instance);
        if (Mathf.Abs(yScroll) > 0f)
        {
            var scrollOffset = planeNormal * yScroll;
            snappedMove += scrollOffset;

            totalScrollOffset += scrollOffset;

            Camera.main.transform.position += scrollOffset;
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

        if (Plugin.Instance.UseLocalGridMode && Plugin.Instance.IsModEnabled && __instance.isGrabbing && !__instance.central.selection.list[^1].placeDynamic)
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
        toggleMode = config.Bind("1. Keybinds", "1.1 Toggle Global/Local Translation", KeyCode.Keypad1,
            "Key to Toggle Global/Local translation");

        setReference = config.Bind("1. Keybinds", "1.2 Set Reference Block", KeyCode.Keypad2,
            "Key to set the reference block");
    }
}