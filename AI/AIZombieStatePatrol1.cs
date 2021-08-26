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
    [SerializeField] private AIWaypointNetwork waypointNetwork;

    [Tooltip("Determines whether the Zombie patrols the waypoints in random")] [SerializeField]
    private bool randomPatrol;

    // currently our speeds are 0, 1, 2, and 3
    [Range(0f, 3f)] [SerializeField] private float speed = 1f;

    [SerializeField] private float turnOnSpotThreshold = 80f;

    [SerializeField] private float slerpSpeed = 5f;

    private int _currentWaypoint = 0;

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
      _zombieStateMachine.Speed = speed;
      _zombieStateMachine.Seeking = 0;
      _zombieStateMachine.IsFeeding = false;
      _zombieStateMachine.AttackType = 0;

      if (_zombieStateMachine.TargetType != AITargetType.Waypoint)
      {
        // we clear the Target if it is not a waypoint
        // because other Targets are more important than the waypoint 
        // we also clear the current target if it's reached and AI is patrolling too
        _zombieStateMachine.ClearTarget();

        if (waypointNetwork != null && waypointNetwork.Waypoints.Count > 0)
        {
          if (randomPatrol)
          {
            _currentWaypoint = Random.Range(0, waypointNetwork.Waypoints.Count);
          }

          if (_currentWaypoint < waypointNetwork.Waypoints.Count)
          {
            var waypoint = waypointNetwork.Waypoints[_currentWaypoint];

            if (waypoint != null)
            {
              _zombieStateMachine.SetTarget(AITargetType.Waypoint, null, waypoint.position,
                Vector3.Distance(_zombieStateMachine.transform.position, waypoint.position));

              _zombieStateMachine.navMeshAgent.SetDestination(waypoint.position);
            }
          }
        }
      }

      _zombieStateMachine.navMeshAgent.isStopped = false;
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
            (_zombieStateMachine.visualThreat.distance / _zombieStateMachine.sensorRadius))
        {
          _stateMachine.SetTarget(_stateMachine.visualThreat);
          return AIStateType.Pursuit;
        }
      }

      // patrol logic
      var angle = Vector3.Angle(_zombieStateMachine.transform.forward,
        (_zombieStateMachine.navMeshAgent.steeringTarget - _zombieStateMachine.transform.position));

      if (angle > turnOnSpotThreshold)
      {
        // so we can turn on spot
        // so the animator root motion can handle the rotation
        return AIStateType.Alerted;
      }

      if (!_zombieStateMachine.useRootMotionRotation)
      {
        // manually rotate the AI using a Quaternion slerp

        // get the direction we should be facing
        var newRotation = Quaternion.LookRotation(_zombieStateMachine.navMeshAgent.desiredVelocity);

        // execute a nice smooth slerp from our current quaternion to newRotation quaternion
        _zombieStateMachine.transform.rotation =
          Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRotation, Time.deltaTime * slerpSpeed);
      }

      if (_zombieStateMachine.navMeshAgent.isPathStale || !_zombieStateMachine.navMeshAgent.hasPath ||
          _zombieStateMachine.navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
      {
        // we don't have a waypoint to go to
        // so we just skip and go to the next waypoint
        NextWaypoint();
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
      if (_zombieStateMachine == null || isReached == false) return;

      if (_zombieStateMachine.TargetType == AITargetType.Waypoint)
      {
        // as soon as the waypoint is reach
        // set the next waypoint
        NextWaypoint();
      }
    }

    /// <summary>
    /// sets the next waypoint
    /// </summary>
    private void NextWaypoint()
    {
      if (randomPatrol && waypointNetwork.Waypoints.Count > 1)
      {
        var oldWaypoint = _currentWaypoint;

        while (_currentWaypoint == oldWaypoint)
        {
          _currentWaypoint = Random.Range(0, waypointNetwork.Waypoints.Count);
        }
      }
      else
      {
        _currentWaypoint = _currentWaypoint == waypointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypoint + 1;
      }

      if (waypointNetwork.Waypoints[_currentWaypoint] != null)
      {
        var newWaypoint = waypointNetwork.Waypoints[_currentWaypoint];

        // this is our new target position
        // sets the Target trigger's position
        _zombieStateMachine.SetTarget(AITargetType.Waypoint, null, newWaypoint.position,
          Vector3.Distance(newWaypoint.position, _zombieStateMachine.transform.position));

        // set new Path
        _zombieStateMachine.navMeshAgent.SetDestination(newWaypoint.position);
      }
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

      // look at the waypoint
      // we add the unit up vector so the AI won't look at the ground
      _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.TargetPosition + Vector3.up);

      // set the weight of the waypoint
      _zombieStateMachine.animator.SetLookAtWeight(0.4f);
    }
  }
}