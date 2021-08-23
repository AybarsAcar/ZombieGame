using UnityEngine;

namespace Dead_Earth.Scripts.TestScripts
{
  public class Controller : MonoBehaviour
  {
    private Animator _animator;

    private int _horizontalHash;
    private int _verticalHash;
    private int _attackHash;

    private void Awake()
    {
      _animator = GetComponent<Animator>();
    }


    private void Start()
    {
      // get the animator parameter hashes for performance
      _horizontalHash = Animator.StringToHash("horizontal");
      _verticalHash = Animator.StringToHash("vertical");
      _attackHash = Animator.StringToHash("attack");
    }

    private void Update()
    {
      var xAxis = Input.GetAxis("Horizontal") * 2.32f;
      var yAxis = Input.GetAxis("Vertical") * 5.66f;

      if (Input.GetMouseButton(0))
      {
        _animator.SetTrigger(_attackHash);
      }

      _animator.SetFloat(_horizontalHash, xAxis, 0.1f, Time.deltaTime);
      _animator.SetFloat(_verticalHash, yAxis, 0.1f, Time.deltaTime);
    }
  }
}