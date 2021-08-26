using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// Base Class for all Individual AI States used by the AI System (AIStateMachine)
  /// </summary>
  public abstract class AIState : MonoBehaviour
  {
    public abstract AIStateType GetStateType();

    /// <summary>
    /// 
    /// </summary>
    public abstract AIStateType OnUpdate();

    protected AIStateMachine _stateMachine;

    public virtual void SetStateMachine(AIStateMachine machine)
    {
      _stateMachine = machine;
    }

    // Default Handler Functions

    /// <summary>
    /// whenever we enter a state each time this method is called
    /// so class variables can be set
    /// </summary>
    public virtual void OnEnterState()
    {
    }

    /// <summary>
    /// called when the state is exited
    /// used for clean up
    /// </summary>
    public virtual void OnExitState()
    {
    }

    /// <summary>
    /// is like OnAnimatorMove
    /// we will use to override the root motion
    /// </summary>
    public virtual void OnAnimatorUpdated()
    {
      // contact the parent state machine and fetch whether using the Root Motion
      if (_stateMachine.useRootMotionPosition)
      {
        _stateMachine.navMeshAgent.velocity = _stateMachine.animator.deltaPosition / Time.deltaTime;
      }

      if (_stateMachine.useRootMotionRotation)
      {
        _stateMachine.transform.rotation = _stateMachine.animator.rootRotation;
      }
    }

    /// <summary>
    /// will be used to direct OnAnimatorIK Unity Callback
    /// </summary>
    public virtual void OnAnimatorIKUpdated()
    {
    }

    /// <summary>
    /// called by the state machine
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="other">collider that triggers AI's sensor</param>
    public virtual void OnTriggerEvent(AITriggerEventType eventType, Collider other)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isReached"></param>
    public virtual void OnDestinationReached(bool isReached)
    {
    }

    /// <summary>
    /// calculates the world space position and the world space radius of the sphere of the collider
    /// taking into account hierarchical scaling
    /// </summary>
    /// <param name="collider">collider we want to get the world position and radius</param>
    /// <param name="position">World position</param>
    /// <param name="radius">World radius</param>
    public static void ConvertSphereColliderToWorldSpace(SphereCollider collider, out Vector3 position,
      out float radius)
    {
      position = Vector3.zero;
      radius = 0f;

      if (collider == null) return;

      position = collider.transform.position;

      position.x += collider.center.x * collider.transform.lossyScale.x;
      position.y += collider.center.y * collider.transform.lossyScale.y;
      position.z += collider.center.z * collider.transform.lossyScale.z;

      // make sure to return the largest radius
      radius = Mathf.Max(collider.radius * collider.transform.lossyScale.x,
        collider.radius * collider.transform.lossyScale.y);

      radius = Mathf.Max(radius, collider.radius * collider.transform.lossyScale.z);
    }

    /// <summary>
    /// returns the angle between 2 vectors passed in
    /// with a sign so the AI can determine which way to return
    /// </summary>
    /// <param name="fromVector">starting vector</param>
    /// <param name="toVector">target vector</param>
    /// <returns></returns>
    public static float FindSignedAngle(Vector3 fromVector, Vector3 toVector)
    {
      if (fromVector == toVector)
      {
        return 0f;
      }

      var angle = Vector3.Angle(fromVector, toVector);

      var cross = Vector3.Cross(fromVector, toVector);

      angle *= Mathf.Sign(cross.y);

      return angle;
    }
  }
}