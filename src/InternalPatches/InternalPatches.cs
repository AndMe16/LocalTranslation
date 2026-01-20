using System;
using System.Collections.Generic;
using System.Linq;
using FMODSyntax;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LocalTranslation.InternalPatches;

// LEV_GizmoHandler_SelectDrag
[HarmonyPatch(typeof(LEV_GizmoHandler), "SelectDrag")]
internal class PatchSelectDragLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        Plugin.MyLogger.LogInfo("SelectDrag called in LEV_GizmoHandler.");

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled)
            Plugin.Instance.SetRotationToLocalMode();
        Plugin.Instance.ToggleLocalTranslationButton.SetActive(true);
        Plugin.Instance.ToggleLabel.SetActive(true);
    }
}

// LEV_GizmoHandler_SelectRotate
[HarmonyPatch(typeof(LEV_GizmoHandler), "SelectRotate")]
internal class PatchSelectRotateLocalTranslation
{
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (!Plugin.Instance.IsModEnabled) return;
        Plugin.MyLogger.LogInfo("SelectRotate called in LEV_GizmoHandler.");
        Plugin.Instance.ToggleLocalTranslationButton.SetActive(false);
        Plugin.Instance.ToggleLabel.SetActive(false);
    }
}

// LEV_GizmoHandler_ResetRotation
[HarmonyPatch(typeof(LEV_GizmoHandler), "ResetRotation")]
internal class PatchResetRotationLocalRotation
{
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled)
        {
            Plugin.MyLogger.LogInfo("ResetRotation called in LEV_GizmoHandler.");
            Plugin.Instance.SetRotationToLocalMode();
        }
    }
}

// LEV_GizmoHandler_GoOutOfGMode
[HarmonyPatch(typeof(LEV_GizmoHandler), "GoOutOfGMode")]
internal class PatchGoOutOfGModeLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled)
        {
            Plugin.MyLogger.LogInfo("GoOutOfGMode called in LEV_GizmoHandler.");
            Plugin.Instance.SetRotationToLocalMode();
        }
    }
}

// LEV_GizmoHandler_GizmoJustGotClicked
[HarmonyPatch(typeof(LEV_GizmoHandler), "GizmoJustGotClicked")]
internal class PatchGizmoJustGotClickedLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return;
        // Reset the last mouse position to the current mouse position
        Plugin.MyLogger.LogInfo("GizmoJustGotClicked called in LEV_GizmoHandler.");
        PatchDragGizmoLocalTranslation.OriginPosition = null;
        PatchDragGizmoLocalTranslation.InitialDragOffset = null;
    }
}

// LEV_GizmoHandler_GizmoJustGotReleased
[HarmonyPatch(typeof(LEV_GizmoHandler), "GizmoJustGotReleased")]
internal class PatchGizmoJustGotReleasedLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));


        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return;
        Plugin.MyLogger.LogInfo("GizmoJustGotReleased called in LEV_GizmoHandler.");
        PatchDragGizmoLocalTranslation.ClearOriginMarker();
    }
}

// LEV_GizmoHandler_ResetRotationGizmoRotation
// Safeguard against null references caused by other mods / vanilla bugs
[HarmonyPatch(typeof(LEV_GizmoHandler), "ResetRotationGizmoRotation")]
internal class PatchResetRotationGizmoRotationLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {

        if (__instance.rotationGizmos == null)
        {
            Plugin.MyLogger.LogWarning(
                "ResetRotationGizmoRotation called but rotationGizmos is NULL — skipping"
            );
            return false;
        }

        if (__instance.central?.selection?.list?.Count > 0)
        {
            for (int i = 0; i < __instance.central.selection.list.Count; i++)
            {
                if (__instance.central.selection.list[i] == null)
                {
                    Plugin.MyLogger.LogWarning(
                        $"Selection[{i}] is NULL — skipping ResetRotationGizmoRotation"
                    );
                    return false;
                }
            }
        }

        return true;
    }
}

// BlockProperties_ConvertBlockToJSON_v15
// Safeguard against null references caused by other mods / vanilla bugs
[HarmonyPatch(typeof(BlockProperties), "ConvertBlockToJSON_v15")]
internal class PatchConvertBlockToJSON_v15LocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(BlockProperties __instance)
    {
        // Destroyed or missing block
        if (__instance == null)
            return false;

        // Unity-style null (destroyed but not C# null)
        if (!__instance)
            return false;

        // GameObject or transform already gone
        if (__instance.transform == null)
            return false;

        return true;
    }
}


