using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Dead_Earth.Scripts.FPS
{
  public enum ScreenFadeType
  {
    FadeIn,
    FadeOut,
  }

  /// <summary>
  /// Manages the Player HUD
  /// </summary>
  public class PlayerHUD : MonoBehaviour
  {
    [SerializeField] private Reticle crossHair;
    [SerializeField] private Text healthText;
    [SerializeField] private Text staminaText;
    [SerializeField] private Text interactionText;
    [SerializeField] private Text missionText;
    [SerializeField] private Image screenFade;

    [SerializeField] private float missionTextDisplayTime = 3f;

    private float _currentFadeLevel = 1f;
    private IEnumerator _coroutine;

    private void Start()
    {
      var color = screenFade.color;
      color.a = _currentFadeLevel;
      screenFade.color = color;

      Invoke(nameof(HideMissionText), missionTextDisplayTime);
    }

    public void Fade(float seconds, ScreenFadeType fadeType)
    {
      if (_coroutine != null)
      {
        StopCoroutine(_coroutine);
      }

      var targetFade = fadeType switch
      {
        ScreenFadeType.FadeIn => 0f,
        ScreenFadeType.FadeOut => 1f,
        _ => 0f
      };

      _coroutine = FadeInternal(seconds, targetFade);
      StartCoroutine(_coroutine);
    }

    private IEnumerator FadeInternal(float seconds, float targetFade)
    {
      if (!screenFade) yield break;

      var timer = 0f;
      var srcFade = _currentFadeLevel;
      var oldColor = screenFade.color;

      if (seconds < 0.1f) seconds = 0.1f;

      while (timer < seconds)
      {
        timer += Time.deltaTime;
        _currentFadeLevel = Mathf.Lerp(srcFade, targetFade, timer / seconds);
        oldColor.a = _currentFadeLevel;
        screenFade.color = oldColor;

        yield return null;
      }

      oldColor.a = _currentFadeLevel = targetFade;
      screenFade.color = oldColor;
    }

    public void Invalidate(CharacterManager characterManager)
    {
      if (characterManager == null) return;

      healthText.text = $"Health {(int)characterManager.Health}%";
      staminaText.text = $"Stamina {(int)characterManager.Stamina}%";
    }

    public void SetInteractionText(string text)
    {
      if (text == null)
      {
        // remove the object
        interactionText.gameObject.SetActive(false);
      }
      else
      {
        interactionText.text = text;
        interactionText.gameObject.SetActive(true);
      }
    }

    public void ShowMissionText(string text)
    {
      missionText.text = text;
      missionText.gameObject.SetActive(true);
    }

    public void HideMissionText()
    {
      missionText.gameObject.SetActive(false);
    }
  }
}