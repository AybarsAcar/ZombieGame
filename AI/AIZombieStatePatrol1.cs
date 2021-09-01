using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// Patrol state of the Zombie AI - Type 1 
  /// </summary>
  public class AIZombieStatePatrol1 : AIZombieState
  {
    // currently our speeds are 0, 1, 2, and 3
    [Range(0f, 3f)] [SerializeField] private float speed = 1f;

    [SerializeField] private float turnOnSpotThreshold = 90f;

    [SerializeField] private float slerpSpeed = 5f;


    /// <summary>
    /// called when AI enters this State
    /// </summary>
    public override void OnEnterState()
    {
      Debug.Log("Entering Patrol State");
      base.OnEnterState();

      if (_zombieStateMachine == null) return;

      // set the NavMeshAgent properties
      _zombieStateMachine.NavMeshAgentControl(true, false);

      // to be extra cautious and for cleanup when entering a new state
      // configure the state machine
      _zombieStateMachine.Seeking = 0;
      _zombieStateMachine.IsFeeding = false;
      _zombieStateMachine.AttackType = 0;

      // Set Destination
      _zombieStateMachine.AINavMeshAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));

      _zombieStateMachine.AINavMeshAgent.isStopped = false;
    }

    public override AIStateType GetStateType()
    {
      return AIStateType.Patrol;
    }

    public override AIStateType OnUpdate()
    {
      // do we have a visual threat that is the player
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualPlayer)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
        return AIStateType.Pursuit;
      }

      // check for the player light
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualLight)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
        return AIStateType.Alerted;
      }

      // sound is the third highest priority
      if (_zombieStateMachine.audioThreat.type == AITargetType.Audio)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
        return AIStateType.Alerted;
      }

      // food is the fourth highest priority
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualFood)
      {
        // if the distance to hunger ratio means we are hungry enough to go to the food source
        if ((1.0f - _zombieStateMachine.Satisfaction) >
            (_zombieStateMachine.visualThreat.distance / _zombieStateMachine.SensorRadius))
        {
          _stateMachine.SetTarget(_stateMachine.visualThreat);
          return AIStateType.Pursuit;
        }
      }

      // set the speed
      // if the path is still being computed then wait
      if (_zombieStateMachine.AINavMeshAgent.pathPending)
      {
        _zombieStateMachine.Speed = 0;
        return AIStateType.Patrol;
      }

      // set the speed as the path is now computed
      _zombieStateMachine.Speed = speed;

      // patrol logic
      var angle = Vector3.Angle(_zombieStateMachine.transform.forward,
        (_zombieStateMachine.AINavMeshAgent.steeringTarget - _zombieStateMachine.transform.position));

      if (angle > turnOnSpotThreshold)
      {
        // so we can turn on spot
        // so the animator root motion can handle the rotation
        // Alerted State will be used to turn on spot when patrolling too
        return AIStateType.Alerted;
      }

      if (!_zombieStateMachine.useRootMotionRotation)
      {
        // manually rotate the AI using a Quaternion slerp

        // get the direction we should be facing
        var newRotation = Quaternion.LookRotation(_zombieStateMachine.AINavMeshAgent.desiredVelocity);

        // execute a nice smooth slerp from our current quaternion to newRotation quaternion
        _zombieStateMachine.transform.rotation =
          Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRotation, Time.deltaTime * slerpSpeed);
      }

      if (_zombieStateMachine.AINavMeshAgent.isPathStale || !_zombieStateMachine.AINavMeshAgent.hasPath ||
          _zombieStateMachine.AINavMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
      {
        // we don't have a waypoint to go to
        // so we just skip and go to the next waypoint
        _zombieStateMachine.GetWaypointPosition(true);
      }

      // if no change keep patrolling
      return AIStateType.Patrol;
    }

    /// <summary>
    /// it is called when AI NavMeshAgent reaches the destination
    /// destination is reached if the AI enters its Target Trigger 
    /// </summary>
    /// <param name="isReached"></param>
    public override void OnDestinationReached(bool isReached)
    {
      // TODO: Issue called too many times
      Debug.Log("DESTINATION REACHED CALLED");
      if (_zombieStateMachine == null || isReached == false) return;

      if (_zombieStateMachine.CurrentTargetType == AITargetType.Waypoint)
      {
        // as soon as the waypoint is reach
        // set the next waypoint
        _zombieStateMachine.GetWaypointPosition(true);
      }
    }
  }
}