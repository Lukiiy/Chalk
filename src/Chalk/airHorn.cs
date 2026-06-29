using HarmonyLib;
using UnityEngine;
using Mirror;

namespace Chalk;

[HarmonyPatch]
public static class Airhorn
{
    private const float rangeMult = 1.2f;
    private const float upwardLaunch = 25f;
    private const float cartTiltForce = 18f;

    [HarmonyPatch(typeof(PlayerInventory), "UserCode_CmdPlayAirhornVfxForAllClients__NetworkConnectionToClient")]
    [HarmonyPostfix]
    private static void Used(PlayerInventory __instance)
    {
        if (!Chalk.airHornExtra.Value || __instance == null || !NetworkServer.active) return;

        var owner = __instance.PlayerInfo;
        if (owner == null) return;

        float rangeSqr = (float) Math.Pow(GameManager.ItemSettings.AirhornRange * rangeMult, 2);

        foreach (var ball in UnityEngine.Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None))
        {
            if (ball == null || !ball.isServer || (ball.transform.position - owner.transform.position).sqrMagnitude > rangeSqr) continue;

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

        foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None))
        {
            if (player == null || player == owner || (player.transform.position - owner.transform.position).sqrMagnitude > rangeSqr) continue;

            Rigidbody body = player.GetComponentInChildren<Rigidbody>();
            if (body == null) continue;

            Vector3 tilt = new(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-1f, 1f));

            body.AddTorque(tilt.normalized * cartTiltForce, ForceMode.VelocityChange);
        }
    }

    private static IEnumerator<object> LaunchBallAfterDelay(GolfBall ball, float delay)
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