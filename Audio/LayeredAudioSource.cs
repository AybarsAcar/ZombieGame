using System.Collections.Generic;
using Dead_Earth.Scripts.ScriptableObjects;
using UnityEngine;

namespace Dead_Earth.Scripts.Audio
{
  public class AudioLayer
  {
    public AudioClip clip;
    public AudioCollection collection;
    public int bank = 0;
    public bool isLooping = true;
    public float time = 0f;
    public float duration = 0f;
    public bool isMuted = false;
  }

  /// <summary>
  /// this interface doesn't define the Update function
  /// </summary>
  public interface ILayeredAudioSource
  {
    public bool Play(AudioCollection collection, int bank, int layer, bool looping = true);
    public void Stop(int layerIndex);
    public void Mute(int layerIndex, bool muted);
    public void Mute(bool muted);
  }

  public class LayeredAudioSource : ILayeredAudioSource
  {
    public AudioSource AudioSource => _audioSource;

    private AudioSource _audioSource;
    private List<AudioLayer> _audioLayers = new List<AudioLayer>();
    private int _activeLayer = -1;

    public LayeredAudioSource(AudioSource source, int layers)
    {
      if (source != null && layers > 0)
      {
        _audioSource = source;

        // create requested number of layers
        for (int i = 0; i < layers; i++)
        {
          var newLayer = new AudioLayer
          {
            collection = null,
            duration = 0f,
            time = 0f,
            isLooping = false,
            bank = 0,
            isMuted = false,
            clip = null
          };

          _audioLayers.Add(newLayer);
        }
      }
    }

    /// <summary>
    /// to tell this layered source to play a sound
    /// sets some variables so in the next Update function sound plays
    /// </summary>
    /// <param name="collection">Collection that the clip is selected from</param>
    /// <param name="bank">in the audio collection we want to use</param>
    /// <param name="layer">int index of the layer we like the sound to be played on</param>
    /// <param name="looping">whether it is a looping layer on not</param>
    /// <returns></returns>
    public bool Play(AudioCollection collection, int bank, int layer, bool looping = true)
    {
      if (layer >= _audioLayers.Count) return false;

      var audioLayer = _audioLayers[layer];

      // already doing what we are doing
      if (audioLayer.collection == collection && audioLayer.isLooping == looping && audioLayer.bank == bank)
      {
        return true;
      }

      // new audio request so copy over the data adn store in the layer
      audioLayer.collection = collection;
      audioLayer.bank = bank;
      audioLayer.isLooping = looping;
      audioLayer.time = 0f;
      audioLayer.duration = 0f;
      audioLayer.isMuted = false;
      audioLayer.clip = null; // will be assigned in the Update function

      return true;
    }

    /// <summary>
    /// Updates the time of all layered clips and makes sure that the audio source
    /// is playing hte clip on the highest layer
    /// </summary>
    public void Update()
    {
      var newActiveLayer = -1;
      var refreshAudioSource = false;

      for (int i = _audioLayers.Count - 1; i >= 0; i--)
      {
        var layer = _audioLayers[i];

        // ignore unassigned layers
        if (layer.collection == null) continue;

        // update the internal play head of the layer
        layer.time += Time.deltaTime;

        // if it has exceeded its duration then we need to take action
        if (layer.time > layer.duration)
        {
          if (layer.isLooping || layer.clip == null)
          {
            // assign e new clip if the layer is a looping layer or the clip is null
            // which means it is a new request
            var clip = layer.collection[layer.bank];

            // calculate the play position based on the time of the layer and store duration
            if (clip == layer.clip)
            {
              // wrap around the clip for looping audio
              layer.time %= layer.clip.length;
            }
            else
            {
              layer.time = 0f;
            }

            // assign the clip and the duration
            layer.duration = clip.length;
            layer.clip = clip;

            // this is a layer that has focus so we need to choose and play
            // a new clip from the pool
            if (newActiveLayer < i )
            {
              // this is the new active layer index
              newActiveLayer = i;

              // we need to issue a play command to the audio source
              refreshAudioSource = true;
            }
          }
          else
          {
            // the layer time has exceeded the duration and it is not a looping audio
            // so deactivate the layer
            layer.clip = null;
            layer.collection = null;
            layer.duration = 0f;
            layer.bank = 0;
            layer.isLooping = false;
            layer.time = 0f;
          }
        }
        else
        {
          // layer is currently playing
          if (newActiveLayer < i)
          {
            // this is the highest layer then record that which is the clip currently playing
            newActiveLayer = i;
          }
        }
      }

      // if we found a new active layer (or none)
      // this update has seen a change and the current audio source is out of date
      if (newActiveLayer != _activeLayer || refreshAudioSource)
      {
        if (newActiveLayer == -1)
        {
          // nothing to play
          _audioSource.Stop();
        }
        else
        {
          // w edo have an active layer
          var layer = _audioLayers[newActiveLayer];

          _audioSource.clip = layer.clip;
          _audioSource.volume = layer.isMuted ? 0f : layer.collection.Volume;
          _audioSource.spatialBlend = layer.collection.SpatialBlend;
          _audioSource.time = layer.time;

          // we wont be looping the audio source
          // we handle the looping in the layer so we are looping the collection and fetch a new
          // audio clip from the collection when looping
          _audioSource.loop = false;

          // so we don't have set the output property of the Audio Source in the AI Zombies
          // it is fetched and assigned dynamically
          _audioSource.outputAudioMixerGroup =
            AudioManager.Instance.GetAudioGroupFromTrackName(layer.collection.AudioGroup);

          _audioSource.Play();
        }
      }

      // assign the new active layer to the class variable
      _activeLayer = newActiveLayer;

      // set the volume of the layer
      if (_activeLayer != -1 && _audioSource != null)
      {
        var audioLayer = _audioLayers[_activeLayer];

        if (audioLayer.isMuted)
        {
          _audioSource.volume = 0;
        }
        else
        {
          _audioSource.volume = audioLayer.collection.Volume;
        }
      }
    }


    /// <summary>
    /// Stops any sound in the layer given the layer index
    /// </summary>
    /// <param name="layerIndex">int index of the layer</param>
    public void Stop(int layerIndex)
    {
      if (layerIndex > _audioLayers.Count) return;

      var layer = _audioLayers[layerIndex];
      if (layer != null)
      {
        layer.isLooping = false;
        layer.time = layer.duration;
      }
    }

    /// <summary>
    /// whether to mute or unmute an audio on a given layer
    /// </summary>
    /// <param name="layerIndex"></param>
    /// <param name="muted"></param>
    public void Mute(int layerIndex, bool muted)
    {
      if (layerIndex > _audioLayers.Count) return;
      var layer = _audioLayers[layerIndex];

      if (layer != null)
      {
        layer.isMuted = muted;
      }
    }

    /// <summary>
    /// mutes or un-mutes the audio on every layer
    /// </summary>
    /// <param name="mute"></param>
    public void Mute(bool mute)
    {
      for (int i = 0; i < _audioLayers.Count; i++)
      {
        Mute(i, mute);
      }
    }
  }
}