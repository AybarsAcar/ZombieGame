using System.Collections;
using System.Linq;
using Dead_Earth.Scripts.AI;
using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.TestScripts
{
  [RequireComponent(typeof(NavMeshAgent))]
  public class NavAgentExample : MonoBehaviour
  {
    [SerializeField] private AIWaypointNetwork waypointNetwork;

    public int CurrentIndex = 0;
    public AnimationCurve jumpCurve;

    private NavMeshAgent _navMeshAgent;

    private bool _isProcessingOffMeshLink;

    private void Awake()
    {
      _navMeshAgent = GetComponent<NavMeshAgent>();

      SetNextDestination(false);
    }

    private void Update()
    {
      if (_navMeshAgent.isOnOffMeshLink & !_isProcessingOffMeshLink)
      {
        _isProcessingOffMeshLink = true;

        StartCoroutine(Jump(1f));
        return;
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

      var nextWaypoint = CurrentIndex + incrementStep >= waypointNetwork.Waypoints.Count()
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