using System;
using UnityEngine;

namespace Dead_Earth.Scripts.Utilities
{
  /// <summary>
  /// self destruction script
  /// used on the prefabs and destroys itself after a specified time
  /// so instantiated prefabs in runtime doesnt clutter the scene hierarchy
  /// </summary>
  public class TimedDestruct : MonoBehaviour
  {
    [SerializeField] private float time = 10f;


    private void Awake()
    {
      Invoke(nameof(DestroyNow), time);
    }

    private void DestroyNow()
    {
      Destroy(gameObject);
    }
  }
}