// LEV_GizmoHandler_ScaleGizmo
// Safeguard against null references caused by other mods / vanilla bugs
[HarmonyPatch(typeof(LEV_GizmoHandler),"ScaleGizmo")]
internal static class PatchScaleGizmoLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        // Camera guards (mirror vanilla intent)
        if (__instance.central?.cam?.cameraTransform == null)
            return false;

        if (__instance.central.cam.cameraCamera == null)
            return false;

        // Mother gizmo must exist AND not be destroyed
        if (__instance.motherGizmo == null || !__instance.motherGizmo)
            return false;

        var list = __instance.central.selection?.list;
        if (list == null || list.Count == 0)
            return false;

        // Any destroyed selection object will crash vanilla
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null || !list[i])
                return false;
        }

        return true;
    }
}


// LEV_GizmoHandler_DragGizmo
[HarmonyPatch(typeof(LEV_GizmoHandler), "DragGizmo")]
internal class PatchDragGizmoLocalTranslation
{
    private const float MaxDistance = 2500f;

    private const float SizeOnScreen = 0.08f;
    private const float MaxReferenceSize = 10f;
    internal static Vector3? InitialDragOffset;
    internal static Vector3? OriginPosition;
    private static DragPlane? _dragPlane;
    private static bool _gotSnapped;
    private static List<float> _gridValues = [];
    private static Vector3? _lastSnappedPosition;
    private static bool isTooFar = false;

    private static readonly List<string> PlaneNames = ["xy", "yz", "xz"];
    private static readonly List<string> AxisNames = ["x", "y", "z"];

    private static GameObject _originMarker;
    private static readonly int Mode = Shader.PropertyToID("_Mode");
    private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return true;

        var currentGizmo = __instance.currentGizmo;
        if (!currentGizmo)
        {
            Plugin.MyLogger.LogWarning("No current gizmo found during DragGizmo call.");
            return false;
        }

        var selection = Plugin.Instance.LevelEditorCentral?.selection;

        if (!selection)
        {
            Plugin.MyLogger.LogWarning("No selection found during DragGizmo call.");
            return false;
        }

        var selectionList = selection.list;

        if (selectionList.Count == 0)
        {
            Plugin.MyLogger.LogWarning("No objects selected for local translation.");
            return false;
        }

        if (currentGizmo.name.Contains("R")) return true;

        var gizmoName = currentGizmo.name.Replace("Gizmo", "").ToLower();

        if (!Plugin.Instance.MainCamera) return false;

        var mouseRay = Plugin.Instance.MainCamera.ScreenPointToRay(Input.mousePosition);

        var motherGizmo = Plugin.Instance.LevelEditorCentral?.gizmos?.motherGizmo;
        var rotationGizmo = Plugin.Instance.LevelEditorCentral?.gizmos?.translationGizmos.transform;

        if (!OriginPosition.HasValue)
            if (motherGizmo)
            {
                Plugin.MyLogger.LogInfo($"Origin position not set, using motherGizmo position: {motherGizmo.position}");

                OriginPosition = motherGizmo.position;
                if (OriginPosition != null)
                {
                    // Spawn the visual indicator
                    CreateOriginMarker(OriginPosition.Value);

                    _dragPlane = GetPlaneFromGizmo(motherGizmo, rotationGizmo, mouseRay, gizmoName);

                    _gotSnapped = false;
                }
            }

        // Size of the marker
        if (OriginPosition != null)
        {
            var dist = Vector3.Distance(Plugin.Instance.MainCamera.transform.position, OriginPosition.Value);
            var uniformScale = Mathf.Min(dist * SizeOnScreen, MaxReferenceSize);
            _originMarker.transform.localScale = Vector3.one * uniformScale;
        }


        if (_dragPlane == null) return false;

        if (!_dragPlane.Value.Plane.Raycast(mouseRay, out var enter)) return false;

        // Gizmo too far
        if (!(enter <= MaxDistance))
        {
            if (!isTooFar)
            {
                PlayerManager.Instance.messenger.Log("The gizmo is too far!", 2f);
                isTooFar = true;
            }
            return false;
        }

