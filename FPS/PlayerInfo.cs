using UnityEngine;

namespace Dead_Earth.Scripts.FPS
{
  /// <summary>
  /// player information class
  /// to be stored in the GameSceneManager
  /// </summary>
  public class PlayerInfo
  {
    public Collider collider;
    public CharacterManager characterManager;
    public Camera camera;
    public CapsuleCollider meleeTrigger;
  }
}