using UnityEngine;

/// <summary>
/// Forwards 2D collision events from bone child colliders on a softbody/jelly ball
/// up to the root Ball component for merge detection.
/// </summary>
public class SlimeBoneRelay : MonoBehaviour
{
    private Ball parentBall;

    public void Init(Ball owner)
    {
        parentBall = owner;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (parentBall != null)
        {
            parentBall.HandleCollision2D(collision);
        }
    }
}