        isTooFar = false;

        var hitPoint = mouseRay.GetPoint(enter);

        // On initial click, store offset between pivot and where mouse hit the plane
        if (!InitialDragOffset.HasValue)
        {
            //Plugin.logger.LogInfo($"Initial drag offset not set, calculating from hit point: {hitPoint}");
            if (OriginPosition != null) InitialDragOffset = hitPoint - OriginPosition.Value;
            return false;
        }

        var localOffset = hitPoint - InitialDragOffset.Value;

        var snappedMove = Vector3.zero;

        _gridValues = [];

        // Axis gizmos (X, Y, Z)

        if (AxisNames.Any(gizmoName.Equals))
        {
            var axis = GetTranslationVector(rotationGizmo, gizmoName);

            if (axis == null)
            {
                Plugin.MyLogger.LogWarning($"Gizmo {gizmoName} does not have a valid translation vector.");
                return false;
            }

            var gridStep = gizmoName.Contains("y") ? __instance.gridY : __instance.gridXZ;

            var closestPoint = Vector3.Project(localOffset, axis.Value);

            if (OriginPosition != null)
            {
                var moveDir = closestPoint - OriginPosition.Value;

                var distance = Vector3.Dot(moveDir, axis.Value);
                var snappedDistance = gridStep > 0f ? Mathf.Round(distance / gridStep) * gridStep : distance;
                snappedMove = axis.Value * snappedDistance;
            }

            _gotSnapped = gridStep > 0f;

            _gridValues.Add(gridStep);
        }

        // Plane gizmos (XY, YZ, XZ)

