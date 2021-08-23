using System.Linq;
using Dead_Earth.Scripts.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.Editor
{
  /// <summary>
  /// custom editor for the AIWaypointNetwork script
  /// </summary>
  [CustomEditor(typeof(AIWaypointNetwork))]
  public class AIWaypointNetworkEditor : UnityEditor.Editor
  {
    /// <summary>
    /// called when the component is rendered on the inspector
    /// </summary>
    public override void OnInspectorGUI()
    {
      // get a reference to the network
      var network = (AIWaypointNetwork)target;

      network.DisplayMode = (PathDisplayMode)EditorGUILayout.EnumPopup("Display Mode", network.DisplayMode);

      if (network.DisplayMode == PathDisplayMode.Paths)
      {
        network.UIStart =
          EditorGUILayout.IntSlider("Waypoint Start", network.UIStart, 0, network.Waypoints.Count() - 1);
        network.UIEnd = EditorGUILayout.IntSlider("Waypoint End", network.UIEnd, 0, network.Waypoints.Count() - 1);
      }

      // draws the unhidden properties as default
      DrawDefaultInspector();
    }

    /// <summary>
    /// called when the component is in the scene
    /// </summary>
    private void OnSceneGUI()
    {
      var network = (AIWaypointNetwork)target;

      // draw the waypoint labels
      for (int i = 0; i < network.Waypoints.Count; i++)
      {
        if (network.Waypoints[i] != null)
        {
          Handles.Label(network.Waypoints[i].position, $"Waypoint {i}");
        }
      }

      if (network.DisplayMode == PathDisplayMode.Connections)
      {
        DrawConnections(network);
      }
      else if (network.DisplayMode == PathDisplayMode.Paths)
      {
        // Path mode
        DrawPaths(network);
      }
    }

    /// <summary>
    /// draws tha path
    /// the shortest distance in A* Unity NavMesh
    /// </summary>
    /// <param name="network"></param>
    private void DrawPaths(AIWaypointNetwork network)
    {
      var path = new NavMeshPath();

      var from = network.Waypoints[network.UIStart].position;
      var to = network.Waypoints[network.UIEnd].position;

      // gets all the corner points in the agent's path
      NavMesh.CalculatePath(@from, to, NavMesh.AllAreas, path);

      Handles.color = Color.yellow;
      Handles.DrawPolyLine(path.corners);
    }

    /// <summary>
    /// draws connections between the waypoints
    /// </summary>
    /// <param name="network"></param>
    private void DrawConnections(AIWaypointNetwork network)
    {
      var linePoints = new Vector3[network.Waypoints.Count + 1];

      for (int i = 0; i <= network.Waypoints.Count; i++)
      {
        var index = i != network.Waypoints.Count ? i : 0;

        if (network.Waypoints[index] != null)
        {
          linePoints[i] = network.Waypoints[index].position;
        }
        else
        {
          linePoints[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        }
      }

      Handles.color = Color.cyan;
      Handles.DrawPolyLine(linePoints);
    }
  }
}