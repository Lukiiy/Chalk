using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Chalk;

[HarmonyPatch(typeof(MatchSetupRules), nameof(MatchSetupRules.Initialize))]
internal static class IngameConfig
{
    private static readonly List<string> boolOptions = ["On", "Off"];
    private static readonly ConfigEntry<bool>[] Toggleables = [Chalk.windBursts, Chalk.minedLootboxes, Chalk.holeMinefield, Chalk.ballSwap, Chalk.instaKills, Chalk.extraLoots, Chalk.airHornExtra, Chalk.mineChain, Chalk.itemBoxTriple];

    [HarmonyPostfix]
    private static void Inject(MatchSetupRules __instance)
    {
        if (__instance?.homingShots == null) return;

        DropdownOption template = __instance.homingShots;
        Transform parent = template.transform.parent;
        UiTooltip tooltip = __instance.tooltip;

        int insertIndex = parent.childCount;

        foreach (var setting in Toggleables)
        {
            string objName = $"Chalk_{setting.Definition.Key}";
            if (parent.Find(objName) != null) continue;

            DropdownOption clone = UnityEngine.Object.Instantiate(template, parent);

            clone.name = objName;
            clone.transform.SetSiblingIndex(insertIndex++);

            foreach (var localizComp in clone.GetComponentsInChildren<LocalizeStringEvent>(true)) localizComp.enabled = false; // disable localization components

            SetLabel(clone, $"Chalk: {setting.Definition.Key}");
            clone.SetOptions(boolOptions);
            clone.Initialize(() => setting.Value = clone.value == 0, setting.Value ? 0 : 1);
            tooltip.RegisterTooltip(clone.GetComponent<RectTransform>(), setting.Description.Description);
        }
    }

    private static void SetLabel(DropdownOption option, string text)
    {
        foreach (TMP_Text meshText in option.GetComponentsInChildren<TMP_Text>(true))
        {
            if (meshText.GetComponentInParent<TMP_Dropdown>() != null) continue;

            meshText.text = text;
            meshText.ForceMeshUpdate();

            break;
        }
    }
}