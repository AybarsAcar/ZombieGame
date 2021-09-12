using Dead_Earth.Scripts.ScriptableObjects;
using UnityEngine;

namespace Dead_Earth.Scripts.AI.StateMachineBehaviours
{
  /// <summary>
  /// to issue play and stop requests to a specific layer
  /// </summary>
  public class AILayeredAudioSourcePlayer : AIStateMachineLink
  {
    [SerializeField] private AudioCollection collection;
    [SerializeField] private int bank;
    [SerializeField] private bool looping = true;
    [SerializeField] private bool stopOnExit = false;

    private float _previousLayerWeight = 0f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,
      int layerIndex)
    {
      if (_stateMachine == null) return;

      var layerWeight = animator.GetLayerWeight(layerIndex);

      if (layerIndex == 0 || layerWeight > 0.5f)
      {
        _stateMachine.PlayAudio(collection, bank, layerIndex, looping);
      }
      else
      {
        _stateMachine.StopAudio(layerIndex);
      }

      // store the layer weight to detect changes mid animation
      _previousLayerWeight = layerWeight;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo,
      int layerIndex)
    {
      if (_stateMachine != null && stopOnExit)
      {
        _stateMachine.StopAudio(layerIndex);
      }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo,
      int layerIndex)
    {
      if (_stateMachine == null) return;

      var layerWeight = animator.GetLayerWeight(layerIndex);

      if (layerWeight != _previousLayerWeight && collection != null)
      {
        if (layerWeight > 0.5f)
        {
          _stateMachine.PlayAudio(collection, bank, layerIndex, true);
        }
        else
        {
          _stateMachine.StopAudio(layerIndex);
        }
      }

      _previousLayerWeight = layerWeight;
    }
  }
}