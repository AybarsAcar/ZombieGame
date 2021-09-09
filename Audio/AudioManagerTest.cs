using UnityEngine;

namespace Dead_Earth.Scripts.Audio
{
  public class AudioManagerTest : MonoBehaviour
  {
    public AudioClip clip;


    private void Start()
    {
      if (AudioManager.Instance)
      {
        AudioManager.Instance.SetTrackVolume("Zombies", 10, 5);

        InvokeRepeating(nameof(PlayTest), 1, 1);
      }
    }


    void PlayTest()
    {
      // 0 spatial blend means 2D 
      AudioManager.Instance.PlayOneShotSound("Player", clip, transform.position, 0.5f, 0f, 128);
    }
  }
}