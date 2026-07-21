using Chalk.utils;
using Mirror;
using UnityEngine;

namespace Chalk;

public class BallPatches
{
    public static IEnumerator<object?> SwapBalls()
    {
        yield return null;

        var participants = CourseManager.MatchParticipants;
        if (participants == null || participants.Count < 2) yield break;

        var players = participants.ToList();
        var balls = players.Select(g => g.OwnBall).ToList();
        List<GolfBall> shuffled;

        do
        {
            shuffled = balls.OrderBy(_ => UnityEngine.Random.value).ToList();
        } while (shuffled.Where((ball, i) => ball == players[i].OwnBall).Any());

        foreach (var (golfer, newBall) in players.Zip(shuffled, (p, b) => (p, b)))
        {
            golfer.NetworkownBall = newBall;
            newBall.Networkowner = golfer;
        }
    }

    public static IEnumerator<object?> HoleBlocker()
    {
        Vector3 minePos = GolfHoleManager.MainHole.transform.position + Vector3.up * 1.1f;

        Chalk.SpawnServerMine(minePos);

        yield return new WaitForSeconds(15f); // 15 seconds!

        foreach (Landmine mine in UnityEngine.Object.FindObjectsByType<Landmine>(FindObjectsSortMode.None))
        {
            if (mine == null || !mine.TryGetComponent<ChalkMine>(out _) || (mine.transform.position - minePos).sqrMagnitude > .01f) continue;

            NetworkServer.Destroy(mine.gameObject);
            break;
        }
    }
}