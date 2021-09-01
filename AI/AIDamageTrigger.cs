using System;
using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// Handles the damage done by the AI to the player
  /// </summary>
  public class AIDamageTrigger : MonoBehaviour
  {
    [Tooltip("Name of the Trigger Parameter in the Animator")] [SerializeField]
    private string parameter;

    // decides how many particles should be instantiated at each blow
    [SerializeField] private int bloodParticlesBurstAmount = 10;

    private AIStateMachine _stateMachine;
    private Animator _animator;

    private int _parameterHash = -1;


    private void Start()
    {
      _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();

      _animator = _stateMachine.AIAnimator;
      _parameterHash = Animator.StringToHash(parameter);
    }

    private void OnTriggerStay(Collider other)
    {
      if (_animator == null) return;

      if (other.gameObject.CompareTag("Player") && _animator.GetFloat(_parameterHash) > 0.9f)
      {
        // instantiate blood particles
        if (GameSceneManager.Instance && GameSceneManager.Instance.BloodParticleSystem)
        {
          var particleSystem = GameSceneManager.Instance.BloodParticleSystem;

          particleSystem.transform.position = transform.position;
          particleSystem.transform.rotation = Camera.main.transform.rotation; // splat towards the camera

          var mainModule = particleSystem.main;
          mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

          particleSystem.Emit(bloodParticlesBurstAmount);
        }

        Debug.Log("Player is damaged");
      }
    }
  }
}