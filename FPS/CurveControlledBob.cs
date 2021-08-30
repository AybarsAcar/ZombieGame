using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dead_Earth.Scripts.FPS
{
  /// <summary>
  /// Head Bob behaviour for the FPS
  /// </summary>
  [Serializable]
  public class CurveControlledBob
  {
    [Tooltip("Head bob animation curve")] [SerializeField]
    private AnimationCurve bobCurve =
      new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f),
        new Keyframe(1.5f, -1f),
        new Keyframe(2f, 0f)
      );

    [Tooltip("Horizontal Head Movement Multiplier")] [SerializeField]
    private float horizontalMultiplier = 0.01f;

    [Tooltip("Vertical Head Movement Multiplier")] [SerializeField]
    private float verticalMultiplier = 0.02f;

    // if larger than 1, y movement will be faster than the x movement
    [SerializeField] private float verticalToHorizontalSpeedRatio = 2f;

    [Tooltip("The interval of the  head bob wave")] [SerializeField]
    private float _baseInterval = 1f;


    private float _previousXPlayHead, _previousYPlayHead;
    private float _xPlayHead, _yPlayHead;
    private float _curveEndTime;

    private List<CurveControlledBobEvent> _events = new List<CurveControlledBobEvent>();

    /// <summary>
    /// our own class initializer
    /// basically initializes the class variables similar to a constructor
    /// </summary>
    public void Init()
    {
      _curveEndTime = bobCurve[bobCurve.length - 1].time;

      _xPlayHead = 0f;
      _yPlayHead = 0f;
      _previousXPlayHead = 0f;
      _previousYPlayHead = 0f;
    }

    /// <summary>
    /// registration function
    /// </summary>
    /// <param name="time"></param>
    /// <param name="func"></param>
    /// <param name="type"></param>
    public void RegisterEventCallback(float time, CurveControlledBobCallback func, CurveControllerBobCallbackType type)
    {
      var eventToAdd = new CurveControlledBobEvent
      {
        time = time,
        func = func,
        type = type
      };

      _events.Add(eventToAdd);

      // sort based on the time the event will be invoked
      // we will compare based on their time to be called
      _events.Sort((t1, t2) => t1.time.CompareTo(t2.time));
    }

    /// <summary>
    /// 3D vector which is the offset to add to the local position of hte camera
    /// </summary>
    /// <param name="speed">current character controller speed</param>
    /// <returns></returns>
    public Vector3 GetVectorOffset(float speed)

    {
      _xPlayHead += (speed * Time.deltaTime) / _baseInterval;
      _yPlayHead += ((speed * Time.deltaTime) / _baseInterval) * verticalToHorizontalSpeedRatio;

      if (_xPlayHead > _curveEndTime)
      {
        _xPlayHead -= _curveEndTime;
      }

      if (_yPlayHead > _curveEndTime)
      {
        _yPlayHead -= _curveEndTime;
      }

      // process events
      foreach (var bobEvent in _events)
      {
        if (bobEvent != null)
        {
          if (bobEvent.type == CurveControllerBobCallbackType.Vertical)
          {
            if ((_previousYPlayHead < bobEvent.time && _yPlayHead >= bobEvent.time) ||
                (_previousYPlayHead > _yPlayHead &&
                 (bobEvent.time > _previousYPlayHead || bobEvent.time <= _yPlayHead)))
            {
              // call the callback
              bobEvent.func();
            }
          }
          else
          {
            // horizontal bob type
            if ((_previousXPlayHead < bobEvent.time && _xPlayHead >= bobEvent.time) ||
                (_previousXPlayHead > _xPlayHead &&
                 (bobEvent.time > _previousXPlayHead || bobEvent.time <= _xPlayHead)))
            {
              // call the callback
              bobEvent.func();
            }
          }
        }
      }

      var xPos = bobCurve.Evaluate(_xPlayHead) * horizontalMultiplier;
      var yPos = bobCurve.Evaluate(_yPlayHead) * verticalMultiplier;

      _previousXPlayHead = _xPlayHead;
      _previousYPlayHead = _yPlayHead;

      // to be added to the local position of the camera 
      return new Vector3(xPos, yPos, 0f);
    }
  }
}