using System;
using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// Handles the damage done by the AI to the player
  /// </summary>
  public class AIDamageTrigger : MonoBehaviour
  {
    // we have set animation curves with the parameter name given
    [Tooltip("Name of the Trigger Parameter in the Animator")] [SerializeField]
    private string parameter;

    // decides how many particles should be instantiated at each blow
    [SerializeField] private int bloodParticlesBurstAmount = 10;

    [Tooltip("Damaged done per second basis")] [SerializeField]
    private float damageAmount = 50f;

    // true then triggers sound effects on the Player
    [SerializeField] private bool doDamageSound = true;
    [SerializeField] private bool doPainSound = true;

    private AIStateMachine _stateMachine;
    private Animator _animator;
    private GameSceneManager _gameSceneManager;

    private int _parameterHash = -1;

    private bool _firstContact;


    private void Start()
    {
      _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();

      _animator = _stateMachine.AIAnimator;
      _parameterHash = Animator.StringToHash(parameter);

      _gameSceneManager = GameSceneManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
      if (!_animator) return;

      if (other.gameObject.CompareTag("Player") && _animator.GetFloat(_parameterHash) > 0.9f)
      {
        _firstContact = true;
      }
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

        if (_gameSceneManager != null)
        {
          // get the player info based in the other collider id
          var playerInfo = _gameSceneManager.GetPlayerInfo(other.GetInstanceID());

          if (playerInfo != null && playerInfo.characterManager != null)
          {
            playerInfo.characterManager.TakeDamage(damageAmount, doDamageSound && _firstContact, doPainSound);
          }
        }

        // set the contact to false since it is already been made
        _firstContact = false;
      }
    }
  }
}