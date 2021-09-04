using UnityEngine;

namespace Dead_Earth.Scripts.AI.StateMachineBehaviours
{
  /// <summary>
  /// Attached onto the Empty State on the Cinematic Layer in the Animator
  /// </summary>
  public class CinematicEnabler : AIStateMachineLink
  {
    public bool onEnter;
    public bool onExit;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      base.OnStateEnter(animator, stateInfo, layerIndex);

      if (_stateMachine != null)
      {
        _stateMachine.CinematicEnabled = onEnter;
      }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      base.OnStateExit(animator, stateInfo, layerIndex);
      
      if (_stateMachine != null)
      {
        _stateMachine.CinematicEnabled = onExit;
      }
    }
  }
}