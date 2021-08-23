using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  public abstract class AIState : MonoBehaviour
  {
    public abstract AIStateType GetStateType();

    /// <summary>
    /// 
    /// </summary>
    public abstract AIStateType OnUpdate();

    protected AIStateMachine _stateMachine;

    public void SetStateMachine(AIStateMachine machine)
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
  }
}