using System.Reflection;
using HarmonyLib;

[HarmonyPatch]
internal static class ItemSpawner3ItemsThing
{
    static MethodInfo TargetMethod() => AccessTools.FirstMethod(typeof(ItemSpawner), m => m.Name.Contains("TryGiveItemToPlayer"));

    static void Postfix(ItemSpawner __instance, PlayerInventory playerInventory, bool __result)
    {
        if (!__result) return;

        for (int i = 0; i < 2; i++)
        {
            if (!playerInventory.HasSpaceForItem(out _)) break;

            var type = __instance.settings.GetRandomItemFor(playerInventory.PlayerInfo);

            if (GameManager.AllItems.TryGetItemData(type, out var data)) playerInventory.ServerTryAddItem(type, data.MaxUses);
        }
    }
}