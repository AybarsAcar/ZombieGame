using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// feeding state for the Zombie AI
  /// </summary>
  public class AIZombieStateFeeding1 : AIZombieState
  {
    [SerializeField] private float slerpSpeed = 5f;

    [SerializeField] private Transform bloodParticlesMount;
    [SerializeField] [Range(0.01f, 1f)] private float bloodParticlesBurstTime = 0.01f;
    [SerializeField] [Range(1, 100)] private int bloodParticlesBurstAMount = 10;

    private float _timer = 0;

    // Animator Hashes
    private int _eatingStateHash = Animator.StringToHash("Feeding State");
    private int _eatingLayerIndex = -1;


    public override void OnEnterState()
    {
      Debug.Log("Entering Feeding State");
      base.OnEnterState();

      if (_zombieStateMachine == null) return;

      // get the layer index if not fetched already
      if (_eatingLayerIndex == -1)
      {
        _eatingLayerIndex = _zombieStateMachine.AIAnimator.GetLayerIndex("Cinematic");
      }

      _timer = 0f;

      // set the NavMeshAgent properties
      _zombieStateMachine.NavMeshAgentControl(true, false);

      // configure the state machine
      _zombieStateMachine.IsFeeding = true;
      _zombieStateMachine.Speed = 0;
      _zombieStateMachine.Seeking = 0;
      _zombieStateMachine.AttackType = 0;
    }

    public override void OnExitState()
    {
      base.OnExitState();
      if (_zombieStateMachine == null) return;

      _zombieStateMachine.IsFeeding = false;
    }

    public override AIStateType GetStateType()
    {
      return AIStateType.Feeding;
    }

    public override AIStateType OnUpdate()
    {
      _timer += Time.deltaTime;

      if (_zombieStateMachine.Satisfaction > 0.9f)
      {
        _zombieStateMachine.GetWaypointPosition(false);
        return AIStateType.Alerted;
      }

      // examine the threat assessments
      // if the visual threat is not food (player or light)
      if (_zombieStateMachine.visualThreat.type != AITargetType.None &&
          _zombieStateMachine.visualThreat.type != AITargetType.VisualFood)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
        return AIStateType.Alerted;
      }

      // if we hear something
      if (_zombieStateMachine.audioThreat.type == AITargetType.Audio)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
        return AIStateType.Alerted;
      }


      // is the feeding animation currently playing now
      if (_zombieStateMachine.AIAnimator.GetCurrentAnimatorStateInfo(_eatingLayerIndex).shortNameHash ==
          _eatingStateHash)
      {
        // add to satisfaction
        _zombieStateMachine.Satisfaction =
          Mathf.Min(_zombieStateMachine.Satisfaction + (Time.deltaTime * _zombieStateMachine.ReplenishRate) / 100f, 1f);

        // start emitting the blood particle effect while eating
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.BloodParticleSystem != null)
        {
          if (_timer > bloodParticlesBurstTime)
          {
            // emit the particle
            var particleSystem = GameSceneManager.Instance.BloodParticleSystem;

            particleSystem.transform.position = bloodParticlesMount.position;
            particleSystem.transform.rotation = bloodParticlesMount.rotation;

            var mainParticle = particleSystem.main;
            mainParticle.simulationSpace = ParticleSystemSimulationSpace.World;

            particleSystem.Emit(bloodParticlesBurstAMount);

            _timer = 0f;
          }
        }
      }

      if (!_zombieStateMachine.useRootMotionRotation)
      {
        // manually rotate the AI using a Quaternion slerp
        // so the zombie faces the target it is feeding on

        // keep the zombie facing the player at all times
        var targetPos = _zombieStateMachine.CurrentTargetPosition;
        targetPos.y = _zombieStateMachine.transform.position.y;

        // get the direction we should be facing
        var newRotation = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);

        // execute a nice smooth slerp from our current quaternion to newRotation quaternion
        _zombieStateMachine.transform.rotation =
          Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRotation, Time.deltaTime * slerpSpeed);
      }


      // default state
      return AIStateType.Feeding;
    }
  }
}