using System;
using System.Collections;
using System.Collections.Generic;
using Dead_Earth.Scripts.Audio;
using Dead_Earth.Scripts.FPS;
using Dead_Earth.Scripts.ScriptableObjects;
using Dead_Earth.Scripts.Utilities;
using UnityEngine;

namespace Dead_Earth.Scripts.InteractiveItems
{
  /// <summary>
  /// defines the animator parameter types in our animators
  /// </summary>
  public enum AnimatorParameterType
  {
    Trigger,
    Bool,
    Int,
    Float,
    String
  }

  [Serializable]
  public class AnimatorParameter
  {
    public AnimatorParameterType type = AnimatorParameterType.Bool;
    public string name;
    public string value;
  }

  [Serializable]
  public class AnimatorConfigurator
  {
    public Animator animator;
    public List<AnimatorParameter> animatorParameters;
  }

  public class InteractiveGenericSwitch : InteractiveItem
  {
    [Header("Game State Management")] [SerializeField]
    private List<GameState> requiredStates = new List<GameState>();

    [SerializeField] private List<GameState> activateStates = new List<GameState>();
    [SerializeField] private List<GameState> deactivateStates = new List<GameState>();

    [Header("Message")] [SerializeField] [TextArea(3, 10)]
    private string stateNotSetText;

    [SerializeField] [TextArea(3, 10)] private string stateSetText;
    [SerializeField] [TextArea(3, 10)] private string objectActiveText;

    [Header("Activation Parameters")] [SerializeField]
    private float activationDelay = 1f;

    [SerializeField] private float deactivationDelay = 1f;

    // activation sounds are in bank 0; deactivation sounds are in bank 1
    [SerializeField] private AudioCollection activationSounds;
    [SerializeField] private AudioSource audioSource; // audio source we want to play it one

    [Header("Operating Mode")] [SerializeField]
    private bool startActivated;

    [SerializeField] private bool canToggle;


    [Header("Configurable Entities")] [SerializeField]
    private List<AnimatorConfigurator> animatorConfigurators = new List<AnimatorConfigurator>();

    [SerializeField] private List<MaterialController> materialControllers = new List<MaterialController>();
    [SerializeField] private List<GameObject> objectActivators = new List<GameObject>();
    [SerializeField] private List<GameObject> objectDeactivators = new List<GameObject>();

    private IEnumerator _coroutine = null;
    private bool _activated = false;
    private bool _firstUse = false;


    protected override void Start()
    {
      // make sure to call the base class to register
      base.Start();

      // activate material controller
      foreach (var controller in materialControllers)
      {
        controller.OnStart();
      }

      // turn off all objects that should be activated
      foreach (var objectActivator in objectActivators)
      {
        // turn off the game object
        objectActivator.SetActive(false);
      }

      foreach (var objectDeactivator in objectDeactivators)
      {
        // turn on the game object
        objectDeactivator.SetActive(true);
      }

      if (startActivated)
      {
        // we don't have a character manager
        Activate(null);
        _firstUse = false;
      }
    }

    public override string GetText()
    {
      if (!enabled) return string.Empty;

      if (_activated)
      {
        return objectActiveText;
      }

      var requiredStates = AreRequiredStatesSet();

      return !requiredStates ? stateNotSetText : stateSetText;
    }

    public override void Activate(CharacterManager characterManager)
    {
      var appManager = ApplicationManager.Instance;
      if (appManager == null) return;

      // if we are already in a different state to the starting state and we are not in toggle mode then
      // this item has been switched on / off and can not longer be altered
      if (_firstUse && !canToggle) return;

      if (!_activated)
      {
        var requiredStatesSet = AreRequiredStatesSet();
        if (!requiredStatesSet)
        {
          return;
        }
      }

      // object state has been switched
      _activated = !_activated;
      _firstUse = true;

      // play the activation sound effect
      // activation sound will be played immediately
      if (activationSounds != null && _activated)
      {
        var clip = activationSounds[0];
        if (clip == null)
        {
          return;
        }

        // set the audio source properties
        audioSource.clip = clip;
        audioSource.volume = activationSounds.Volume;
        audioSource.spatialBlend = activationSounds.SpatialBlend;
        audioSource.priority = activationSounds.Priority;
        audioSource.outputAudioMixerGroup =
          AudioManager.Instance.GetAudioGroupFromTrackName(activationSounds.AudioGroup);

        // play the sound set
        audioSource.Play();
      }

      // deactivation sound will be played after the delay
      if (_coroutine != null)
      {
        StopCoroutine(_coroutine);
      }

      _coroutine = DoDelayedActivation();
      StartCoroutine(_coroutine);
    }

    private IEnumerator DoDelayedActivation()
    {
      foreach (var configurator in animatorConfigurators)
      {
        foreach (var parameter in configurator.animatorParameters)
        {
          // TODO: support other types
          switch (parameter.type)
          {
            case AnimatorParameterType.Bool:
              var b = bool.Parse(parameter.value);
              configurator.animator.SetBool(parameter.name, _activated ? b : !b);
              break;
          }
        }
      }

      yield return new WaitForSeconds(_activated ? activationDelay : deactivationDelay);

      // set the states that should be set when activating / deactivating
      SetActivationStates();

      // play the deactivation sound
      if (activationSounds != null && !_activated)
      {
        var clip = activationSounds[1];
        if (clip != null && audioSource != null)
        {
          // set the audio source properties
          audioSource.clip = clip;
          audioSource.volume = activationSounds.Volume;
          audioSource.spatialBlend = activationSounds.SpatialBlend;
          audioSource.priority = activationSounds.Priority;
          audioSource.outputAudioMixerGroup =
            AudioManager.Instance.GetAudioGroupFromTrackName(activationSounds.AudioGroup);

          // play the sound set
          audioSource.Play();
        }
      }

      // if we get here then we are allowed to enable this object so first turn on any
      // game objects that should be made active by this
      if (objectActivators.Count > 0)
      {
        foreach (var objectActivator in objectActivators)
        {
          objectActivator.SetActive(_activated);
        }
      }

      // turn off any game objects that should be disabled by this action
      if (objectDeactivators.Count > 0)
      {
        foreach (var objectDeactivator in objectDeactivators)
        {
          objectDeactivator.SetActive(!_activated);
        }
      }

      // activate material controller
      foreach (var materialController in materialControllers)
      {
        materialController.Activate(_activated);
      }
    }

    private bool AreRequiredStatesSet()
    {
      var appManager = ApplicationManager.Instance;
      if (appManager == null) return false;

      foreach (var requiredState in requiredStates)
      {
        var result = appManager.GetGameState(requiredState.key);

        if (string.IsNullOrEmpty(result) || !result.Equals(requiredState.value))
        {
          return false;
        }
      }

      // all of the states match
      return true;
    }

    private void SetActivationStates()
    {
      var appManager = ApplicationManager.Instance;
      if (appManager == null) return;

      if (_activated)
      {
        foreach (var activateState in activateStates)
        {
          appManager.SetGameState(activateState.key, activateState.value);
        }
      }
      else
      {
        foreach (var deactivateState in deactivateStates)
        {
          appManager.SetGameState(deactivateState.key, deactivateState.value);
        }
      }
    }
  }
}