using UnityEngine;

namespace Dead_Earth.Scripts.FPS
{
  /// <summary>
  /// dynamically renders / changes the size of the reticle
  /// based on the player speed 
  /// </summary>
  public class Reticle : MonoBehaviour
  {
    [SerializeField] private FPSController player;

    private RectTransform _rect;
    private float _currentRectSize;

    private const float RunningRectSize = 150;
    private const float WalkingRectSize = 100;

    private void Awake()
    {
      _rect = GetComponent<RectTransform>();

      _currentRectSize = WalkingRectSize;
    }

    private void Update()
    {
      if (player.MovementStatus == PlayerMoveStatus.Running || player.MovementStatus == PlayerMoveStatus.NotGrounded)
      {
        _currentRectSize = Mathf.Lerp(_currentRectSize, RunningRectSize, Time.deltaTime * 2f);
      }
      else
      {
        _currentRectSize = Mathf.Lerp(_currentRectSize, WalkingRectSize, Time.deltaTime * 2f);
      }

      _rect.sizeDelta = new Vector2(_currentRectSize, _currentRectSize);
    }
  }
}