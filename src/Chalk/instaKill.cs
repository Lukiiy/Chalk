using HarmonyLib;
using UnityEngine;

namespace Chalk;

[HarmonyPatch]
public class InstaKill
{
    public static bool toggled = true;

    [HarmonyPatch(typeof(GolfBall), "ServerReturnToBounds")]
    [HarmonyPostfix]
    private static void BallReturnToBounds(GolfBall __instance)
    {
        if (!toggled || __instance == null || !__instance.isServer) return;

        var playerInfo = __instance.Networkowner?.PlayerInfo;
        if (playerInfo == null) return;

        Eliminate(playerInfo);
    }

    public static void Eliminate(PlayerInfo playerInfo)
    {
        VfxManager.ServerPlayPooledVfxForAllClients(VfxType.MineExplosion, playerInfo.transform.position, Quaternion.identity);
        playerInfo.AsGolfer.ServerEliminate(EliminationReason.OutOfBounds);
    }
}