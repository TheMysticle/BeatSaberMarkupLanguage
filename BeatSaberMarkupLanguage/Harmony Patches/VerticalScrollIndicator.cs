using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HMUI;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    /// <summary>
    /// This patch reduces the minimum handle size of <see cref="VerticalScrollIndicator.RefreshHandle"/>.
    /// </summary>
    // As of 1.45.1, RefreshHandle's position-calculation formula was rewritten by the game
    // itself (decompiled: `_handle.anchoredPosition = new Vector2(0f, (0f - _progress) * (num
    // - num2) - _padding)`), which looks like a complete, correct fix for the same handle-
    // position bug this patch used to work around -- the exact IL sequence this patch's
    // second transformation matched (`(1f - _normalizedPageHeight) * num`) no longer exists
    // anywhere in the method. Without a ThrowIfInvalid() check, the failed match silently
    // proceeded to RemoveInstructions on an invalid position, throwing
    // InvalidOperationException at Harmony patch-apply time (confirmed via an actual
    // in-headset-equivalent launch: BSML logged "Failed to patch ... RefreshHandle" and
    // skipped the rest of its own Harmony patches for that class, though the game kept
    // running otherwise). Dropped the now-invalid second transformation; kept the first
    // (still valid IL), which only shrinks the minimum handle size and doesn't touch
    // position calculation at all.
    [HarmonyPatch(typeof(VerticalScrollIndicator), nameof(VerticalScrollIndicator.RefreshHandle))]
    internal class VerticalScrollIndicator_RefreshHandle
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)

                // make the minimum size delta of the scroll indicator handle 2 instead of 10
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 10f))
                .ThrowIfInvalid("Ldc_R4 10f not found")
                .SetOperandAndAdvance(2f)
                .InstructionEnumeration();
        }
    }
}
