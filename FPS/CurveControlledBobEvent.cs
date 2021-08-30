using System;

namespace Dead_Earth.Scripts.FPS
{
  [Serializable]
  public class CurveControlledBobEvent
  {
    public float time = 0f;
    public CurveControlledBobCallback func = null;
    public CurveControllerBobCallbackType type = CurveControllerBobCallbackType.Vertical;
  }
}