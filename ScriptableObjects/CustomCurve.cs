using UnityEngine;

namespace Dead_Earth.Scripts.ScriptableObjects
{
  /// <summary>
  /// to create a custom curve
  /// used for the animations that are pre-baked like the Mixamo animations
  /// </summary>
  [CreateAssetMenu(fileName = "New Custom Curve", menuName = "Dead Earth/Audio/Custom Animation Curve", order = 0)]
  public class CustomCurve : ScriptableObject
  {
    [SerializeField] private AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));

    public float Evaluate(float t)
    {
      return curve.Evaluate(t);
    }
  }
}