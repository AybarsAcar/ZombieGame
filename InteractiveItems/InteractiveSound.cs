using System.Collections;
using Dead_Earth.Scripts.Audio;
using Dead_Earth.Scripts.FPS;
using Dead_Earth.Scripts.ScriptableObjects;
using UnityEngine;

namespace Dead_Earth.Scripts.InteractiveItems
{
  public class InteractiveSound : InteractiveItem
  {
    [TextArea(3, 10)] [SerializeField] private string infoText;

    [Tooltip("This text is displayed after the item is activated")] [TextArea(3, 10)] [SerializeField]
    private string activatedText;

    [Tooltip("Duration that the activated text is displayed")] [SerializeField]
    private float activatedTextDuration = 3f;

    [SerializeField] private AudioCollection audioCollection;
    [SerializeField] private int bank;

    private IEnumerator _coroutine;
    private float _hideActivatedTextTime = 0f;

    public override string GetText()
    {
      if (_coroutine != null || Time.time < _hideActivatedTextTime)
      {
        return activatedText;
      }

      return infoText;
    }

    public override void Activate(CharacterManager characterManager)
    {
      if (_coroutine == null)
      {
        _hideActivatedTextTime = Time.time + activatedTextDuration;

        _coroutine = DoActivation();
        StartCoroutine(_coroutine);
      }
    }

    private IEnumerator DoActivation()
    {
      if (audioCollection == null || AudioManager.Instance == null) yield break;

      var clip = audioCollection[bank];

      if (clip == null) yield break;

      AudioManager.Instance.PlayOneShotSound(audioCollection.AudioGroup, clip, transform.position,
        audioCollection.Volume, audioCollection.SpatialBlend, audioCollection.Priority);

      yield return new WaitForSeconds(clip.length);

      _coroutine = null;
    }
  }
}