        else if (PlaneNames.Any(gizmoName.Equals))
        {
            Vector3[] axes = [_dragPlane.Value.Axis1.normalized, _dragPlane.Value.Axis2.normalized];

            if (OriginPosition != null)
            {
                var rawMove = localOffset - OriginPosition.Value;

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
                    if (gridStep > 0f && !Mathf.Approximately(snapped, moveAmount))
                    {
                        _gotSnapped = true;
                        _gridValues.Add(gridStep);
                    }

                    snappedMove += axis * snapped;
                }
            }
        }

        // Apply movement
        foreach (var obj in selectionList.Where(_ => OriginPosition != null))
            if (OriginPosition != null)
                obj.transform.position = OriginPosition.Value + snappedMove +
                                         (obj.transform.position - __instance.motherGizmo.position);

        if (OriginPosition != null) __instance.motherGizmo.position = OriginPosition.Value + snappedMove;

        __instance.central.validation.BreakLock(false, null, "Gizmo11", false);

        // Play Sound
        var minNonZero = _gridValues.Where(v => v > 0).DefaultIfEmpty(float.MaxValue).Min();

        if (_gotSnapped && (!_lastSnappedPosition.HasValue ||
                            Vector3.Distance(__instance.motherGizmo.position, _lastSnappedPosition.Value) >=
                            minNonZero)) AudioEvents.MenuHover1.Play();
        _lastSnappedPosition = __instance.motherGizmo.position;

        return false;
    }

    private static void CreateOriginMarker(Vector3 position)
    {
        Plugin.MyLogger.LogInfo($"Creating origin marker at position: {position}");

        if (_originMarker) Object.Destroy(_originMarker);

        _originMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _originMarker.transform.position = position;
        _originMarker.transform.localScale = Vector3.one; // Small sphere

        var renderer = _originMarker.GetComponent<Renderer>();
        if (renderer)
        {
            renderer.material = new Material(Shader.Find("Standard"))
            {
                color = new Color(1f, 1f, 0f, 0.7f) // Yellow with alpha
            };
            renderer.material.SetFloat(Mode, 3); // Transparent mode
            renderer.material.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);
            renderer.material.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt(ZWrite, 0);
            renderer.material.DisableKeyword("_ALPHATEST_ON");
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            renderer.material.renderQueue = 3000;

            _originMarker.layer = 14; // Set to Ignore Raycast layer
        }

        Object.Destroy(_originMarker.GetComponent<Collider>()); // Remove the collider
    }

    internal static void ClearOriginMarker()
    {
        //Plugin.logger.LogInfo("Clearing origin marker...");

        if (_originMarker)
        {
            Object.Destroy(_originMarker);
            _originMarker = null;
        }

        OriginPosition = null;
        InitialDragOffset = null;
        isTooFar = false;
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

    private static DragPlane? GetPlaneFromGizmo(Transform motherGizmo, Transform rotationGizmo, Ray mouseRay,
        string name)
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
                Plugin.MyLogger.LogWarning($"Unknown gizmo name: {name}");
                return null;
        }

        // Use vector from camera to object as a stable basis
        if (!Plugin.Instance.MainCamera) return null;

        var toObject = (motherGizmo.position - Plugin.Instance.MainCamera.transform.position).normalized;

        // Make a plane perpendicular to the axis and facing the camera
        var planeNormal = Vector3.Cross(axis, toObject.normalized).normalized;
        if (planeNormal == Vector3.zero)
        {
            Plugin.MyLogger.LogWarning($"Plane normal is zero for gizmo {name}. Cannot create drag plane.");
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

// LEV_MotherGizmoFlipper_Update
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

// LEV_GizmoHandler_DisableGizmosOnDistance
[HarmonyPatch(typeof(LEV_GizmoHandler), "DisableGizmosOnDistance")]
public static class PatchDisableGizmosOnDistanceLocalTranslation
{
    private const float MaxDistance = 2500f;
    private static readonly Material GrayMaterial = new(Shader.Find("Standard"))
    {
        color = new Color(0.2f, 0.2f, 0.2f, 1f)
    };

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return true;

        if (Plugin.Instance.MainCamera)
        {
            var camTransform = Plugin.Instance.MainCamera.transform;
            var gizmoRoot = __instance.translationGizmos.transform;

            float cam_gizmo_dist = Vector3.Distance(camTransform.transform.position, gizmoRoot.position);

            if (cam_gizmo_dist > MaxDistance)
            {
                //// Gray out gizmos
                GrayOutGizmo(__instance.XZgizmo);
                GrayOutGizmo(__instance.YZgizmo);
                GrayOutGizmo(__instance.XYgizmo);
                GrayOutGizmo(__instance.Xgizmo);
                GrayOutGizmo(__instance.Ygizmo);
                GrayOutGizmo(__instance.Zgizmo);
            }

            else
            {
                RestoreGizmoMaterial(__instance.XZgizmo);
                RestoreGizmoMaterial(__instance.YZgizmo);
                RestoreGizmoMaterial(__instance.XYgizmo);
                RestoreGizmoMaterial(__instance.Xgizmo);
                RestoreGizmoMaterial(__instance.Ygizmo);
                RestoreGizmoMaterial(__instance.Zgizmo);
            }

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

    private static void GrayOutGizmo(LEV_SingleGizmo gizmo)
    {
        if (!gizmo) return;
        gizmo.renderdude.material = GrayMaterial;
    }

    private static void RestoreGizmoMaterial(LEV_SingleGizmo gizmo)
    {
        if (!gizmo) return;
        gizmo.renderdude.material = gizmo.original;
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

// LEV_CustomButton_SetToDefaultColor
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

// LEV_GizmoHandler_SetMotherPosition
[HarmonyPatch(typeof(LEV_GizmoHandler), "SetMotherPosition")]
public static class PatchSetMotherPositionLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled || __instance.isDragging) return;

        //Plugin.logger.LogInfo("SetMotherPosition called in LEV_GizmoHandler.");

        // Reset the last mouse position to the current mouse position
        PatchDragGizmoLocalTranslation.OriginPosition = null;
        PatchDragGizmoLocalTranslation.InitialDragOffset = null;

        // Set the translation gizmos to local mode
        Plugin.Instance.SetRotationToLocalMode();
    }
}

// LEV_GizmoHandler_SnapToGridXY
[HarmonyPatch(typeof(LEV_GizmoHandler), "SnapToGridXZ")]
public static class PatchSnapToGridXZLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled)
            return true;

        Plugin.MyLogger.LogInfo("SnapToGridXZ called in LEV_GizmoHandler.");

        return LocalGridSnapUtils.SnapToLocalGrid(__instance, true, false);
    }
}

