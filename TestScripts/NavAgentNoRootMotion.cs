using System.Collections;
using Dead_Earth.Scripts.AI;
using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.TestScripts
{
  /// <summary>
  /// Nav agent script that sets the speed of the nav mesh agent
  /// the character's animation has no Apply Root Motion
  /// the Transform of the gameObject is fully controller by the NavMeshAgent  
  /// </summary>
  [RequireComponent(typeof(NavMeshAgent))]
  public class NavAgentNoRootMotion : MonoBehaviour
  {
    [SerializeField] private AIWaypointNetwork waypointNetwork;

    public int CurrentIndex = 0;
    public AnimationCurve jumpCurve;

    private NavMeshAgent _navMeshAgent;
    private Animator _animator;

    private bool _isProcessingOffMeshLink;
    private float _initialMaxSpeed;

    private void Awake()
    {
      _navMeshAgent = GetComponent<NavMeshAgent>();
      _animator = GetComponent<Animator>();
    }

    private void Start()
    {
      SetNextDestination(false);

      _initialMaxSpeed = _navMeshAgent.speed;
    }

    private void Update()
    {
      int turnOnSpot;

      // cross product to get our angular speed and direction
      var cross = Vector3.Cross(transform.forward, _navMeshAgent.desiredVelocity.normalized);

      // get the rotation direction
      var horizontal = cross.y < 0 ? -cross.magnitude : cross.magnitude;
      horizontal = Mathf.Clamp(horizontal * 4.32f, -2.32f, 2.32f);

      // turn on spot detection
      // if we fall below 1f speed we stop our agent
      // and only enter if the turn angle is larger than 20 degrees
      if (_navMeshAgent.desiredVelocity.magnitude < 1f &&
          Vector3.Angle(transform.forward, _navMeshAgent.desiredVelocity) > 10f)
      {
        _navMeshAgent.speed = 0.1f;
        turnOnSpot = (int)Mathf.Sign(horizontal);
      }
      else
      {
        _navMeshAgent.speed = _initialMaxSpeed;
        turnOnSpot = 0;
      }

      // pass the horizontal speed to the animator
      _animator.SetFloat("horizontal", horizontal, 0.1f, Time.deltaTime);

      // vertical speed is the magnitude of our desired velocity
      _animator.SetFloat("vertical", _navMeshAgent.desiredVelocity.magnitude, 0.1f, Time.deltaTime);

      // turn on spot
      _animator.SetInteger("turnOnSpot", turnOnSpot);

      // if (_navMeshAgent.isOnOffMeshLink & !_isProcessingOffMeshLink)
      // {
      //   _isProcessingOffMeshLink = true;
      //
      //   StartCoroutine(Jump(1f));
      //   return;
      // }

      if (!_navMeshAgent.hasPath && !_navMeshAgent.pathPending ||
          _navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
      {
        SetNextDestination(true);
      }
      else if (_navMeshAgent.isPathStale)
      {
        SetNextDestination(false);
      }
    }

    /// <summary>
    /// Coroutine
    /// </summary>
    /// <param name="duration">duration of the jump</param>
    /// <returns></returns>
    private IEnumerator Jump(float duration)
    {
      // get the data of the current off mesh link
      var data = _navMeshAgent.currentOffMeshLinkData;

      var startPos = _navMeshAgent.transform.position;
      var endPos = data.endPos + (_navMeshAgent.baseOffset * Vector3.up);

      var timeElapsed = 0f;
      while (timeElapsed < duration)
      {
        var t = timeElapsed / duration;

        // set the new position of the nav agent
        _navMeshAgent.transform.position = Vector3.Lerp(startPos, endPos, t) + (jumpCurve.Evaluate(t) * Vector3.up);

        timeElapsed += Time.deltaTime;

        yield return null;
      }

      _isProcessingOffMeshLink = false;

      // complete the custom off mesh link
      // so the remaining is calculated by the agent A* search algorithm
      _navMeshAgent.CompleteOffMeshLink();
    }

    /// <summary>
    /// sets the next destination
    /// bool parameter is used to avoid stale path to reset the destination of the agent
    /// to the current index
    /// </summary>
    /// <param name="increment"></param>
    private void SetNextDestination(bool increment)
    {
      if (!waypointNetwork) return;

      var incrementStep = increment ? 1 : 0;

      var nextWaypoint = CurrentIndex + incrementStep >= waypointNetwork.Waypoints.Count
        ? 0
        : CurrentIndex + incrementStep;

      var nextWaypointTransform = waypointNetwork.Waypoints[nextWaypoint];

      if (nextWaypointTransform != null)
      {
        CurrentIndex = nextWaypoint;
        _navMeshAgent.SetDestination(nextWaypointTransform.position);
        return;
      }

      // did not find a valid waypoint - increment the current index
      CurrentIndex++;
    }
  }
}