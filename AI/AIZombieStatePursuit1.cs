using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.AI
{
  public class AIZombieStatePursuit1 : AIZombieState
  {
    [Range(0f, 10f)] [SerializeField] private float speed = 3f;
    [SerializeField] private float slerpSpeed = 5f;

    [Tooltip("Maximum time the AI can chase the Target before it stops to rest / give up")] [SerializeField]
    private float maxPursuitDuration = 40f;

    // used to scale the distance between the target and the Zombie
    // setting the number of fraction of seconds until the next repath should be done
    [Tooltip("Decides how ofter NavMeshPath is generated based on the distance to AI")] [SerializeField]
    private float repathDistanceMultiplier = 0.035f;

    // however close the Zombie close to the Target
    // we still have a min and max duration for NavMesh to regenerate a new path
    [Tooltip("Minimum frequency to generate a new NavMeshPath to the target")] [SerializeField]
    private float repathVisualMinDuration = 0.05f;

    [Tooltip("Maximum frequency to generate a new NavMeshPath to the target")] [SerializeField]
    private float repathVisualMaxDuration = 5f;

    // Audio Targets are have static transforms so we dont have to calculate that often
    [SerializeField] private float repathAudioMinDuration = 0.25f;
    [SerializeField] private float repathAudioMaxDuration = 5;

    private float _timer;
    private float _repathTimer; // set to 0 each time we repath


    public override void OnEnterState()
    {
      Debug.Log("Entering Pursuit State");
      base.OnEnterState();

      if (_zombieStateMachine == null) return;

      // set the NavMeshAgent properties
      _zombieStateMachine.NavMeshAgentControl(true, false);

      // to be extra cautious and for cleanup when entering a new state
      // configure the state machine
      _zombieStateMachine.Seeking = 0;
      _zombieStateMachine.IsFeeding = false;
      _zombieStateMachine.AttackType = 0;

      // set local variables to keep track
      _timer = 0f;
      _repathTimer = 0f;

      // set path
      _zombieStateMachine.AINavMeshAgent.SetDestination(_zombieStateMachine.CurrentTargetPosition);
      _zombieStateMachine.AINavMeshAgent.isStopped = false;
    }

    public override AIStateType GetStateType()
    {
      return AIStateType.Pursuit;
    }

    public override AIStateType OnUpdate()
    {
      _timer += Time.deltaTime;
      _repathTimer += Time.deltaTime;

      // check how much time has passed
      // we dont want the AI to relentlessly chase
      if (_timer > maxPursuitDuration)
      {
        return AIStateType.Patrol;
      }

      
      // if chasing the Player and now is in its melee attack range
      if (_zombieStateMachine.CurrentTargetType == AITargetType.VisualPlayer && _zombieStateMachine.IsInMeleeRange)
      {
        return AIStateType.Attack;
      }

      // otherwise this is navigation to areas of interest so use the standard target threshold
      if (_zombieStateMachine.IsTargetReached)
      {
        switch (_stateMachine.CurrentTargetType)
        {
          case AITargetType.Audio:
          case AITargetType.VisualLight:
            _stateMachine.ClearTarget(); // clear the target
            return AIStateType.Alerted; // become alert and scan for targets

          case AITargetType.VisualFood:
            return AIStateType.Feeding;
        }
      }

      // if for any reason the nav agent has lost its path then call then drop into alerted state
      // so it will try to re-acquire the target or eventually give up and resume patrolling
      if (_zombieStateMachine.AINavMeshAgent.isPathStale ||
          (!_zombieStateMachine.AINavMeshAgent.hasPath && !_zombieStateMachine.AINavMeshAgent.pathPending) ||
          _zombieStateMachine.AINavMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
      {
        return AIStateType.Alerted;
      }

      if (_zombieStateMachine.AINavMeshAgent.pathPending)
      {
        _zombieStateMachine.Speed = 0;
      }
      else
      {
        // set the speed of the zombie
        _zombieStateMachine.Speed = speed;

        // if we are close to the target that was a player and we still have the player in our vision
        // and within the target range but not yet in the melee range
        if (!_zombieStateMachine.useRootMotionRotation &&
            _zombieStateMachine.CurrentTargetType == AITargetType.VisualPlayer &&
            _zombieStateMachine.visualThreat.type == AITargetType.VisualPlayer && _zombieStateMachine.IsTargetReached)
        {
          // rotate the zombie at each frame so it is harder to dodge
          var targetPos = _zombieStateMachine.CurrentTargetPosition;
          targetPos.y = _zombieStateMachine.transform.position.y;

          var newRotation = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);

          // we dont want any slerp when we are close to the Target Player
          _zombieStateMachine.transform.rotation = newRotation;
        }

        // generic pursuit state
        // slowly update our rotation to match the nav agents desired rotation but only if we are pursuing the player
        else if (!_stateMachine.useRootMotionRotation && !_zombieStateMachine.IsTargetReached)
        {
          // generate a new Quaternion representing the rotation we should have
          var newRotation = Quaternion.LookRotation(_zombieStateMachine.AINavMeshAgent.desiredVelocity);

          // smoothly rotate to that new rotation over time
          _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRotation,
            slerpSpeed * Time.deltaTime);
        }

        // we reached the destination but the destination is not the Player
        // like audio source and see if there is a player etc
        else if (_zombieStateMachine.IsTargetReached)
        {
          return AIStateType.Alerted;
        }
      }


      // do we have a visual threat that is the player
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualPlayer)
      {
        // the position is different - maybe same threat but it has moved so repath periodically
        if (_zombieStateMachine.CurrentTargetPosition != _zombieStateMachine.visualThreat.position)
        {
          // repath more frequently as we get closer to the target (try and save some CPU cycles)
          if (Mathf.Clamp(_zombieStateMachine.visualThreat.distance * repathDistanceMultiplier, repathVisualMinDuration,
            repathVisualMaxDuration) < _repathTimer)
          {
            // time to do a repath
            _zombieStateMachine.AINavMeshAgent.SetDestination(_zombieStateMachine.visualThreat.position);
            _repathTimer = 0;
          }
        }

        // make sure to set this as the current target
        _stateMachine.SetTarget(_zombieStateMachine.visualThreat);

        return AIStateType.Pursuit;
      }

      // if our target is the last sighting of a player then remain in pursuit as nothing else can override
      if (_zombieStateMachine.CurrentTargetType == AITargetType.VisualPlayer)
      {
        return AIStateType.Pursuit;
      }

      // player is not currently in the view cone and registering itself as a visual threat
      // our CurrentTarget is not of type VisualPlayer - so it is a lower priority of that
      // handle the remaining threats / targets

      // if we have a visual threat that is the player's light
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualLight)
      {
        // and we currently have a lower priority target then drop into alerted mode
        // and try to find the source of light
        if (_zombieStateMachine.CurrentTargetType == AITargetType.Audio ||
            _zombieStateMachine.CurrentTargetType == AITargetType.VisualFood)
        {
          _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
          return AIStateType.Alerted;
        }

        if (_zombieStateMachine.CurrentTargetType == AITargetType.VisualLight)
        {
          var currentId = _zombieStateMachine.CurrentTargetColliderId;

          // if this is the same light
          if (currentId == _zombieStateMachine.visualThreat.collider.GetInstanceID())
          {
            // the position is different - maybe the same threat but it has moved so repath periodically
            if (_zombieStateMachine.CurrentTargetPosition != _zombieStateMachine.visualThreat.position)
            {
              if (Mathf.Clamp(_zombieStateMachine.visualThreat.distance * repathDistanceMultiplier,
                repathVisualMinDuration,
                repathVisualMaxDuration) < _repathTimer)
              {
                // repath the agent
                _zombieStateMachine.AINavMeshAgent.SetDestination(_zombieStateMachine.visualThreat.position);
                _repathTimer = 0f;
              }
            }

            _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
            return AIStateType.Pursuit;
          }

          // ids are different so we have a new source
          _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
          return AIStateType.Alerted;
        }
      }

      else if (_zombieStateMachine.audioThreat.type == AITargetType.Audio)
      {
        if (_zombieStateMachine.CurrentTargetType == AITargetType.VisualFood)
        {
          _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
          return AIStateType.Alerted;
        }

        if (_zombieStateMachine.CurrentTargetType == AITargetType.Audio)
        {
          var currentId = _zombieStateMachine.CurrentTargetColliderId;

          // check if its the same Audio Source - periodically repath so the sources can move i.e footsteps
          if (currentId == _zombieStateMachine.audioThreat.collider.GetInstanceID())
          {
            // the position is different - maybe same threat but it has moved so repath periodically
            if (_zombieStateMachine.CurrentTargetPosition != _zombieStateMachine.audioThreat.position)
            {
              if (Mathf.Clamp(_zombieStateMachine.audioThreat.distance * repathDistanceMultiplier,
                repathAudioMinDuration,
                repathAudioMaxDuration) < _repathTimer)
              {
                // repath the agent
                _zombieStateMachine.AINavMeshAgent.SetDestination(_zombieStateMachine.audioThreat.position);
                _repathTimer = 0f;
              }
            }

            _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
            return AIStateType.Pursuit;
          }

          // different instance id
          _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
          return AIStateType.Alerted;
        }
      }

      // Default
      return AIStateType.Pursuit;
    }
  }
}