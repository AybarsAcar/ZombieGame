using System;
using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// attached to the objects that emits sound that triggers the AIZombie
  /// attached to the SoundEmitter to under the Player as well
  /// </summary>
  [RequireComponent(typeof(SphereCollider))]
  public class AISoundEmitter : MonoBehaviour
  {
    [Tooltip("The rate that the sound decades")] [SerializeField]
    private float decayRate = 1f;

    private SphereCollider _collider;
    private float _sourceRadius;
    private float _targetRadius;
    private float _interpolator;
    private float _interpolatorSpeed;

    private void Awake()
    {
      _collider = GetComponent<SphereCollider>();
      if (!_collider) return;

      // initialise radius values
      _sourceRadius = _targetRadius = _collider.radius;

      // setup interpolator
      _interpolator = 0f;
      if (decayRate > 0.02f)
      {
        _interpolatorSpeed = 1f / decayRate;
      }
      else
      {
        _interpolatorSpeed = 0;
      }
    }

    private void FixedUpdate()
    {
      _interpolator = Mathf.Clamp01(_interpolator + Time.deltaTime * _interpolatorSpeed);

      // change the radius of the radius
      _collider.radius = Mathf.Lerp(_sourceRadius, _targetRadius, _interpolator);

      _collider.enabled = !(_collider.radius < Mathf.Epsilon);
    }


    /// <summary>
    /// called by the other objects that creates the sounds
    /// sets the target radius of the sound emitter 
    /// </summary>
    /// <param name="radius">target radius of the sound</param>
    /// <param name="instantResize">optional, if true, no interpolation is performed</param>
    public void SetRadius(float radius, bool instantResize = false)
    {
      if (!_collider || Math.Abs(radius - _targetRadius) < Mathf.Epsilon) return;

      _sourceRadius = instantResize || radius > _collider.radius ? radius : _collider.radius;
      _targetRadius = radius;
      _interpolator = 0f;
    }
  }
}