using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dead_Earth.Scripts
{
  [Serializable]
  public class GameState
  {
    public string key = null;
    public string value = null;
  }

  /// <summary>
  /// Singleton class with don't destroy on load
  /// </summary>
  public class ApplicationManager : MonoBehaviour
  {
    private static ApplicationManager _instance = null;

    public static ApplicationManager Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = FindObjectOfType<ApplicationManager>();
        }

        return _instance;
      }
    }

    [SerializeField] private List<GameState> startingGameStates = new List<GameState>();

    // to be more efficiently set and fetched at runtime
    private Dictionary<string, string> _gameStateDictionary = new Dictionary<string, string>();

    private void Awake()
    {
      DontDestroyOnLoad(gameObject);

      foreach (var gameState in startingGameStates)
      {
        _gameStateDictionary.Add(gameState.key, gameState.value);
      }
    }

    /// <summary>
    /// returns the value of a game state
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string GetGameState(string key)
    {
      return _gameStateDictionary.ContainsKey(key) ? _gameStateDictionary[key] : null;
    }

    /// <summary>
    /// Sets the key value pair if not null
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool SetGameState(string key, string value)
    {
      if (key == null || value == null) return false;

      _gameStateDictionary.Add(key, value);
      return true;
    }
  }
}