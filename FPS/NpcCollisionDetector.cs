using System;
using Dead_Earth.Scripts.AI;
using UnityEngine;

namespace Dead_Earth.Scripts.FPS
{
  /// <summary>
  /// handles the detection of sticky collisions
  /// </summary>
  public class NpcCollisionDetector : MonoBehaviour
  {
    private FPSController _fpsController;

    private void Start()
    {
      _fpsController = GetComponentInParent<FPSController>();
    }

    private void OnTriggerStay(Collider other)
    {
      var aiStateMachine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());

      if (aiStateMachine != null && _fpsController != null)
      {
        // slow down the player
        _fpsController.HandleNpcCollision();

        // set the visual threat of the AI State State machine collided to the player
        // so the player won't be able to sneak from behind
        aiStateMachine.visualThreat.Set(AITargetType.VisualPlayer, _fpsController.FpsCharacterController,
          _fpsController.transform.position,
          Vector3.Distance(aiStateMachine.transform.position, _fpsController.transform.position));

        // enforce the AI into the Attack state so it immediately turns and starts attacking
        aiStateMachine.SetStateOverride(AIStateType.Attack);
      }
    }
  }
}