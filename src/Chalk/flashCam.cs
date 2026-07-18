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

        Transform origin = __instance.PlayerInfo.transform;

        float range = GameManager.ItemSettings.FlashCameraMaxRange;
        const float viewAngle = 35f;

        foreach (var mine in UnityEngine.Object.FindObjectsByType<Landmine>(FindObjectsSortMode.None))
        {
            if (mine == null || !mine.isServer) continue;

            Vector3 toMine = mine.transform.position - origin.position;
            float dist = toMine.magnitude;

            if (dist > range || dist < .01f || Vector3.Angle(origin.forward, toMine) > viewAngle) continue;

            ExtraMines.ExplodeMethod.Invoke(mine, null);
        }
    }
}
