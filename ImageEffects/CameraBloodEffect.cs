using UnityEngine;

namespace Dead_Earth.Scripts.ImageEffects
{
  /// <summary>
  /// uses the Camera Blood Effect Shader
  /// Creates a blood effect on the camera render when the main player takes damage
  /// </summary>
  [ExecuteInEditMode]
  public class CameraBloodEffect : MonoBehaviour
  {
    [SerializeField] private Texture2D bloodTexture;
    [SerializeField] private Texture2D bloodNormalMap;

    [SerializeField] private float bloodAmount = 0f;
    [SerializeField] private float minBloodAmount = 0f;
    [SerializeField] private float distortion = 1f;

    [Tooltip("Automatically decrements the bloodAmount to minBloodAmount over time")] [SerializeField]
    private bool autoFade = true;

    [Tooltip("How quickly the blood effect fades over time")] [SerializeField]
    private float fadeSpeed = 0.1f;


    // reference to our image effect to shader
    private Shader _shader;
    private Material _material;


    // public props
    public float BloodAmount
    {
      get => bloodAmount;
      set => bloodAmount = value;
    }

    public float MinBloodAmount
    {
      get => minBloodAmount;
      set => minBloodAmount = value;
    }

    public bool AutoFade
    {
      get => autoFade;
      set => autoFade = value;
    }

    public float FadeSpeed
    {
      get => fadeSpeed;
      set => fadeSpeed = value;
    }


    private void Update()
    {
      if (autoFade)
      {
        bloodAmount -= fadeSpeed * Time.deltaTime;
        bloodAmount = Mathf.Max(bloodAmount, minBloodAmount);
      }
    }

    /// <summary>
    /// Runs when rendering on the camera
    /// this function only runs on the camera
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dest"></param>
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
      _shader = Shader.Find("Hidden/Image Effects/Camera Blood Effect");
      if (_shader == null) return;

      if (_material == null)
      {
        // it is the first invocation of this function
        // assign our material
        _material = new Material(_shader);
      }

      if (_material == null) return;

      // Send data into the shader
      // make sure the string references match the variables in the Shader 
      _material.SetTexture("_BloodTex", bloodTexture);
      _material.SetTexture("_BloodBump", bloodNormalMap);
      _material.SetFloat("_Distortion", distortion);
      _material.SetFloat("_BloodAmount", bloodAmount);


      // render the shader as image effect
      Graphics.Blit(src, dest, _material);
    }
  }
}