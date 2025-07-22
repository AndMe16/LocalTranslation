using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LocalTranslation.ExternalPatches;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LocalTranslation;

// will be replaced by assemblyName if desired
[BepInPlugin("andme123.localtranslation", "LocalTranslation", MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.metalted.zeepkist.blueprintsX", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    // Only patch the LevelEditor2 scene
    private const string TargetSceneName = "LevelEditor2";
    private const float MaxReferenceSize = 4f; // maximum size of the reference block in world units
    private const float SizeOnScreen = 0.15f; // world‐units per unit of distance 
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
    private Transform _referenceBlock;
    private LEV_RotateFlip _rotateFlip;
    private Image _toggleLocalTranslationImage;

    internal LEV_CustomButton CustomButton;

    internal bool IsModEnabled;
    internal LEV_LevelEditorCentral LevelEditorCentral;

    internal Camera MainCamera;

    internal GameObject ReferenceBlockObject;
    internal GameObject ToggleLabel;
    internal GameObject ToggleLocalTranslationButton;
    internal bool UseLocalGridMode;
    internal bool UseLocalTranslationMode;

    public static Plugin Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        logger = Logger;

        _harmony = new Harmony("andme123.localtranslation");

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            if (type.Namespace == "LocalTranslation.InternalPatches") // or use an attribute tag
                _harmony.PatchAll(type);


        // Conditionally patch BPX
        TryPatchBpx();

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


        if (!Input.GetKeyDown(ModConfig.setReference.Value) || LevelEditorCentral.input.inputLocked ||
            LevelEditorCentral.gizmos.isGrabbing) return;
        if (LevelEditorCentral.selection.list.Count == 0)
        {
            if (ReferenceBlockObject)
            {
                PlayerManager.Instance.messenger.Log("[LocTrans] Reference Block removed", 5);
                Destroy(ReferenceBlockObject);
                _referenceBlock = null;
                UseLocalGridMode = false;

                logger.LogInfo("Reference Block removed, local grid mode deactivated.");
            }
            else
            {
                PlayerManager.Instance.messenger.LogCustomColor("[LocTrans] No blocks selected to set as reference",
                    5, Color.black, new Color(1f, 0.98f, 0.29f, 0.9f));
            }

            return;
        }

        if (_referenceBlock)
            if (LevelEditorCentral.selection.list[^1].transform == _referenceBlock)
            {
                PlayerManager.Instance.messenger.Log("[LocTrans] Reference Block removed", 5);
                Destroy(ReferenceBlockObject);
                _referenceBlock = null;
                UseLocalGridMode = false;
                logger.LogInfo("Reference Block removed, local grid mode deactivated.");
                return;
            }

        if (!UseLocalTranslationMode)
        {
            UseLocalTranslationMode = true;
            SetRotationToLocalMode();
        }

        UseLocalGridMode = true;

        _referenceBlock = LevelEditorCentral.selection.list[^1].transform;

        if (!ReferenceBlockObject)
            CreateReferenceBlockObject(_referenceBlock);

        ReferenceBlockObject.SetActive(true);

        PlayerManager.Instance.messenger.Log("[LocTrans] Reference Block set, local translation mode activated", 5);
        logger.LogInfo(
            "Reference Block set, local translation mode activated");
    }

    private void LateUpdate()
    {
        if (ReferenceBlockObject && _referenceBlock)
        {
            // match position and rotation
            ReferenceBlockObject.transform.position = _referenceBlock.position;
            ReferenceBlockObject.transform.rotation = _referenceBlock.rotation;

            // scale so that screen‐space size stays roughly constant
            if (!MainCamera) return;
            var dist = Vector3.Distance(MainCamera.transform.position, ReferenceBlockObject.transform.position);
            var uniformScale = Mathf.Min(dist * SizeOnScreen, MaxReferenceSize);
            ReferenceBlockObject.transform.localScale = Vector3.one * uniformScale;
        }
        else if (!_referenceBlock && ReferenceBlockObject)
        {
            logger.LogInfo("Reference block is null, destroying ReferenceBlockObject.");
            Destroy(ReferenceBlockObject?.gameObject);
            ReferenceBlockObject = null;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    private void CreateReferenceBlockObject(Transform source)
    {
        logger.LogInfo("Creating Reference Block Object...");

        if (!ReferenceBlockObject) ReferenceBlockObject = CreateReferenceGizmo();

        ReferenceBlockObject.transform.position = source.position;
        ReferenceBlockObject.transform.rotation = source.rotation;
        ReferenceBlockObject.SetActive(true);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private static GameObject CreateReferenceGizmo()
    {
        var gizmoRoot = new GameObject("ReferenceGizmo");


        // Create one disc for each plane
        CreateDisc("Disc_X", Vector3.right, Color.red, 1f); // YZ plane
        CreateDisc("Disc_Y", Vector3.up, Color.green, 1.01f); // XZ plane
        CreateDisc("Disc_Z", Vector3.forward, Color.blue, 1.02f); // XY plane

        gizmoRoot.layer = 14;

        // Also, apply the layer to all children (Unity doesn't do this recursively)
        foreach (var child in gizmoRoot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 14;

        gizmoRoot.SetActive(false);
        return gizmoRoot;

        void CreateDisc(string discName, Vector3 normal, Color color, float scaler)
        {
            // Outline
            var outline = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outline.name = discName + "_Outline";
            outline.transform.SetParent(gizmoRoot.transform, false);
            outline.transform.localScale = new Vector3(1.1f * scaler, 0.03f, 1.1f * scaler); // 1 unit + tiny bit
            outline.transform.localPosition = Vector3.zero;
            outline.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            Destroy(outline.GetComponent<Collider>());

            var oMat = outline.GetComponent<Renderer>().material;
            oMat.shader = Shader.Find("Unlit/Color");
            oMat.color = Color.black;

            // Inner
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = discName;
            disc.transform.SetParent(gizmoRoot.transform, false);
            disc.transform.localScale = new Vector3(1f * scaler, 0.05f, 1f * scaler);
            disc.transform.localPosition = normal * 0.01f;
            disc.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            Destroy(disc.GetComponent<Collider>());

            var dMat = disc.GetComponent<Renderer>().material;
            dMat.shader = Shader.Find("Unlit/Color");
            dMat.color = color;
        }
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
            MainCamera = Camera.main;
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
            MainCamera = null;

            // Logger.LogInfo("Not in the Level Editor scene — mod inactive.");
        }
    }

    private void CreateToggleLocalModeButton()
    {
        logger.LogInfo("Creating Toggle Local Translation button...");

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

        logger.LogInfo("Toggle Local Translation button created successfully.");
    }

    internal void SetRotationToLocalMode()
    {
        if (!LevelEditorCentral)
        {
            Logger.LogError("LEV_LevelEditorCentral is not initialized.");
            return;
        }

        // Get the translation gizmos from the LevelEditorCentral
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

                Logger.LogInfo("TranslationGizmos set to local mode based on selected object.");
            }
            else
            {
                // Set gizmos to world mode
                translationGizmos.transform.localRotation = Quaternion.Euler(0, 0, 0); // Reset rotation to world space

                _toggleLocalTranslationImage.sprite = _sprites["Pivot_Average"];

                Logger.LogInfo("TranslationGizmos set to world mode.");
            }
        }
        else
        {
            Logger.LogWarning("TranslationGizmos not found");
        }
    }

    private void TryPatchBpx()
    {
        var bpxType = AccessTools.TypeByName("BPX.BPXUtils");
        if (bpxType == null) return;
        var method = AccessTools.Method(bpxType, "ConvertLocalToWorldVectors");
        var prefix = new HarmonyMethod(typeof(PatchConvertLocalToWorldVectorsLocalTranslation).GetMethod("Prefix"));
        _harmony.Patch(method, prefix);
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