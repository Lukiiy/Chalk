using HarmonyLib;
using Mirror;
using UnityEngine;

namespace Chalk;

[HarmonyPatch]
public static class FlashCam
{
    [HarmonyPatch(typeof(PlayerInventory), "UserCode_CmdInformUsedFlashCamera__Hittable[]__Single[]__UInt64__NetworkConnectionToClient")]
    [HarmonyPostfix]
    private static void Flash(PlayerInventory __instance, NetworkConnectionToClient sender)
    {
        if (!NetworkServer.active || __instance.PlayerInfo == null) return;

        float rangeSq = Mathf.Pow(GameManager.ItemSettings.FlashCameraMaxRange, 2) / 1.5f;

        foreach (Landmine mine in UnityEngine.Object.FindObjectsByType<Landmine>(FindObjectsSortMode.None))
        {
            if (mine == null || !mine.isServer || (mine.transform.position - __instance.PlayerInfo.transform.position).sqrMagnitude > rangeSq) continue;

            ExtraMines.ExplodeMethod.Invoke(mine, null);
        }
    }
}
