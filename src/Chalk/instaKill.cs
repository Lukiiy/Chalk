using HarmonyLib;
using Mirror;
using UnityEngine;

namespace Chalk;

[HarmonyPatch]
public class InstaKill
{
    [HarmonyPatch(typeof(GolfBall), "ServerReturnToBounds")]
    [HarmonyPostfix]
    private static void BallReturnToBounds(GolfBall __instance)
    {
        if (!Chalk.instaKills.Value || __instance == null || !__instance.isServer || !NetworkServer.active) return;

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