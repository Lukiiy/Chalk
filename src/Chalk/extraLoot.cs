using HarmonyLib;
using UnityEngine;

namespace Chalk;

[HarmonyPatch]
public class ExtraLoot
{
    public static bool toggled = true;

    [HarmonyPatch(typeof(CourseManager), "InformPlayerKnockedOutInternal")]
    [HarmonyPostfix]
    private static void knockedOutByPlayer(PlayerMovement knockedOutPlayer, PlayerInfo responsiblePlayer, KnockoutType knockoutType, ref bool knockoutCounted)
    {
        if (!toggled || !knockoutCounted || responsiblePlayer == null || knockedOutPlayer == null) return;

        ItemType[] possible = [ItemType.Coffee, ItemType.SpringBoots, ItemType.GolfCart, ItemType.Airhorn];
        ItemType randomItem = possible[UnityEngine.Random.Range(0, possible.Length)];

        if (GameManager.AllItems.TryGetItemData(randomItem, out var itemData))
        {
            if (responsiblePlayer.Inventory.HasSpaceForItem(out _))
            {
                responsiblePlayer.Inventory.ServerTryAddItem(randomItem, itemData.MaxUses);
                responsiblePlayer.RpcPopUp(PlayerTextPopupType.Comeback, 0);
            }
        }
    }
}