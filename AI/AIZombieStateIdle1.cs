using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  public class AIZombieStateIdle1 : AIZombieState
  {
    [Tooltip("Min and Max time range the Zombie will remain Idle")] [SerializeField]
    private Vector2 idleTimeRange = new Vector2(10f, 60f);

    private float _idleTime = 0f;
    private float _timer = 0f;

    /// <summary>
    /// is called when the AI transition to this state
    /// is called in the AIStateMachine class
    /// </summary>
    public override void OnEnterState()
    {
      Debug.Log("Entering Idle State");
      base.OnEnterState();

      if (_zombieStateMachine == null) return;

      // generate random idle time
      _idleTime = Random.Range(idleTimeRange.x, idleTimeRange.y);
      _timer = 0;

      // set the NavMeshAgent properties
      _zombieStateMachine.NavMeshAgentControl(true, false);

      // to be extra cautious and for cleanup when entering a new state
      // configure the state machine
      _zombieStateMachine.Speed = 0;
      _zombieStateMachine.Seeking = 0;
      _zombieStateMachine.IsFeeding = false;
      _zombieStateMachine.AttackType = 0;

      _zombieStateMachine.ClearTarget();
    }

    public override AIStateType GetStateType()
    {
      return AIStateType.Idle;
    }

    /// <summary>
    /// examine the audio and visual threats and if there are transitions to another state
    /// keep track of the current state
    ///
    /// idle state will finish when the timer ends for this Idle1 state
    /// </summary>
    /// <returns></returns>
    public override AIStateType OnUpdate()
    {
      if (_zombieStateMachine == null) return AIStateType.Idle;

      // check for Visual threats
      // if the player is set to be the visual threat
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualPlayer)
      {
        // set it as the new potential target
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);

        // transition to pursuit state
        return AIStateType.Pursuit;
      }

      // if there is a light
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualLight)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);

        // transition to alerted state
        return AIStateType.Alerted;
      }

      if (_zombieStateMachine.audioThreat.type == AITargetType.Audio)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);

        // transition to alerted state
        return AIStateType.Alerted;
      }

      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualFood)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);

        // transition to pursuit state and set the food as the target
        return AIStateType.Pursuit;
      }

      // increment the timer at each second
      _timer += Time.deltaTime;

      if (_timer > _idleTime)
      {
        return AIStateType.Patrol;
      }

      return AIStateType.Idle;
    }
  }
}