// LEV_GizmoHandler_SnapToGridY
[HarmonyPatch(typeof(LEV_GizmoHandler), "SnapToGridY")]
public static class PatchSnapToGridYLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled)
            return true;

        Plugin.MyLogger.LogInfo("SnapToGridY called in LEV_GizmoHandler.");

        return LocalGridSnapUtils.SnapToLocalGrid(__instance, false, true);
    }
}

// LEV_GizmoHandler_ResetRotation
[HarmonyPatch(typeof(LEV_GizmoHandler), "ResetRotation")]
public static class PatchResetRotationLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled)
            return true;

        Plugin.MyLogger.LogInfo("ResetRotation called in LEV_GizmoHandler.");

        var selection = Plugin.Instance.LevelEditorCentral.selection;
        var selectedList = selection.list;


        if (selectedList.Count == 0 || !Plugin.Instance.ReferenceBlockObject || __instance.isGrabbing)
        {
            Plugin.MyLogger.LogWarning("No blocks selected or reference transform not set, skipping ResetRotation.");
            return true;
        }

        var referenceTransform = Plugin.Instance.ReferenceBlockObject?.transform;

        var before = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBlockListToJSONList(selectedList);

        // Match rotation to the selected block
        // Perform rotation
        var currentRotation = selectedList[^1].transform.rotation;
        if (referenceTransform != null)
        {
            var targetRotation = referenceTransform.rotation;
            var deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);

            // Convert quaternion delta to axis + angle
            deltaRotation.ToAngleAxis(out var angle, out var axis);

            __instance.central.rotflip.RotateBlocks(axis, angle, selectedList[^1].transform.position);
            __instance.central.gizmos.ResetRotationGizmoRotation();
        }


        var after = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBlockListToJSONList(selectedList);
        var selectionStr = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertSelectionToStringList(selectedList);

        Plugin.Instance.LevelEditorCentral.validation.BreakLock(
            Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBeforeAndAfterListToCollection(before, after,
                selectedList, selectionStr, selectionStr),
            "Gizmo_LocalSnap"
        );


        return false; // Skip the original method
    }
}

// LocalGridSnap Helper Class
public static class LocalGridSnapUtils
{
    // ReSharper disable once InconsistentNaming
    public static bool SnapToLocalGrid(LEV_GizmoHandler __instance, bool snapXZ, bool snapY)
    {
        var selection = Plugin.Instance.LevelEditorCentral.selection;
        var selectedList = selection.list;

        if (selectedList.Count == 0 || !Plugin.Instance.ReferenceBlockObject || __instance.isGrabbing)
        {
            Plugin.MyLogger.LogWarning("No blocks selected or reference transform not set, skipping SnapToLocalGrid.");
            return true;
        }

        Plugin.MyLogger.LogInfo("SnapToLocalGrid called in LEV_GizmoHandler.");

        var refTransform = Plugin.Instance.ReferenceBlockObject.transform;

        var gridXZ = __instance.gridXZ != 0f ? __instance.gridXZ : __instance.list_gridXZ[^1];
        var gridY = __instance.gridY != 0f ? __instance.gridY : __instance.list_gridY[^1];

        var referencePosition = selectedList[^1].transform.position;
        var before = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBlockListToJSONList(selectedList);

        var rotation = refTransform.rotation;
        var position = refTransform.position;
        var worldToLocal = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        var localToWorld = Matrix4x4.TRS(position, rotation, Vector3.one);

        var localPos = worldToLocal.MultiplyPoint(referencePosition);

        var snappedX = snapXZ ? Mathf.Round(localPos.x / gridXZ) * gridXZ : localPos.x;
        var snappedY = snapY ? Mathf.Round(localPos.y / gridY) * gridY : localPos.y;
        var snappedZ = snapXZ ? Mathf.Round(localPos.z / gridXZ) * gridXZ : localPos.z;

        Vector3 snappedLocalPos = new(snappedX, snappedY, snappedZ);
        var snappedWorldPos = localToWorld.MultiplyPoint(snappedLocalPos);

        var delta = snappedWorldPos - referencePosition;

        Plugin.Instance.LevelEditorCentral.selection.TranslatePositions(delta);
        __instance.SetMotherPosition(__instance.motherGizmo.position + delta);

        var after = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBlockListToJSONList(selectedList);
        var selectionStr = Plugin.Instance.LevelEditorCentral.undoRedo.ConvertSelectionToStringList(selectedList);

        Plugin.Instance.LevelEditorCentral.validation.BreakLock(
            Plugin.Instance.LevelEditorCentral.undoRedo.ConvertBeforeAndAfterListToCollection(before, after,
                selectedList, selectionStr, selectionStr),
            "Gizmo_LocalSnap"
        );


        return false;
    }
}

