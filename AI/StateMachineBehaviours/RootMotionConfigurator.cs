using UnityEngine;

namespace Dead_Earth.Scripts.AI.StateMachineBehaviours
{
  /// <summary>
  /// Configures the Root Motion Behaviour
  /// </summary>
  public class RootMotionConfigurator : AIStateMachineLink
  {
    [SerializeField] private int rootPosition = 0;
    [SerializeField] private int rootRotation = 0;

    private bool _rootMotionProcessed;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,
      int layerIndex)
    {
      if (_stateMachine != null)
      {
        _stateMachine.AddRootMotionRequest(rootPosition, rootRotation);
        _rootMotionProcessed = true;
      }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo,
      int layerIndex)
    {
      if (_stateMachine != null && _rootMotionProcessed)
      {
        _stateMachine.AddRootMotionRequest(-rootPosition, -rootRotation);
        _rootMotionProcessed = false;
      }
    }
  }
}