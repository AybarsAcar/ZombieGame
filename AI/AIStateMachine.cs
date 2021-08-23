using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// States that AI can take
  /// </summary>
  public enum AIStateType
  {
    None,
    Idle,
    Alert,
    Attack,
    Feeding,
    Pursuit,
    Dead
  }

  /// <summary>
  /// Aggro enter the Zombie's sensor they are notified
  /// contains the threats and aggro that triggers the AIs
  /// </summary>
  public enum AITargetType
  {
    None,
    Waypoint,
    VisualPlayer,
    VisualLight,
    VisualFood,
    Audio
  };

  public enum AITriggerEventType
  {
    Enter,
    Stay,
    Exit
  }

  /// <summary>
  /// Describes a potential target to the AI System
  /// </summary>
  public struct AITarget
  {
    private AITargetType _type; // the type of target
    private Collider _collider; // the collider
    private Vector3 _position; // current position in the world
    private float _distance; // Distance from player
    private float _time; // time the target was last pinged

    public AITargetType type => _type;
    public Collider collider => _collider;
    public Vector3 position => _position;

    public float distance
    {
      get => _distance;
      set => _distance = value;
    }

    public float time => _time;

    public void Set(AITargetType t, Collider c, Vector3 p, float d)
    {
      _type = t;
      _collider = c;
      _position = p;
      _distance = d;
      _time = Time.time;
    }

    public void Clear()
    {
      _type = AITargetType.None;
      _collider = null;
      _position = Vector3.zero;
      _distance = Mathf.Infinity;
      _time = 0f;
    }
  }

  /// <summary>
  /// base AI State Machine class
  /// </summary>
  public abstract class AIStateMachine : MonoBehaviour
  {
    public AITarget visualThreat = new AITarget();
    public AITarget audioThreat = new AITarget();

    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
    protected AITarget _target = new AITarget();
    protected AIState _currentState;


    // idle is its default state
    [SerializeField] protected AIStateType currentStateType = AIStateType.Idle;
    [SerializeField] protected SphereCollider targetTrigger;
    [SerializeField] protected SphereCollider sensorTrigger;
    [SerializeField] [Range(0, 15)] protected float stoppingDistance = 1f;

    // Component Cache
    protected Animator _animator;
    protected NavMeshAgent _navMeshAgent;
    protected Collider _collider;
    protected Transform _transform;

    // Component Cache Accessors
    public Animator animator => _animator;
    public NavMeshAgent navMeshAgent => _navMeshAgent;

    protected void Awake()
    {
      // cache components
      _transform = transform;
      _animator = GetComponent<Animator>();
      _navMeshAgent = GetComponent<NavMeshAgent>();
      _collider = GetComponent<Collider>();
    }

    protected virtual void Start()
    {
      var states = GetComponents<AIState>();

      foreach (var state in states)
      {
        if (state != null && !_states.ContainsKey(state.GetStateType()))
        {
          // set the local dictionary if not already in the dictionary
          _states[state.GetStateType()] = state;

          // send this as a parameter
          state.SetStateMachine(this);
        }
      }

      // get the initial state from the dictionary to use as the current state
      if (_states.ContainsKey(currentStateType))
      {
        _currentState = _states[currentStateType];
        _currentState.OnEnterState();
      }
      else
      {
        // fallback case of error
        _currentState = null;
      }
    }

    /// <summary>
    /// Gives the current state a chance to update itself and perform transitions
    /// </summary>
    protected virtual void Update()
    {
      if (!_currentState) return;

      var newStateType = _currentState.OnUpdate();

      if (newStateType != currentStateType)
      {
        // trigger state change
        if (_states.TryGetValue(newStateType, out var newState))
        {
          // change the state
          _currentState.OnExitState();
          newState.OnEnterState();
          _currentState = newState;
        }
        else if (_states.TryGetValue(AIStateType.Idle, out var fallbackState))
        {
          // fallback - set to Idle State
          _currentState.OnExitState();
          fallbackState.OnEnterState();
          _currentState = fallbackState;
        }

        // set the new state type
        currentStateType = newStateType;
      }
    }

    /// <summary>
    /// called at each tick of the Physics system
    /// 
    /// Keeps track of the current target and the distance from it
    /// makes sure audio and visual threats are cleared at each physics tick
    /// </summary>
    protected virtual void FixedUpdate()
    {
      visualThreat.Clear();
      audioThreat.Clear();

      if (_target.type != AITargetType.None)
      {
        _target.distance = Vector3.Distance(_transform.position, _target.position);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t">ai target</param>
    /// <param name="c">collider</param>
    /// <param name="p">position</param>
    /// <param name="d">distance to target</param>
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
    {
      _target.Set(t, c, p, d);

      if (targetTrigger != null)
      {
        targetTrigger.radius = stoppingDistance;
        targetTrigger.transform.position = _target.position;
        targetTrigger.enabled = true;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="aiTarget"></param>
    public void SetTarget(AITarget aiTarget)
    {
      _target = aiTarget;

      if (targetTrigger != null)
      {
        targetTrigger.radius = stoppingDistance;
        targetTrigger.transform.position = _target.position;
        targetTrigger.enabled = true;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t"></param>
    /// <param name="c"></param>
    /// <param name="p"></param>
    /// <param name="d"></param>
    /// <param name="s">Stopping Distance</param>
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
    {
      _target.Set(t, c, p, d);

      if (targetTrigger != null)
      {
        targetTrigger.radius = s;
        targetTrigger.transform.position = _target.position;
        targetTrigger.enabled = true;
      }
    }

    /// <summary>
    /// Clears the Current Target
    /// </summary>
    public void ClearTarget()
    {
      _target.Clear();

      if (targetTrigger != null)
      {
        targetTrigger.enabled = false;
      }
    }
  }
}