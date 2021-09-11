using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

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
  /// Describes an audio entity in the pooling system
  /// </summary>
  public class AudioPoolItem
  {
    public GameObject gameObject;
    public Transform transform;
    public AudioSource audioSource;
    public float unimportance = float.MaxValue; // used by the priority system
    public bool isPlaying;
    public IEnumerator coroutine;

    // generated every single time a sound request is made
    // id will be unique each time it plays a sound
    // so this will change over time as we reuse the object to play other sounds
    public ulong id = 0;
  }

  /// <summary>
  /// Manages the Audios in the Game - it is a singleton
  /// provides a pooling system for the Audio's in the scene
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

    [Tooltip("Pool size of the Audio Manager")] [SerializeField]
    private int maxSounds = 10;

    private Dictionary<string, TrackInfo> _tracks = new Dictionary<string, TrackInfo>();
    private List<AudioPoolItem> _pool = new List<AudioPoolItem>();

    // stores the currently playing / active sounds from our pool list
    private Dictionary<ulong, AudioPoolItem> _activePool = new Dictionary<ulong, AudioPoolItem>();

    private ulong _idGiver = 0;
    private Transform _listenerPosition; // current audio listener position

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

      // Generate the pool
      for (int i = 0; i < maxSounds; i++)
      {
        var gameObj = new GameObject("Pool Item");
        var audioSource = gameObj.AddComponent<AudioSource>();

        // add it as a child to the AudioManger
        gameObj.transform.parent = transform;

        var poolItem = new AudioPoolItem
        {
          gameObject = gameObj,
          audioSource = audioSource,
          transform = gameObj.transform,
          isPlaying = false,
        };

        gameObj.SetActive(false);

        _pool.Add(poolItem);
      }
    }

    private void OnEnable()
    {
      SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// a new scene has just been loaded so we need to find the listener
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      // cache the audio listener position in the scene
      _listenerPosition = FindObjectOfType<AudioListener>().transform;
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

    /// <summary>
    /// Plays a One Shot sound
    /// </summary>
    /// <param name="track"></param>
    /// <param name="clip"></param>
    /// <param name="position"></param>
    /// <param name="volume"></param>
    /// <param name="spatialBlend"></param>
    /// <param name="priority">used to generate unimportance value</param>
    /// <returns></returns>
    public ulong PlayOneShotSound(string track, AudioClip clip, Vector3 position, float volume, float spatialBlend,
      int priority = 128)
    {
      if (!_tracks.ContainsKey(track) || clip == null || volume.Equals(0f))
      {
        return 0;
      }

      // less it is better it is
      var unimportance = (_listenerPosition.position - position).sqrMagnitude / Mathf.Max(1, priority);

      var leastImportantIndex = -1;
      var leastImportanceValue = float.MaxValue;

      for (int i = 0; i < _pool.Count; i++)
      {
        var poolItem = _pool[i];

        // not playing audio pool item is available
        if (!poolItem.isPlaying)
        {
          return ConfigurePoolObject(i, track, clip, position, volume, spatialBlend, unimportance);
        }

        // the current pool audio item is playing but less important than our new audio
        if (poolItem.unimportance > leastImportanceValue)
        {
          leastImportanceValue = poolItem.unimportance;
          leastImportantIndex = i;
        }
      }

      // all the pool items are playing a sound but we have a less important audio playing
      // so we stop the least important audio to play the new audio if the new audio is not the least important one
      if (leastImportanceValue > unimportance)
      {
        return ConfigurePoolObject(leastImportantIndex, track, clip, position, volume, spatialBlend,
          unimportance);
      }

      // our audio is less important the the least important in our currently playing list
      // the new audio is not played
      return 0;
    }

    /// <summary>
    /// Queue up a one shot sound to be played after a number of seconds
    /// </summary>
    /// <param name="track"></param>
    /// <param name="clip"></param>
    /// <param name="position"></param>
    /// <param name="volume"></param>
    /// <param name="spatialBlend"></param>
    /// <param name="duration"></param>
    /// <param name="priority"></param>
    /// <returns></returns>
    public IEnumerator PlayOneShotSoundWithDelay(string track, AudioClip clip, Vector3 position, float volume,
      float spatialBlend, float duration, int priority = 128)
    {
      yield return new WaitForSeconds(duration);

      PlayOneShotSound(track, clip, position, volume, spatialBlend, priority);
    }

    /// <summary>
    /// Stops the currently playing sound passed in the id
    /// </summary>
    /// <param name="id"></param>
    public void StopOneShotSound(ulong id)
    {
      if (_activePool.TryGetValue(id, out var activeSound))
      {
        StopCoroutine(activeSound.coroutine);

        activeSound.audioSource.Stop();
        activeSound.audioSource.clip = null;
        activeSound.gameObject.SetActive(false);

        // remove from the active list
        _activePool.Remove(id);

        activeSound.isPlaying = false;
      }
    }

    /// <summary>
    /// this will play a one shot sound from our Pool List
    /// </summary>
    /// <param name="poolIndex"></param>
    /// <param name="track"></param>
    /// <param name="clip"></param>
    /// <param name="position"></param>
    /// <param name="volume"></param>
    /// <param name="spatialBlend"></param>
    /// <param name="unimportance"></param>
    /// <returns></returns>
    protected ulong ConfigurePoolObject(int poolIndex, string track, AudioClip clip, Vector3 position, float volume,
      float spatialBlend, float unimportance)
    {
      if (poolIndex < 0 || poolIndex >= _pool.Count) return 0;

      var poolItem = _pool[poolIndex];

      _idGiver++;

      var source = poolItem.audioSource;
      source.clip = clip;
      source.volume = volume;
      source.spatialBlend = spatialBlend;

      // assign to requested audio group / track
      source.outputAudioMixerGroup = _tracks[track].group;

      source.transform.position = position;

      poolItem.isPlaying = true;
      poolItem.unimportance = unimportance;
      poolItem.id = _idGiver;
      poolItem.gameObject.SetActive(true);

      source.Play();

      poolItem.coroutine = StopSoundDelayed(_idGiver, source.clip.length);
      StartCoroutine(poolItem.coroutine);

      // add the audio pool item to the active audio's pool 
      _activePool.Add(_idGiver, poolItem);

      // return the id to the called
      return _idGiver;
    }

    /// <summary>
    /// plays the audio as its duration
    /// then stops the sound and cleans up the audio pool item
    /// before removing from the active audio pool list
    /// </summary>
    /// <param name="id"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    private IEnumerator StopSoundDelayed(ulong id, float duration)
    {
      yield return new WaitForSeconds(duration);

      if (_activePool.TryGetValue(id, out var poolItem))
      {
        // stop the audio and cleanup before removing from the active dictionary 
        poolItem.audioSource.Stop();
        poolItem.audioSource.clip = null;
        poolItem.gameObject.SetActive(false);

        _activePool.Remove(id);

        poolItem.isPlaying = false;
      }
    }
  }
}