using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BeatSaberMarkupLanguage.Components;
using HarmonyLib;
using TMPro;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    // TMP_Text_CreateMaterialInstance removed as of the 1.45.1 port. It copied
    // Material.enabledKeywords onto the result because "the version of TextMesh Pro that
    // Beat Saber uses doesn't support it (yet)" (see git history). As of 1.45.1, Unity's own
    // bundled TextMeshPro (now built into the engine rather than a separate package) already
    // copies keywords itself -- decompiled TMP_Text.CreateMaterialInstance:
    // `new Material(source) { shaderKeywords = source.shaderKeywords }`. Re-applying keywords
    // afterward through the newer enabledKeywords API on top of an already-correct material
    // is redundant at best, and is the likely cause of a garbled/smudged-texture rendering bug
    // seen on SongCore's "X songs loaded" overlay (and other CurvedTextMeshPro-based text)
    // after porting to 1.45.1.

    [HarmonyPatch(typeof(TMP_Text), "CalculatePreferredValues")]
    internal static class TMP_Text_CalculatePreferredValues
    {
        private static readonly MethodInfo TargetMethod = AccessTools.Method(typeof(char), nameof(char.IsWhiteSpace), new[] { typeof(char) });
        private static readonly MethodInfo OverrideMethod = AccessTools.Method(typeof(TMP_Text_CalculatePreferredValues), nameof(IsWhiteSpace));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            foreach (CodeInstruction codeInstruction in codeInstructions)
            {
                if (codeInstruction.operand is MethodInfo methodInfo && methodInfo == TargetMethod)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, OverrideMethod);
                }
                else
                {
                    yield return codeInstruction;
                }
            }
        }

        private static bool IsWhiteSpace(char c, TMP_Text instance) => instance is not WhitespaceIncludingCurvedTextMeshPro && char.IsWhiteSpace(c);
    }
}
