using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  public class AIZombieStateAttack1 : AIZombieState
  {
    [SerializeField] [Range(0f, 10f)] private float speed = 0f;
    [SerializeField] private float slerpSpeed = 5f;

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

      // do we have a visual threat that is player
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualPlayer)
      {
        Debug.Log("Visual Player");
        
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
  }
}