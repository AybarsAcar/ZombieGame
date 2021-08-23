using System.Collections.Generic;
using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  public enum PathDisplayMode
  {
    None,
    Connections,
    Paths
  }

  public class AIWaypointNetwork : MonoBehaviour
  {
    // HideInInspector to hide it so we can override with our custom Inspector EditorGUI
    [HideInInspector] public PathDisplayMode DisplayMode = PathDisplayMode.Connections;

    [HideInInspector] public int UIStart = 0;

    [HideInInspector] public int UIEnd = 0;

    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    public List<Transform> Waypoints => waypoints;
  }
}