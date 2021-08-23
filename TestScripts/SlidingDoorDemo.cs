using System.Collections;
using UnityEngine;

namespace Dead_Earth.Scripts.TestScripts
{
  public enum DoorState
  {
    Open,
    Animating,
    Closed
  };

  /// <summary>
  /// handles the sliding door behaviour
  /// </summary>
  public class SlidingDoorDemo : MonoBehaviour
  {
    public float slidingDistance = 4f;
    public float duration = 1.5f;
    public AnimationCurve curve = new AnimationCurve();

    private Transform _transform;
    private Vector3 _openPos = Vector3.zero;
    private Vector3 _closedPos = Vector3.zero;
    private DoorState _doorState = DoorState.Closed;

    private void Start()
    {
      _transform = transform;

      // calculate the closed position
      _closedPos = _transform.position;

      // calculate the open position
      _openPos = _closedPos + _transform.right * slidingDistance;
    }

    private void Update()
    {
      if (Input.GetKeyDown(KeyCode.Space) && _doorState != DoorState.Animating)
      {
        var newState = _doorState == DoorState.Open ? DoorState.Closed : DoorState.Open;

        StartCoroutine(AnimateDoor(newState));
      }
    }

    /// <summary>
    /// changes the door state at the end of the animation
    /// </summary>
    /// <param name="state">state we want it to be</param>
    /// <returns></returns>
    private IEnumerator AnimateDoor(DoorState state)
    {
      _doorState = DoorState.Animating;

      var timeElapsed = 0f;

      var startPos = state == DoorState.Open ? _closedPos : _openPos;
      var endPos = state == DoorState.Open ? _openPos : _closedPos;

      while (timeElapsed < duration)
      {
        var t = timeElapsed / duration;

        _transform.position = Vector3.Lerp(startPos, endPos, curve.Evaluate(t));

        timeElapsed += Time.deltaTime;

        yield return null;
      }

      _transform.position = endPos;
      _doorState = state;
    }
  }
}