using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

/// <summary>
/// Contains patches allowing mods to customize card descriptions globally.
/// These are intended for mods that add new keyword-like effects that don't necessarily work as actual keywords.
/// <para/>
/// Note that just declaring the patches requires Publicize, which doesn't make sense for most mods to require.
/// Without it, Harmony cannot differentiate between the overloads of <c>GetDescriptionForPile</c>.
/// </summary>
[HarmonyPatch]
public static class DescriptionOverridePatches
{
    public delegate void CustomizeDescriptionHandler(CardModel card, Creature? target, ref string description);
    
    /// <summary>
    /// Allows customizing a card's description before it is processed by the game.
    /// </summary>
    public static event CustomizeDescriptionHandler? CustomizeDescription;

    /// <summary>
    /// Allow customizing a card's description after it has been processed by the game.
    /// </summary>
    public static event CustomizeDescriptionHandler? CustomizeDescriptionPost;
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile),
        typeof(PileType), typeof(CardModel.DescriptionPreviewType), typeof(Creature))]
    static List<CodeInstruction> TranspileGetDescriptionForPile(IEnumerable<CodeInstruction> instructionsIn)
    {
        return new InstructionPatcher(instructionsIn)
            .Match(new InstructionMatcher()
                .ldloc_0()
                .callvirt(typeof(LocString), nameof(LocString.GetFormattedText))
                .opcode(OpCodes.Stind_Ref)
            )
            .Step(-2)
            .Insert([
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadArgument(3),
            ])
            // The replace seems necessary, and no, I'm not sure why.
            .Replace(CodeInstruction.Call(typeof(DescriptionOverridePatches), nameof(InvokeCustomize)));
    }

    internal static string InvokeCustomize(LocString locString, CardModel card, Creature? target)
    {
        var s = locString.GetFormattedText();
        CustomizeDescription?.Invoke(card, target, ref s);
        return s;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile),
        typeof(PileType), typeof(CardModel.DescriptionPreviewType), typeof(Creature))]
    internal static void InvokeCustomizePost(CardModel __instance, Creature? target, ref string __result)
    {
        CustomizeDescriptionPost?.Invoke(__instance, target, ref __result);
    }
}