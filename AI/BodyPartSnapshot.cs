using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// snapshots of the bones and its positions when the body rag dolled
  /// used when transitioning from a rag doll to animated state
  /// </summary>
  public class BodyPartSnapshot
  {
    public Transform transform;

    // prior to starting the animation
    public Vector3 position;
    public Quaternion rotation;
  }
}