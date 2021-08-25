using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// Base Class for the any Zombie AI State
  /// </summary>
  public abstract class AIZombieState : AIState
  {
    protected int _playerLayerMask = -1;
    protected int _visualLayerMask = -1;
    protected int _bodyPartLayer = -1;

    protected AIZombieStateMachine _zombieStateMachine;

    private void Awake()
    {
      // setup the layer mask
      _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Default");
      _visualLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Default", "Visual Aggravator");

      _bodyPartLayer = LayerMask.NameToLayer("AI Body Part");
    }

    public override void SetStateMachine(AIStateMachine machine)
    {
      if (_stateMachine.GetType() != typeof(AIZombieStateMachine)) return;

      base.SetStateMachine(machine);
      _zombieStateMachine = (AIZombieStateMachine)machine;
    }

    public override void OnTriggerEvent(AITriggerEventType eventType, Collider other)
    {
      if (_zombieStateMachine == null) return;

      if (eventType != AITriggerEventType.Exit)
      {
        // fetch the current visual threat
        var currentType = _zombieStateMachine.visualThreat.type;

        // is the collider entered the trigger Player
        // Player is the highest priority trigger
        if (other.CompareTag("Player"))
        {
          var distance = Vector3.Distance(_zombieStateMachine.sensorPosition, other.transform.position);

          if (currentType != AITargetType.VisualPlayer || (currentType == AITargetType.VisualPlayer &&
                                                           distance < _zombieStateMachine.visualThreat.distance))
          {
            if (IsColliderVisible(other, out var hitInfo, _playerLayerMask))
            {
              // set the Player as the visual target
              _zombieStateMachine.visualThreat.Set(AITargetType.VisualPlayer, other, other.transform.position,
                distance);
            }
          }
        }

        // Player's flashlight is the second highest priority
        else if (other.CompareTag("Flashlight") && currentType != AITargetType.VisualPlayer)
        {
          var flashlightCollider = (BoxCollider)other;

          var distanceToThreat =
            Vector3.Distance(_zombieStateMachine.sensorPosition, flashlightCollider.transform.position);

          var zSize = flashlightCollider.size.z * flashlightCollider.transform.lossyScale.z;

          var aggroFactor = distanceToThreat / zSize;

          // only trigger the zombie if the factor is less than its sight and intelligence
          if (aggroFactor <= _zombieStateMachine.Sight && aggroFactor <= _zombieStateMachine.Intelligence)
          {
            _zombieStateMachine.visualThreat.Set(AITargetType.VisualLight, other, other.transform.position,
              distanceToThreat);
          }
        }

        // handle the sound emitters
        else if (other.CompareTag("AI Sound Emitter"))
        {
          // sound triggers are always assumed to be sphere triggers
          var soundTrigger = (SphereCollider)other;
          if (soundTrigger == null) return;

          var agentSensorPosition = _zombieStateMachine.sensorPosition;

          ConvertSphereColliderToWorldSpace(soundTrigger, out var soundWorldPos, out var soundWorldRadius);

          // another way of saying Vector3.Distance()
          var distanceToThreat = (soundWorldPos - agentSensorPosition).magnitude;

          // calculate a distance factor such that it is 1 when at sound radius 0 when at center
          var distanceFactor = (distanceToThreat / soundWorldRadius);

          // bias the factor based on hearing ability of Agent
          distanceFactor += distanceFactor * (1f - _zombieStateMachine.Hearing);

          // the sound is too far away for the Agent
          if (distanceFactor > 1) return;

          // if we can hear it and it is closer then what we previously have stored
          if (distanceToThreat < _zombieStateMachine.audioThreat.distance)
          {
            // closest Audio threat so far
            _zombieStateMachine.audioThreat.Set(AITargetType.Audio, other, soundWorldPos, distanceToThreat);
          }
        }

        // food threat (trigger) which is the least important
        // feeds on a dead body - when dies we assign a trigger and Tag it as AI food
        else if (other.CompareTag("AI Food") && currentType != AITargetType.VisualPlayer &&
                 currentType != AITargetType.VisualLight && _zombieStateMachine.Satisfaction <= 0.9f &&
                 _zombieStateMachine.audioThreat.type == AITargetType.None)
        {
          var distanceToThreat = Vector3.Distance(other.transform.position, _zombieStateMachine.sensorPosition);

          if (distanceToThreat < _zombieStateMachine.visualThreat.distance)
          {
            // go to the closer food
            if (IsColliderVisible(other, out var hitInfo, _visualLayerMask))
            {
              _zombieStateMachine.audioThreat.Set(AITargetType.VisualFood, other, other.transform.position,
                distanceToThreat);
            }
          }
        }
      }
    }


    /// <summary>
    /// Checks if the collider is within the given AI view sight
    /// </summary>
    /// <param name="other"></param>
    /// <param name="hitInfo"></param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    protected virtual bool IsColliderVisible(Collider other, out RaycastHit hitInfo, int layerMask)
    {
      hitInfo = new RaycastHit();

      if (_zombieStateMachine == null) return false;

      var head = _zombieStateMachine.sensorPosition;
      var direction = other.transform.position - head;

      var angle = Vector3.Angle(direction, transform.forward);

      // if the angle is larger than the half of the point of view, the player if out of sight
      // because the transform.forward splits the field of view from the middle
      if (angle > _zombieStateMachine.FieldOfView * 0.5f) return false;

      var hits = Physics.RaycastAll(head, direction.normalized,
        _zombieStateMachine.sensorRadius * _zombieStateMachine.Sight,
        layerMask);

      var closestColliderDistance = float.MaxValue;
      Collider closestCollider = null;
      foreach (var hit in hits)
      {
        if (hit.distance < closestColliderDistance)
        {
          if (hit.transform.gameObject.layer == _bodyPartLayer)
          {
            if (_stateMachine != GameSceneManager.Instance.GetAIStateMachine(hit.rigidbody.GetInstanceID()))
            {
              closestColliderDistance = hit.distance;
              closestCollider = hit.collider;
              hitInfo = hit;
            }
          }
          else
          {
            closestColliderDistance = hit.distance;
            closestCollider = hit.collider;
            hitInfo = hit;
          }
        }
      }

      if (closestCollider != null && closestCollider.gameObject == other.gameObject)
      {
        return true;
      }

      return false;
    }
  }
}