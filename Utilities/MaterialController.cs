using System;
using UnityEngine;

namespace Dead_Earth.Scripts.Utilities
{
  [Serializable]
  public class MaterialController
  {
    [SerializeField] private Material material;

    [SerializeField] private Texture diffuseTexture;
    [SerializeField] private Color diffuseColor = Color.white;
    [SerializeField] private Texture normalMap;
    [SerializeField] private float normalStrength = 1f;

    [SerializeField] private Texture emissiveTexture;
    [SerializeField] private Color emissionColor = Color.black;
    [SerializeField] private float emissionScale = 1f;


    // internal as a data backup
    private MaterialController _backup = null;
    private bool _started = false;

    public Material Material => material;


    /// <summary>
    /// 
    /// </summary>
    public void OnStart()
    {
      if (material == null || _started) return;
      _started = true;

      _backup = new MaterialController
      {
        // backup settings in a temp controller fetched from the shader code
        diffuseColor = material.GetColor("_Color"),
        diffuseTexture = material.GetTexture("_MainTex"),
        emissionColor = material.GetColor("_EmissionColor"),
        emissionScale = 1f,
        emissiveTexture = material.GetTexture("_EmissionMap"),
        normalMap = material.GetTexture("_BumpMap"),
        normalStrength = material.GetFloat("_BumpScale")
      };

      // Register this controller with teh game scene manager using material instance id
      // the GameSceneManager will reset all registered materials when the scene closes
      if (GameSceneManager.Instance)
      {
        GameSceneManager.Instance.RegisterMaterialController(material.GetInstanceID(), this);
      }
    }

    /// <summary>
    /// sets the material properties to activated properties
    /// </summary>
    /// <param name="activate">we wish to activate or deactivate</param>
    public void Activate(bool activate)
    {
      if (!_started || material == null) return;

      if (activate)
      {
        material.SetColor("_Color", diffuseColor);
        material.SetTexture("_MainTex", diffuseTexture);
        material.SetColor("_EmissionColor", emissionColor * emissionScale);
        material.SetTexture("_EmissionMap", emissiveTexture);
        material.SetTexture("_BumpMap", normalMap);
        material.SetFloat("_BumpScale", normalStrength);
      }
      else
      {
        material.SetColor("_Color", _backup.diffuseColor);
        material.SetTexture("_MainTex", _backup.diffuseTexture);
        material.SetColor("_EmissionColor", _backup.emissionColor * _backup.emissionScale);
        material.SetTexture("_EmissionMap", _backup.emissiveTexture);
        material.SetTexture("_BumpMap", _backup.normalMap);
        material.SetFloat("_BumpScale", _backup.normalStrength);
      }
    }

    /// <summary>
    /// Called by the GameSceneManager's OnDestroy function
    /// so we reset material properties
    /// </summary>
    public void OnReset()
    {
      if (_backup == null || material == null) return;

      material.SetColor("_Color", _backup.diffuseColor);
      material.SetTexture("_MainTex", _backup.diffuseTexture);
      material.SetColor("_EmissionColor", _backup.emissionColor * _backup.emissionScale);
      material.SetTexture("_EmissionMap", _backup.emissiveTexture);
      material.SetTexture("_BumpMap", _backup.normalMap);
      material.SetFloat("_BumpScale", _backup.normalStrength);
    }

    /// <summary>
    /// returns the instance id of the underlying material 
    /// </summary>
    /// <returns></returns>
    public int GetInstanceId()
    {
      return material.GetInstanceID();
    }
  }
}