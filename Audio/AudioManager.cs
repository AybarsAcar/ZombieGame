using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Dead_Earth.Scripts.Audio
{
  /// <summary>
  /// stores all the information about an Audio Mixer Group
  /// contains the name of the group which is exposed 
  /// </summary>
  public class TrackInfo
  {
    public string name = string.Empty;
    public AudioMixerGroup group;
    public IEnumerator trackFader;
  }

  /// <summary>
  /// Manages the Audios in the Game - it is a singleton
  /// </summary>
  public class AudioManager : MonoBehaviour
  {
    private static AudioManager _instance;

    public static AudioManager Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = FindObjectOfType<AudioManager>();
        }

        return _instance;
      }
    }

    [SerializeField] private AudioMixer mixer;

    private Dictionary<string, TrackInfo> _tracks = new Dictionary<string, TrackInfo>();

    private void Awake()
    {
      DontDestroyOnLoad(gameObject);

      if (mixer == null) return;

      // fetch all the groups in the mixer - these will be our mixer tracks
      var groups = mixer.FindMatchingGroups(string.Empty);

      // create our mixer tracks based on group name (Track -> AudioGroup)
      foreach (var mixerGroup in groups)
      {
        var trackInfo = new TrackInfo
        {
          name = mixerGroup.name,
          group = mixerGroup,
          trackFader = null
        };

        _tracks.Add(mixerGroup.name, trackInfo);
      }
    }

    /// <summary>
    /// Returns the Volume of the Track volume passed on the volume if exists
    /// if does not exist returns the min float value
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    public float GetTrackVolume(string track)
    {
      if (_tracks.TryGetValue(track, out var trackInfo))
      {
        mixer.GetFloat(track, out var volume);
        return volume;
      }

      return float.MinValue;
    }

    /// <summary>
    /// returns the AudioMixerGroup of a given trackName
    /// returns null if the trackName does not exist 
    /// </summary>
    /// <param name="trackName"></param>
    /// <returns></returns>
    public AudioMixerGroup GetAudioGroupFromTrackName(string trackName)
    {
      if (_tracks.TryGetValue(trackName, out var trackInfo))
      {
        return trackInfo.group;
      }

      return null;
    }


    /// <summary>
    /// public wrapper of the internal coroutine
    /// Sets the volume of the AudioMixerGroup assigned to the passed track
    /// AudioMixerGroup MUST expose its volume variable to script for this to work and the variable MUST be
    /// the same as teh name of the group
    /// if a fade is given a coroutine will be used to perform the fade
    /// </summary>
    /// <param name="track"></param>
    /// <param name="volume"></param>
    /// <param name="fadeTime"></param>
    public void SetTrackVolume(string track, float volume, float fadeTime = 0f)
    {
      if (!mixer) return;

      if (_tracks.TryGetValue(track, out var trackInfo))
      {
        // stop any coroutine that might be in the middle of fading this track
        if (trackInfo.trackFader != null)
        {
          StartCoroutine(trackInfo.trackFader);
        }

        if (fadeTime == 0f)
        {
          mixer.SetFloat(track, volume);
        }
        else
        {
          // cache the coroutine
          trackInfo.trackFader = SetTrackVolumeInternal(track, volume, fadeTime);

          // execute the coroutine
          StartCoroutine(trackInfo.trackFader);
        }
      }
    }

    /// <summary>
    /// Coroutine used by SetTrackVolume to implement a fade between volumes
    /// of a track over time
    /// </summary>
    /// <param name="track"></param>
    /// <param name="volume"></param>
    /// <param name="fadeTime"></param>
    /// <returns></returns>
    protected IEnumerator SetTrackVolumeInternal(string track, float volume, float fadeTime)
    {
      var startVolume = 0f; // current volume of the audio mixer prior to the animation running
      var timer = 0f; // time elapsed since the coroutine started running

      mixer.GetFloat(track, out startVolume);

      while (timer < fadeTime)
      {
        timer += Time.unscaledDeltaTime;

        // fade to the volume over the fade time
        mixer.SetFloat(track, Mathf.Lerp(startVolume, volume, timer / fadeTime));

        yield return null;
      }

      // set the volume to the target passed in
      // this line is to be safe against floating point precision
      // this is ideally achieved in the while loop above
      mixer.SetFloat(track, volume);
    }
  }
}