using HarmonyLib;
using UnityEngine;
using Mirror;

namespace Chalk;

[HarmonyPatch]
public static class Airhorn
{
    private const float rangeMult = 1.2f;
    private const float upwardLaunch = 25f;

    [HarmonyPatch(typeof(PlayerInventory), "UserCode_CmdPlayAirhornVfxForAllClients__NetworkConnectionToClient")]
    [HarmonyPostfix]
    private static void Used(PlayerInventory __instance)
    {
        if (!Chalk.airHornExtra.Value || __instance == null || !NetworkServer.active) return;

        PlayerInfo owner = __instance.PlayerInfo;
        if (owner == null) return;

        Vector3 origin = owner.transform.position;
        float rangeSqr = (float) Math.Pow(GameManager.ItemSettings.AirhornRange * rangeMult, 2);

        foreach (GolfBall ball in UnityEngine.Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None))
        {
            if (ball == null || !ball.isServer || (ball.transform.position - origin).sqrMagnitude > rangeSqr) continue;

            bool someoneClose = false;

            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None))
            {
                if (player == null) continue;

                if ((player.transform.position - ball.transform.position).sqrMagnitude <= 2.5f * 2.5f)
                {
                    someoneClose = true;
                    break;
                }
            }

            if (someoneClose) owner.StartCoroutine(LaunchBallAfterDelay(ball, 0.75f)); else LaunchBall(ball);
        }

        foreach (GolfCartInfo cart in UnityEngine.Object.FindObjectsByType<GolfCartInfo>(FindObjectsSortMode.None))
        {
            if (cart == null || (cart.transform.position - origin).sqrMagnitude > rangeSqr) continue;

            var rigid = cart.AsEntity.Rigidbody;

            cart.ServerSetMovementSyncDirectionForAllClients(SyncDirection.ServerToClient); // make cart to do what server says

            Vector3 dir = (cart.transform.position - origin).normalized;

            rigid.linearVelocity = rigid.linearVelocity + Vector3.up * 8f + dir * 4f;
            rigid.angularVelocity = UnityEngine.Random.insideUnitSphere * 6f;
        }
    }

    private static IEnumerator<object?> LaunchBallAfterDelay(GolfBall ball, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ball == null || !ball.isServer) yield break;

        LaunchBall(ball);

        yield return null;
    }

    private static void LaunchBall(GolfBall ball)
    {
        ball.AsEntity.Rigidbody.AddForce(Vector3.up * upwardLaunch, ForceMode.VelocityChange);
        VfxManager.ServerPlayPooledVfxForAllClients(VfxType.AirhornPlayerTriggered, ball.transform.position, Quaternion.identity);
    }
}