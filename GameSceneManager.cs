using System.Collections.Generic;
using Dead_Earth.Scripts.AI;
using Dead_Earth.Scripts.FPS;
using UnityEngine;

namespace Dead_Earth.Scripts
{
  /// <summary>
  /// manages the scene - Singleton
  /// stores every collider of the Zombie in a dictionary
  /// </summary>
  public class GameSceneManager : MonoBehaviour
  {
    // blood particle system in the scene
    [SerializeField] private ParticleSystem bloodParticleSystem;
    public ParticleSystem BloodParticleSystem => bloodParticleSystem;

    // make it singleton
    private static GameSceneManager _instance;

    public static GameSceneManager Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = FindObjectOfType<GameSceneManager>();
        }

        return _instance;
      }
    }

    private void Awake()
    {
      DontDestroyOnLoad(gameObject);
    }

    // private caches
    // int - Unity Game Object Unique Id which is integer type
    private Dictionary<int, AIStateMachine> _stateMachines = new Dictionary<int, AIStateMachine>();

    private Dictionary<int, PlayerInfo> _playerInfos = new Dictionary<int, PlayerInfo>();


    /// <summary>
    /// caches the passed in state machine to the class dictionary
    /// </summary>
    /// <param name="id"></param>
    /// <param name="stateMachine"></param>
    public void RegisterAIStateMachine(int id, AIStateMachine stateMachine)
    {
      if (!_stateMachines.ContainsKey(id))
      {
        _stateMachines.Add(id, stateMachine);
      }
    }

    /// <summary>
    /// returns the AIState machine by its id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>AIStateMachine with the id or null if id doesn't exist</returns>
    public AIStateMachine GetAIStateMachine(int id)
    {
      return _stateMachines.ContainsKey(id) ? _stateMachines[id] : null;
    }


    /// <summary>
    /// caches the passed in player info to the class dictionary
    /// </summary>
    /// <param name="id">Player's collider id</param>
    /// <param name="playerInfo"></param>
    public void RegisterPlayerInfo(int id, PlayerInfo playerInfo)
    {
      if (!_stateMachines.ContainsKey(id))
      {
        _playerInfos.Add(id, playerInfo);
      }
    }

    /// <summary>
    /// returns the PlayerInfo by its id
    /// </summary>
    /// <param name="id">Player's collider id</param>
    /// <returns>PlayerInfo with the id or null if id doesn't exist</returns>
    public PlayerInfo GetPlayerInfo(int id)
    {
      return _playerInfos.ContainsKey(id) ? _playerInfos[id] : null;
    }
  }
}