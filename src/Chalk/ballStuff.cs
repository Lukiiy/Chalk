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
}