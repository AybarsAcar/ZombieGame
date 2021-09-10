using Dead_Earth.Scripts.Audio;
using Dead_Earth.Scripts.ScriptableObjects;
using UnityEngine;

namespace Dead_Earth.Scripts.AI.StateMachineBehaviours
{
  /// <summary>
  /// used to listen to audio and play from the animator using animation curves
  /// </summary>
  public class AudioCollectionPlayer : AIStateMachineLink
  {
    [SerializeField] private ComChannelName commandChannel = ComChannelName.ComChannel1;
    [SerializeField] private AudioCollection collection;

    // we cast the command to an integer from enum
    private int previousCommand = 0;
    private AudioManager _audioManager;
    private int _commandChannelHash = 0;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      _audioManager = AudioManager.Instance;
      previousCommand = 0;

      if (_commandChannelHash == 0)
      {
        // cache the parameter hash value
        _commandChannelHash = Animator.StringToHash(commandChannel.ToString());
      }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      base.OnStateUpdate(animator, stateInfo, layerIndex);

      if (layerIndex != 0 && animator.GetLayerWeight(layerIndex).Equals(0f)) return;
      if (_stateMachine == null) return;

      var command = Mathf.FloorToInt(animator.GetFloat(_commandChannelHash));

      if (previousCommand != command && command > 0 && _audioManager != null)
      {
        // sample an audio clip from our collection
        // first bank is bank 0 that's why subtract 1 from the command
        var bank = Mathf.Max(0, Mathf.Min(command - 1, collection.BankCount - 1));

        // play the sound
        _audioManager.PlayOneShotSound(collection.AudioGroup, collection[bank], _stateMachine.transform.position,
          collection.SpatialBlend, collection.Priority);
      }

      previousCommand = command;
    }
  }
}