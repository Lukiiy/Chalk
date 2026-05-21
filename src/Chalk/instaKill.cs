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

        LevelBoundsTracker tracker = __instance.AsEntity.LevelBoundsTracker;

        bool inWater = (tracker.AuthoritativeBoundsState.HasState(BoundsState.InMainOutOfBoundsHazard) && (MainOutOfBoundsHazard.Type == OutOfBoundsHazard.Water || MainOutOfBoundsHazard.Type == OutOfBoundsHazard.Fog)) || (tracker.AuthoritativeBoundsState.HasState(BoundsState.InSecondaryOutOfBoundsHazard) && tracker.CurrentSecondaryHazardLocalOnly != null && tracker.CurrentSecondaryHazardLocalOnly.Type == OutOfBoundsHazard.Water);
        if (!inWater) return;

        var playerInfo = __instance.Networkowner?.PlayerInfo;
        if (playerInfo == null) return;

        VfxManager.ServerPlayPooledVfxForAllClients(VfxType.MineExplosion, playerInfo.transform.position, Quaternion.identity);
        playerInfo.AsGolfer.ServerEliminate(EliminationReason.OutOfBounds);
    }
}