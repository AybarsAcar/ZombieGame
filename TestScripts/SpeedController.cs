using UnityEngine;

namespace Dead_Earth.Scripts.TestScripts
{
  public class SpeedController : MonoBehaviour
  {
    public float speed = 0f;

    private Animator _animator;

    private void Awake()
    {
      _animator = GetComponent<Animator>();
    }

    private void Update()
    {
      _animator.SetFloat("speed", speed);
    }
  }
}