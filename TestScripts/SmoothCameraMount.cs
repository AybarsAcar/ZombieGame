using UnityEngine;

namespace Dead_Earth.Scripts.TestScripts
{
  public class SmoothCameraMount : MonoBehaviour
  {
    [SerializeField] private Transform mount;
    [SerializeField] private float speed = 5f;

    private void LateUpdate()
    {
      transform.position = Vector3.Lerp(transform.position, mount.transform.position, Time.deltaTime * speed);
      transform.rotation = Quaternion.Slerp(transform.rotation, mount.rotation, Time.deltaTime * speed);
    }
  }
}