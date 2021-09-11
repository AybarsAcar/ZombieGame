using System.Collections.Generic;
using UnityEngine;

namespace Dead_Earth.Scripts.ScriptableObjects
{
  /// <summary>
  /// used as an exclusion list in our Animator so they play the sound on the layer
  /// that is in exclusively
  /// </summary>
  [CreateAssetMenu(fileName = "New String List", menuName = "Dead Earth/String List", order = 0)]
  public class StringList : ScriptableObject
  {
    [SerializeField] private List<string> stringList = new List<string>();

    public string this[int i] => i < stringList.Count ? stringList[i] : null;
  }
}