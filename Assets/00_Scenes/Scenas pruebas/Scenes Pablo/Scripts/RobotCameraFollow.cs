using UnityEngine;

public class RobotCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 4, -6);
    public float smooth = 8f;
    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
        transform.LookAt(target.position + Vector3.up * 0.5f);
    }
}
