using Chalk.utils;
using Mirror;
using UnityEngine;

namespace Chalk;

public class BallPatches
{
    public static IEnumerator<object?> SwapBalls()
    {
        yield return null;

        var participants = CourseManager.MatchParticipants?.ToList();
        if (participants == null || participants.Count < 2) yield break;

        const float maxDist = 15f;
        var unmatched = new List<PlayerGolfer>(participants);

        while (unmatched.Count > 1)
        {
            var playerA = unmatched[0];
            unmatched.RemoveAt(0);

            var playerB = unmatched.Where(p => Vector3.Distance(playerA.OwnBall.transform.position, p.OwnBall.transform.position) <= maxDist).OrderBy(p => Vector3.Distance(playerA.OwnBall.transform.position, p.OwnBall.transform.position)).FirstOrDefault();
            if (playerB == null) continue;

            unmatched.Remove(playerB);

            var aBall = playerA.OwnBall;
            var bBall = playerB.OwnBall;

            playerA.NetworkownBall = bBall;
            playerB.NetworkownBall = aBall;
            aBall.Networkowner = playerB;
            bBall.Networkowner = playerA;
        }
    }

    public static IEnumerator<object?> HoleBlocker()
    {
        Vector3 minePos = GolfHoleManager.MainHole.transform.position + Vector3.up * 1.1f;

        Chalk.SpawnServerMine(minePos);

        yield return new WaitForSeconds(10f); // 10 seconds!

        foreach (Landmine mine in UnityEngine.Object.FindObjectsByType<Landmine>(FindObjectsSortMode.None))
        {
            if (mine == null || !mine.TryGetComponent<ChalkMine>(out _) || (mine.transform.position - minePos).sqrMagnitude > .01f) continue;

            NetworkServer.Destroy(mine.gameObject);
            break;
        }
    }
}