using System.Collections;
using Dead_Earth.Scripts.AI;
using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.TestScripts
{
  /// <summary>
  /// Root motion on the animator is enabled
  /// Root motion of the animation will pass the data to the nav mesh agent
  /// Updated - the rotation is handled by us not Root Motion
  /// 
  /// when mixed mode is enabled we switch back to root motion out of Locomotion stat when turning the
  /// agent in place
  /// </summary>
  [RequireComponent(typeof(NavMeshAgent))]
  public class NavAgentRootMotion : MonoBehaviour
  {
    [SerializeField] private AIWaypointNetwork waypointNetwork;

    // mixed mode switch
    public bool mixedMode = true;

    public int CurrentIndex = 0;
    public AnimationCurve jumpCurve;

    private NavMeshAgent _navMeshAgent;
    private Animator _animator;

    private bool _isProcessingOffMeshLink;
    private float _smoothAngle = 0f;

    private void Awake()
    {
      _navMeshAgent = GetComponent<NavMeshAgent>();
      _animator = GetComponent<Animator>();
    }

    private void Start()
    {
      // tell NavMeshAgent not to update rotation
      _navMeshAgent.updateRotation = false;

      SetNextDestination(false);
    }

    private void Update()
    {
      // if (_navMeshAgent.isOnOffMeshLink & !_isProcessingOffMeshLink)
      // {
      //   _isProcessingOffMeshLink = true;
      //
      //   StartCoroutine(Jump(1f));
      //   return;
      // }

      // calculate the angle
      var localDesiredVelocity = transform.InverseTransformVector(_navMeshAgent.desiredVelocity);
      var angle = Mathf.Atan2(localDesiredVelocity.x, localDesiredVelocity.z) * Mathf.Rad2Deg;

      // calculate the smoothing
      // we wont be able to move more than 80 degrees in 1 second
      _smoothAngle = Mathf.MoveTowardsAngle(_smoothAngle, angle, 80f * Time.deltaTime);

      var forwardSpeed = localDesiredVelocity.z;

      _animator.SetFloat("angle", _smoothAngle);
      _animator.SetFloat("speed", forwardSpeed, 0.1f, Time.deltaTime);

      if (localDesiredVelocity.sqrMagnitude > Mathf.Epsilon)
      {
        if (!mixedMode || mixedMode && Mathf.Abs(angle) < 80f &&
          _animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))
        {
          // update the rotation ourselves to look into the direction of the desiredVelocity
          var lookRotation = Quaternion.LookRotation(_navMeshAgent.desiredVelocity, Vector3.up);

          transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);
        }
      }

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
    /// Handles animator's root motion
    /// </summary>
    private void OnAnimatorMove()
    {
      if (mixedMode && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))
      {
        // rotation from the animator root motion
        transform.rotation = _animator.rootRotation;
      }

      // give the root motion velocity to navmesh agent
      _navMeshAgent.velocity = _animator.deltaPosition / Time.deltaTime;
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