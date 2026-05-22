using HarmonyLib;
using UnityEngine;

namespace Chalk;

public class BallPatches
{
    private const float pushRadius = 0.8f;
    private const float pushForce = 3f;
    private const float pushRadiusSq = pushRadius * pushRadius;

    public static void Update()
    {
        if (!Chalk.IsMatchActive()) return;

        var players = CourseManager.ServerMatchParticipants;
        if (players == null) return;

        var balls = UnityEngine.Object.FindObjectsByType<GolfBall>(FindObjectsSortMode.None);
        if (balls.Length == 0) return;

        foreach (PlayerGolfer golfer in players)
        {
            if (golfer == null) continue;

            var movement = golfer?.PlayerInfo?.Movement;
            if (movement == null) return;

            Vector3 playerPos = movement.transform.position;

            foreach (GolfBall ball in balls)
            {
                if (ball == null) return;
                if (ball.Networkowner == golfer) continue;

                Vector3 delta = ball.Rigidbody.position - playerPos;
                delta.y = 0f;

                if (delta.sqrMagnitude > pushRadiusSq || delta.sqrMagnitude < 0.0001f) continue;

                float strength = Mathf.InverseLerp(pushRadius, 0f, delta.magnitude) * pushForce;

                ball.Rigidbody.AddForce(delta.normalized * strength, ForceMode.Impulse);
            }
        }
    }
}