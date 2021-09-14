using System.Collections;
using Dead_Earth.Scripts.Audio;
using Dead_Earth.Scripts.FPS;
using Dead_Earth.Scripts.ScriptableObjects;
using UnityEngine;

namespace Dead_Earth.Scripts.InteractiveItems
{
  /// <summary>
  /// Keypads behaviour which is an interactive item
  /// </summary>
  public class InteractiveKeypad : InteractiveItem
  {
    // the elevator that the keypad will activate
    [SerializeField] private Transform elevator;
    [SerializeField] private AudioCollection collection;
    [SerializeField] private int bank;
    [SerializeField] private float activationDelay = 2f;

    private bool _isActivated;

    private int _activateAnimatorHash = Animator.StringToHash("activate");

    public override string GetText()
    {
      var appDatabase = ApplicationManager.Instance;
      if (!appDatabase) return string.Empty;

      var powerState = appDatabase.GetGameState("POWER");
      var lockdownState = appDatabase.GetGameState("LOCKDOWN");
      var accessCodeState = appDatabase.GetGameState("ACCESSCODE");

      if (string.IsNullOrEmpty(powerState) || !powerState.Equals("TRUE"))
      {
        return "Keypad: No Power";
      }

      if (string.IsNullOrEmpty(lockdownState) || !lockdownState.Equals("FALSE"))
      {
        return "Keypad: Under Lockdown";
      }

      if (string.IsNullOrEmpty(accessCodeState) || !accessCodeState.Equals("TRUE"))
      {
        return "Keypad: Access Code Required";
      }

      return "Keypad";
    }

    public override void Activate(CharacterManager characterManager)
    {
      if (_isActivated)
      {
        // elevated activated before
        return;
      }

      var appDatabase = ApplicationManager.Instance;
      if (!appDatabase) return;

      var powerState = appDatabase.GetGameState("POWER");
      var lockdownState = appDatabase.GetGameState("LOCKDOWN");
      var accessCodeState = appDatabase.GetGameState("ACCESSCODE");

      if (string.IsNullOrEmpty(powerState) || !powerState.Equals("TRUE"))
      {
        return;
      }

      if (string.IsNullOrEmpty(lockdownState) || !lockdownState.Equals("FALSE"))
      {
        return;
      }

      if (string.IsNullOrEmpty(accessCodeState) || !accessCodeState.Equals("TRUE"))
      {
        return;
      }

      // activate the elevator
      StartCoroutine(DoDelayedActivation(characterManager));

      _isActivated = true;
    }

    private IEnumerator DoDelayedActivation(CharacterManager characterManager)
    {
      var clip = collection[bank];

      if (clip != null)
      {
        // play the sound
        if (AudioManager.Instance != null)
        {
          AudioManager.Instance.PlayOneShotSound(collection.AudioGroup, clip, elevator.position, collection.Volume,
            collection.SpatialBlend, collection.Priority);
        }
      }

      // wait for the delay before activating the animation
      yield return new WaitForSeconds(activationDelay);

      if (characterManager != null)
      {
        // make it a child of the elevator to avoid stuttering jerky animation
        characterManager.transform.parent = elevator;

        var animator = elevator.GetComponent<Animator>();

        // to avoid shake and stuff
        animator.updateMode = AnimatorUpdateMode.AnimatePhysics;

        animator.SetTrigger(_activateAnimatorHash);

        if (characterManager.FpsController)
        {
          characterManager.FpsController.FreezeMovement = true;
        }
      }
    }
  }
}