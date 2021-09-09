using UnityEngine;

namespace Dead_Earth.Scripts.Audio
{
  public class AudioManagerTest : MonoBehaviour
  {
    private void Start()
    {
      if (AudioManager.Instance)
      {
        AudioManager.Instance.SetTrackVolume("Zombies", 10, 5);
      }
    }
  }
}