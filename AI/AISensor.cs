using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  public class AISensor : MonoBehaviour
  {
    private AIStateMachine _parentStateMachine;

    public AIStateMachine ParentStateMachine
    {
      set => _parentStateMachine = value;
    }

    private void OnTriggerEnter(Collider other)
    {
      if (_parentStateMachine != null)
      {
        _parentStateMachine.OnTriggerEvent(AITriggerEventType.Enter, other);
      }
    }

    private void OnTriggerStay(Collider other)
    {
      if (_parentStateMachine != null)
      {
        _parentStateMachine.OnTriggerEvent(AITriggerEventType.Stay, other);
      }
    }

    private void OnTriggerExit(Collider other)
    {
      if (_parentStateMachine != null)
      {
        _parentStateMachine.OnTriggerEvent(AITriggerEventType.Exit, other);
      }
    }
  }
}