// LEV_GizmoHandler_GrabGizmo
[HarmonyPatch(typeof(LEV_GizmoHandler), "GrabGizmo")]
public static class PatchGrabGizmoLocalTranslation
{
    private const float MaxDistance = 1500;
    private static Vector3? _lastAxisPoint;
    private static Vector3 _totalScrollOffset = Vector3.zero;

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(LEV_GizmoHandler __instance)
    {
        if (!Plugin.Instance.UseLocalGridMode || !Plugin.Instance.IsModEnabled)
            return true;

        // Safety check
        if (!Plugin.Instance.ReferenceBlockObject)
        {
            Plugin.MyLogger.LogInfo("Reference block object not set, skipping to original method");
            return true;
        }
            

        if (__instance.central.selection.list.Count == 0)
        {
            Plugin.MyLogger.LogWarning("No blocks selected for local translation, skipping to original method, blame the game code in case of error :)");
            return true;
        }
        var lastSelectionPosition = __instance.central.selection.list[^1].transform.position;

        // Get the reference transform
        var referenceTransform = Plugin.Instance.ReferenceBlockObject.transform;

        var planeNormal = referenceTransform.up;
        var planeOrigin = referenceTransform.position + _totalScrollOffset;
        Vector3[] planeAxes = [referenceTransform.right, referenceTransform.forward];


        // Create the custom drag plane
        var dragPlane = new Plane(planeNormal, planeOrigin);

        // Cast a ray from the mouse to the plane
        if (!Plugin.Instance.MainCamera) return false;
        var mouseRay = Plugin.Instance.MainCamera.ScreenPointToRay(Input.mousePosition);
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

            _lastAxisPoint = referenceTransform.position;

            // Match rotation to the selected block
            // Perform rotation
            var currentRotation = __instance.central.selection.list[^1].transform.rotation;
            var targetRotation = referenceTransform.rotation;
            var deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);

            // Convert quaternion delta to axis + angle
            deltaRotation.ToAngleAxis(out var angle, out var axis);

            __instance.central.rotflip.RotateBlocks(axis, angle,
                __instance.central.selection.list[^1].transform.position);
            __instance.central.gizmos.ResetRotationGizmoRotation();

            _totalScrollOffset = Vector3.zero;

            __instance.newGizmo = false;

            Plugin.MyLogger.LogInfo(
                $"Local translation initialized with reference block: {referenceTransform.name} " +
                $"at position {referenceTransform.position} with rotation {referenceTransform.rotation}");
        }

        var localOffset = hitPoint;

        if (_lastAxisPoint == null) return false;

        var rawMove = localOffset - _lastAxisPoint.Value;

        var snappedMove = Vector3.zero;

        var yScroll = SetYGridStep(__instance);
        if (Mathf.Abs(yScroll) > 0f)
        {
            var scrollOffset = planeNormal * yScroll;
            snappedMove += scrollOffset;

            _totalScrollOffset += scrollOffset;

            Plugin.Instance.MainCamera.transform.position += scrollOffset;
        }

        var xyGridStep = __instance.gridXZ;


        foreach (var axis in planeAxes)
        {
            // Determine a grid step based on the axis and gizmo name
            var moveAmount = Vector3.Dot(rawMove, axis);
            var snapped = xyGridStep > 0f ? Mathf.Round(moveAmount / xyGridStep) * xyGridStep : moveAmount;
            snappedMove += axis * snapped;
        }

        // Apply movement to the selected block

        __instance.motherGizmo.transform.position += snappedMove;
        __instance.dragGizmoOrigin = __instance.motherGizmo.position;
        __instance.central.selection.TranslatePositions(snappedMove);

        if (__instance.motherGizmo.transform.position != __instance.rememberTranslation && xyGridStep > 0f)
        {
            AudioEvents.MenuHover1.Play();
            __instance.rememberTranslation = __instance.motherGizmo.transform.position;
        }

        _lastAxisPoint = _lastAxisPoint.Value + snappedMove;

        __instance.newBlockHeight = __instance.central.selection.list[^1].transform.position.y;

        return false;
    }

    // ReSharper disable once InconsistentNaming
    private static float SetYGridStep(LEV_GizmoHandler __instance)
    {
        var yGridStep = 0.1f;
        if (__instance.gridY == 0f)
        {
            if(__instance.list_gridY.Count != 0)
            {
                yGridStep = Mathf.Max(__instance.list_gridY[^1],0.1f);
            }   
        }
        else
        {
            yGridStep = __instance.gridY;
        }


        if (__instance.central.input.GizmoGridVertical.positiveButtonDown && !__instance.central.input.inputLocked)
            return yGridStep;

        if (__instance.central.input.GizmoGridVertical.negativeButtonDown && !__instance.central.input.inputLocked)
            return -yGridStep;

        return 0f;
    }
}

