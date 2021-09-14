using Dead_Earth.Scripts.ScriptableObjects;
using UnityEngine;

namespace Dead_Earth.Scripts.Audio.StateMachineBehaviours
{
  /// <summary>
  /// plays as enter to the state
  /// this will be used to play a one shot sound as we enter an animations state from the animator
  /// </summary>
  public class AudioOnEnter : StateMachineBehaviour
  {
    [SerializeField] private AudioCollection audioCollection;
    [SerializeField] private int bank;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,
      int layerIndex)
    {
      if (AudioManager.Instance == null || audioCollection == null) return;

      AudioManager.Instance.PlayOneShotSound(audioCollection.AudioGroup, audioCollection[bank],
        animator.transform.position, audioCollection.Volume, audioCollection.SpatialBlend, audioCollection.Priority);
    }
  }
}