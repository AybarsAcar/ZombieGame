using System;
using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  public class MeleeZoneTrigger : MonoBehaviour
  {
    /// <summary>
    /// Set the IsInMeleeRange property of that specific AI State Machine to true
    /// when an AI enters the trigger
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
      var aiStateMachine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());

      if (aiStateMachine)
      {
        aiStateMachine.IsInMeleeRange = true;
      }
    }

    /// <summary>
    /// Set the IsInMeleeRange property of that specific AI State Machine to false
    /// when an AI exits the trigger
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
      var aiStateMachine = GameSceneManager.Instance.GetAIStateMachine(other.GetInstanceID());

      if (aiStateMachine)
      {
        aiStateMachine.IsInMeleeRange = false;
      }
    }
  }
}