using UnityEngine;

namespace LocalTranslation.ExternalPatches
{
    public static class PatchConvertLocalToWorldVectorsLocalTranslation
    {
        public static bool Prefix(Transform local, ref Vector3[] __result)
        {

            if (!Plugin.Instance.UseLocalTranslationMode || !Plugin.Instance.IsModEnabled) return true;

            // Return world-aligned axes regardless of transform orientation
            __result = new Vector3[]
            {
            Vector3.right,
            Vector3.up,
            Vector3.forward
            };
            return false; // Skip original
        }
    }

}
