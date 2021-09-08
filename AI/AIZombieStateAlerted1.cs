using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  public class AIZombieStateAlerted1 : AIZombieState
  {
    [Tooltip("Maximum seconds the AI is allowed to stay in the alerted state")] [SerializeField] [Range(1f, 60f)]
    private float maxDuration = 10f;

    [Tooltip("Make sure it is less than turnOnSpotThreshold in the Pursuit State")] [SerializeField]
    private float waypointAngleThreshold = 30f;

    [SerializeField] private float threatAngleThreshold = 10f;

    [SerializeField] [Range(1f, 5f)] private float directionChangeDuration = 1.5f;

    [Tooltip("Slerp speed in degrees")] [SerializeField]
    private float slerpSpeed = 45f;

    [SerializeField] private float screamFrequency = 120f;


    private float _timer;
    private float _directionChangeTimer;
    private float _screamChance;

    // next time we are allowed to scream - timer for the next scream
    private float nextScream;


    /// <summary>
    /// is called when the AI transition to this state
    /// is called in the AIStateMachine class
    /// </summary>
    public override void OnEnterState()
    {
      Debug.Log("Entering Alerted State");
      base.OnEnterState();

      if (_zombieStateMachine == null) return;

      // set the NavMeshAgent properties
      _zombieStateMachine.NavMeshAgentControl(true, false);

      // to be extra cautious and for cleanup when entering a new state
      // configure the state machine
      _zombieStateMachine.Speed = 0;
      _zombieStateMachine.Seeking = 0;
      _zombieStateMachine.IsFeeding = false;
      _zombieStateMachine.AttackType = 0;

      _timer = maxDuration;
      _directionChangeTimer = 0f;

      // Random.value returns a value between 0, 1
      // we will scream if the _screamChance is positive
      _screamChance = _zombieStateMachine.ScreamChance - Random.value;
    }

    public override AIStateType GetStateType()
    {
      return AIStateType.Alerted;
    }

    public override AIStateType OnUpdate()
    {
      _timer -= Time.deltaTime;
      _directionChangeTimer += Time.deltaTime;

      if (_timer <= 0f)
      {
        // get the waypoint you are currently visiting before going into the alerted state
        _zombieStateMachine.AINavMeshAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));

        // resume patrolling
        _zombieStateMachine.AINavMeshAgent.isStopped = false;
        _timer = maxDuration;
      }

      // when we have a visual of the Player - highest priority
      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualPlayer)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);

        // scream
        if (_screamChance > 0f && Time.time > nextScream)
        {
          if (_zombieStateMachine.Scream())
          {
            // make sure do not trigger this again when we are staying in the Alerted state without leaving
            _screamChance = float.MinValue;
            return AIStateType.Alerted;
          }

          nextScream = Time.time + screamFrequency;
        }

        return AIStateType.Pursuit;
      }

      if (_zombieStateMachine.audioThreat.type == AITargetType.Audio)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);

        // heard the audio again so reset the timer
        // we do not return so we can overwrite if there is a visual light threat
        _timer = maxDuration;
      }

      if (_zombieStateMachine.visualThreat.type == AITargetType.VisualLight)
      {
        _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);

        // reset the timer when the AI sees the light again 
        _timer = maxDuration;
      }

      if (_zombieStateMachine.audioThreat.type == AITargetType.None &&
          _zombieStateMachine.CurrentTargetType == AITargetType.None &&
          _zombieStateMachine.visualThreat.type == AITargetType.VisualFood)
      {
        _zombieStateMachine.SetTarget(_stateMachine.visualThreat);

        return AIStateType.Pursuit;
      }


      // handle turn on spot
      float angle;

      // handle the case where the threat is Audio or Visual Light
      if ((_zombieStateMachine.CurrentTargetType == AITargetType.Audio ||
           _zombieStateMachine.CurrentTargetType == AITargetType.VisualLight) && !_zombieStateMachine.IsTargetReached)
      {
        angle = FindSignedAngle(_zombieStateMachine.transform.forward,
          _zombieStateMachine.CurrentTargetPosition - _zombieStateMachine.transform.position);

        if (_zombieStateMachine.CurrentTargetType == AITargetType.Audio && Mathf.Abs(angle) < threatAngleThreshold)
        {
          return AIStateType.Pursuit;
        }

        if (_directionChangeTimer > directionChangeDuration)
        {
          if (Random.value < _zombieStateMachine.Intelligence)
          {
            // make an informed decision
            // set seeking to either  +1 or -1
            _zombieStateMachine.Seeking = (int)Mathf.Sign(angle);
          }
          else
          {
            // make a random decision
            _zombieStateMachine.Seeking = (int)Mathf.Sign(Random.Range(-1f, 1f));
          }

          // reset timer
          _directionChangeTimer = 0f;
        }
      }

      // handle the waypoint case on patrol state
      else if (_zombieStateMachine.CurrentTargetType == AITargetType.Waypoint &&
               !_zombieStateMachine.AINavMeshAgent.pathPending)
      {
        angle = FindSignedAngle(_zombieStateMachine.transform.forward,
          _zombieStateMachine.AINavMeshAgent.steeringTarget - _zombieStateMachine.transform.position);

        if (Mathf.Abs(angle) < waypointAngleThreshold)
        {
          // go back to patrol if we fall below the waypoint threshold
          return AIStateType.Patrol;
        }

        // turn on spot until the current angle towards the steering target is less than the waypoint angle threshold
        _zombieStateMachine.Seeking = (int)Mathf.Sign(angle);
      }

      else
      {
        // not a waypoint, not a visual or audio threat
        // so zombie stays alerted when they go to the source of light or audio
        // but the threat is not there anymore
        if (_directionChangeTimer > directionChangeDuration)
        {
          _zombieStateMachine.Seeking = (int)Mathf.Sign(Random.Range(-1f, 1f));
          _directionChangeTimer = 0f;
        }
      }

      // handle rotation for the crawling state
      if (!_zombieStateMachine.useRootMotionRotation)
      {
        _zombieStateMachine.transform
          .Rotate(new Vector3(0f, slerpSpeed * _zombieStateMachine.Seeking * Time.deltaTime, 0f));
      }

      return AIStateType.Alerted;
    }
  }
}