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

    [Tooltip("Only used for pre-baked in animations without fbx files, to assign a custom curve instead")]
    [SerializeField]
    private CustomCurve customCurve;

    // if any of the excluded layers are active ignore the request; otherwise, play the sound
    [Tooltip("List of layers that we want to override this Audio Collection Player")] [SerializeField]
    private StringList layerExclusions;

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
      if (layerIndex != 0 && animator.GetLayerWeight(layerIndex).Equals(0f)) return;
      if (_stateMachine == null) return;

      // play only if more important layers are disabled
      if (layerExclusions != null)
      {
        for (int i = 0; i < layerExclusions.Count; i++)
        {
          if (_stateMachine.IsLayerActive(layerExclusions[i])) return;
        }
      }

      // check for a custom curve which is used for pre-baked animations as a work around
      // if the animation has an .fbx file this code will return 0
      var customCommand = customCurve == null
        ? 0
        : Mathf.FloorToInt(customCurve.Evaluate(stateInfo.normalizedTime - (long)stateInfo.normalizedTime));

      // fetch the command for the animations with no custom curve
      // because they have an .fbx file that we can set the curve already
      var command = customCommand != 0 ? customCommand : Mathf.FloorToInt(animator.GetFloat(_commandChannelHash));

      if (previousCommand != command && command > 0 && _audioManager != null)
      {
        // sample an audio clip from our collection
        // first bank is bank 0 that's why subtract 1 from the command
        var bank = Mathf.Max(0, Mathf.Min(command - 1, collection.BankCount - 1));

        // play the sound
        _audioManager.PlayOneShotSound(collection.AudioGroup, collection[bank], _stateMachine.transform.position,
          collection.Volume, collection.SpatialBlend, collection.Priority);
      }

      previousCommand = command;
    }
  }
}