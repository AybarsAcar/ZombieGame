using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dead_Earth.Scripts.ScriptableObjects
{
  [Serializable]
  public class ClipBank
  {
    public List<AudioClip> clips = new List<AudioClip>();
  }

  [CreateAssetMenu(fileName = "New Audio Collection", menuName = "Dead Earth/Audio/Audio Collection", order = 0)]
  public class AudioCollection : ScriptableObject
  {
    [Tooltip("Audio group's name")] [SerializeField]
    private string audioGroup = string.Empty;

    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    [Tooltip("2D sound when 0; 3D sound when 1")] [SerializeField] [Range(0f, 1f)]
    private float spatialBlend = 1f;

    [SerializeField] [Range(0, 256)] private int priority = 128;
    [SerializeField] private List<ClipBank> audioClipBanks = new List<ClipBank>();

    public string AudioGroup => audioGroup;
    public float Volume => volume;
    public float SpatialBlend => spatialBlend;
    public int Priority => priority;
    public int BankCount => audioClipBanks.Count;

    /// <summary>
    /// custom array accessor
    /// when we pass an array index it will return a random clip from that bank
    /// </summary>
    /// <param name="i"></param>
    public AudioClip this[int i]
    {
      get
      {
        if (audioClipBanks == null || audioClipBanks.Count <= i) return null;
        if (audioClipBanks[i].clips.Count == 0) return null;

        var clipList = audioClipBanks[i].clips;

        return clipList[Random.Range(0, clipList.Count)];
      }
    }

    public AudioClip Clip
    {
      get
      {
        if (audioClipBanks == null || audioClipBanks.Count <= 0) return null;
        if (audioClipBanks[0].clips.Count == 0) return null;

        var clipList = audioClipBanks[0].clips;
        return clipList[Random.Range(0, clipList.Count)];
      }
    }
  }
}