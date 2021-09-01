using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  public class AIZombieStateAttack1 : AIZombieState
  {
    [SerializeField] [Range(0f, 10f)] private float speed = 2f;

    [Tooltip("Distance between the FPS and the AI where AI stops moving its legs")] [SerializeField]
    private float stoppingDistance = 1.2f;

    [SerializeField] private float slerpSpeed = 5f;

    [SerializeField] [Range(0f, 1f)] private float lookAtWeight = 0.7f;
    [SerializeField] [Range(0f, 90f)] private float lookAtAngleThreshold = 15f;

    private float _currentLookAtWeight;

    public override void OnEnterState()
    {
      Debug.Log("Entering Attack State");

      base.OnEnterState();
      if (_zombieStateMachine == null) return;

      // set the NavMeshAgent properties
      _zombieStateMachine.NavMeshAgentControl(true, false);

      // to be extra cautious and for cleanup when entering a new state
      // configure the state machine
      _zombieStateMachine.Speed = speed;
      _zombieStateMachine.Seeking = 0;
      _zombieStateMachine.IsFeeding = false;

      // set the attack type between 1 to 100 which is the percent chance
      // the attack generated will play the attack animation 
      // which the generated attack falls in its attack range
      _zombieStateMachine.AttackType = Random.Range(1, 100);

      _currentLookAtWeight = 0f;
    }

    public override void OnExitState()
    {
      base.OnExitState();

      _zombieStateMachine.AttackType = 0;
    }

    public override AIStateType GetStateType()
    {
      return AIStateType.Attack;
    }

    public override AIStateType OnUpdate()
    {
      Vector3 targetPos;
      Quaternion newRot;

      if (Vector3.Distance(_zombieStateMachine.transform.position, _zombieStateMachine.CurrentTargetPosition) <
          stoppingDistance)
      {
        _zombieStateMachine.Speed = 0;
      }
      else
      {
        _zombieStateMachine.Speed = speed;
      }

      // do we have a visual threat that is player
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualPlayer)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);

        // if we are not in melee range any more than fall back to pursuit mode
        if (!_zombieStateMachine.IsInMeleeRange)
        {
          return AIStateType.Pursuit;
        }

        if (!_zombieStateMachine.useRootMotionRotation)
        {
          // keep the zombie facing the player at all times
          targetPos = _zombieStateMachine.CurrentTargetPosition;
          targetPos.y = _zombieStateMachine.transform.position.y;
          newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);

          _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot,
            slerpSpeed * Time.deltaTime);
        }

        // generate a new attack integer
        _zombieStateMachine.AttackType = Random.Range(1, 100);

        return AIStateType.Attack;
      }

      // the player is managed to get out of the sight of the zombie
      // so stay alerted if no longer able to see the zombie
      if (!_zombieStateMachine.useRootMotionRotation)
      {
        // keep the zombie facing the player at all times
        targetPos = _zombieStateMachine.CurrentTargetPosition;
        targetPos.y = _zombieStateMachine.transform.position.y;
        newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);

        _zombieStateMachine.transform.rotation = newRot;
      }

      return AIStateType.Alerted;
    }

    /// <summary>
    /// called in the OnAnimatorIK
    /// we can set the Humanoid parts on it
    ///
    /// Overrides IK Goals
    /// </summary>
    public override void OnAnimatorIKUpdated()
    {
      base.OnAnimatorIKUpdated();

      if (_zombieStateMachine == null) return;

      if (Vector3.Angle(_zombieStateMachine.transform.forward,
        _zombieStateMachine.CurrentTargetPosition - _zombieStateMachine.SensorPosition) < lookAtAngleThreshold)
      {
        // look at the waypoint
        // we add the unit up vector so the AI won't look at the ground
        _zombieStateMachine.AIAnimator.SetLookAtPosition(_zombieStateMachine.CurrentTargetPosition + Vector3.up);

        // set the weight of the look to make it natural
        _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, lookAtWeight, Time.deltaTime);

        _zombieStateMachine.AIAnimator.SetLookAtWeight(_currentLookAtWeight);
      }
      else
      {
        // as we leave the attack state slowly blend to not look at again
        _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, 0, Time.deltaTime);

        _zombieStateMachine.AIAnimator.SetLookAtWeight(_currentLookAtWeight);
      }
    }
  }
}