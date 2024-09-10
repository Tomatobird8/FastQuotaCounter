using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;

namespace FastQuotaCounter.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class QuotaTextPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("rackUpNewQuotaText", MethodType.Enumerator)]
        static IEnumerable<CodeInstruction> rackUpNewQuotaTextMoveNext(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo quotaTextAmountFieldInfo = null;

            foreach (CodeInstruction code in instructions)
            {
                if ((code.operand as FieldInfo)?.Name?.Contains("<quotaTextAmount>") == true
                    && (code.operand as FieldInfo)?.ReflectedType?.Name.Contains("<rackUpNewQuotaText>") == true)
                {
                    quotaTextAmountFieldInfo = code.operand as FieldInfo;
                    break;
                }
            }

            if (quotaTextAmountFieldInfo == null)
            {
                return instructions;
            }

            IEnumerable<CodeInstruction> newInstructions = instructions;
            try
            {
                newInstructions = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldc_R4, 250f),
                        new CodeMatch(OpCodes.Mul))
                    .ThrowIfNotMatch("250f quota inrement amount not found")
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TimeOfDay), "get_Instance")))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TimeOfDay), "profitQuota")))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Conv_R4))
                    .InstructionEnumeration();
            }
            catch (Exception ex)
            {
                FastQuotaCounter.Logger.LogError($"Error while patching rackUpNewQuotaText. Exception: {ex}");
            }

            return newInstructions;
        }
    }
}
