using System;
using Dead_Earth.Scripts.FPS;
using UnityEngine;

namespace Dead_Earth.Scripts.InteractiveItems
{
  public class InteractiveItem : MonoBehaviour
  {
    // we will return all the interactive items with our raycast
    // and rank them according to their priority
    [SerializeField] protected int priority = 0;
    public int Priority => priority;

    protected GameSceneManager _gameSceneManager;
    protected Collider _collider;

    protected virtual void Start()
    {
      _gameSceneManager = GameSceneManager.Instance;
      _collider = GetComponent<Collider>();

      if (_gameSceneManager != null && _collider != null)
      {
        _gameSceneManager.RegisterInteractiveItem(_collider.GetInstanceID(), this);
      }
    }

    /// <summary>
    /// derived classes will use method to get the items on on the cross hair
    /// </summary>
    /// <returns></returns>
    public virtual string GetText()
    {
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="characterManager"></param>
    public virtual void Activate(CharacterManager characterManager)
    {
    }
  }
}