// LEV_GizmoHandler_Update
[HarmonyPatch(typeof(LEV_GizmoHandler), "Update")]
public static class PatchGizmoHandlerUpdateLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (__instance.central.selection.list.Count == 0) return;

        if (Plugin.Instance.UseLocalGridMode && Plugin.Instance.IsModEnabled && __instance.isGrabbing &&
            !__instance.central.selection.list[^1].placeDynamic)
        {
            // If the gridHeightHelper is not active, activate it
            __instance.gridHeightHelper.gameObject.SetActive(true);

            __instance.gridHeightHelper.position = __instance.central.selection.list[^1].transform.position;
            __instance.gridHeightHelper.rotation = __instance.central.selection.list[^1].transform.rotation;
        }
        else if (__instance.central.selection.list.Count == 1 && !__instance.central.selection.list[0].placeDynamic)
        {
            __instance.gridHeightHelper.position = new Vector3(__instance.motherGizmo.position.x,
                __instance.newBlockHeight - 4f, __instance.motherGizmo.position.z);
            __instance.gridHeightHelper.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}

// LEV_RotateFlip_RotateBlocks 2Params (Vector3 upVector, float angle)
[HarmonyPatch(typeof(LEV_RotateFlip), nameof(LEV_RotateFlip.RotateBlocks))]
[HarmonyPatch([typeof(Vector3), typeof(float)])]
internal class PatchRotateBlocks2ParamLocalTranslation
{
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once UnusedParameter.Local
    private static void Prefix(ref Vector3 upVector, float angle)
    {
        if (!Plugin.Instance.LevelEditorCentral)
        {
            Plugin.MyLogger.LogWarning("LevelEditorCentral.Instance is null in RotateBlocks prefix.");
            return;
        }

        if ((!Plugin.Instance.UseLocalTranslationMode && !Plugin.Instance.UseLocalGridMode) ||
            !Plugin.Instance.IsModEnabled) return;

        if (Plugin.Instance.LevelEditorCentral.selection.list.Count <= 0) return;
        // Modify the upVector
        upVector = Plugin.Instance.LevelEditorCentral.selection.list[^1].transform.up;
        Plugin.MyLogger.LogInfo(
            $"RotateBlocks called in local translation mode.");
    }

    // ReSharper disable once InconsistentNaming
    // ReSharper disable twice UnusedParameter.Local
    // ReSharper disable once UnusedMember.Local
    private static void Postfix(Vector3 upVector, float angle)
    {
        if ((Plugin.Instance.UseLocalTranslationMode || Plugin.Instance.UseLocalGridMode) &&
            Plugin.Instance.IsModEnabled) Plugin.Instance.SetRotationToLocalMode();
    }
}

// LEV_InspectorGUICreator_UpdateTransform
[HarmonyPatch(typeof(LEV_InspectorGUICreator), "UpdateTransform")]
public static class PatchUpdateTransformLocalTranslation
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(LEV_InspectorGUICreator __instance, bool resetOnly, bool isRealTime)
    {
        if (__instance is null) throw new ArgumentNullException(nameof(__instance));

        if (Plugin.Instance.UseLocalTranslationMode && Plugin.Instance.IsModEnabled && !isRealTime && !resetOnly)
            Plugin.Instance.SetRotationToLocalMode();
    }
}