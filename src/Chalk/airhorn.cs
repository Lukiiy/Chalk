using HarmonyLib;
using UnityEngine;
using Mirror;

namespace Chalk;

[HarmonyPatch]
public static class Airhorn
{
    public static bool toggle = true;
    private const float rangeMult = 1.2f;
    private const float upwardLaunch = 25f;

    [HarmonyPatch(typeof(PlayerInventory), "UserCode_CmdPlayAirhornVfxForAllClients__NetworkConnectionToClient")]
    [HarmonyPostfix]
    private static void Used(PlayerInventory __instance)
    {
        if (!toggle || __instance == null || !NetworkServer.active) return;

        var owner = __instance.PlayerInfo;
        if (owner == null) return;

        Vector3 origin = owner.transform.position;
        float range = GameManager.ItemSettings.AirhornRange * rangeMult;
        float rangeSqr = range * range;

        foreach (var ball in UnityEngine.Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None))
        {
            if (ball == null || !ball.isServer || (ball.transform.position - origin).sqrMagnitude > rangeSqr) continue;

            ball.AsEntity.Rigidbody.AddForce(Vector3.up * upwardLaunch, ForceMode.VelocityChange);
            VfxManager.ServerPlayPooledVfxForAllClients(VfxType.AirhornPlayerTriggered, ball.transform.position, Quaternion.identity);
        }
    }
}