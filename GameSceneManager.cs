using System;
using System.Collections.Generic;
using Dead_Earth.Scripts.AI;
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


    /// <summary>
    /// caches the passed in state machine to the class dictionary
    /// </summary>
    /// <param name="key"></param>
    /// <param name="stateMachine"></param>
    public void RegisterAIStateMachine(int key, AIStateMachine stateMachine)
    {
      if (!_stateMachines.ContainsKey(key))
      {
        _stateMachines.Add(key, stateMachine);
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
  }
}