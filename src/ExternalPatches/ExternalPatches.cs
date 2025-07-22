using UnityEngine;

namespace LocalTranslation.ExternalPatches;

public static class PatchConvertLocalToWorldVectorsLocalTranslation
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public static bool Prefix(ref Vector3[] __result)
    {
        if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return true;

        // Return world-aligned axes regardless of transform orientation
        __result =
        [
            Vector3.right,
            Vector3.up,
            Vector3.forward
        ];
        return false; // Skip original
    }
}