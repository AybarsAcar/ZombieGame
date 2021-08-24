using UnityEngine;

namespace Dead_Earth.Scripts.AI.StateMachineBehaviours
{
  /// <summary>
  /// Base Class for our AI State Machines for the Animation State Machine
  /// </summary>
  public class AIStateMachineLink : StateMachineBehaviour
  {
    protected AIStateMachine _stateMachine;

    public AIStateMachine StateMachine
    {
      set => _stateMachine = value;
    }
  }
}