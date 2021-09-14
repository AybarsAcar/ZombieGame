using System;
using UnityEngine;

namespace Dead_Earth.Scripts
{
  /// <summary>
  /// ends the mission as the player enters the trigger
  /// </summary>
  public class MissionObjective : MonoBehaviour
  {
    private void OnTriggerEnter(Collider other)
    {
      if (other.gameObject.CompareTag("Player"))
      {
        if (GameSceneManager.Instance)
        {
          var playerInfo = GameSceneManager.Instance.GetPlayerInfo(other.GetInstanceID());

          if (playerInfo != null)
          {
            // execute the level completion sequence
            playerInfo.characterManager.CompleteLevel();
          }
        }
      }
    }
  }
}