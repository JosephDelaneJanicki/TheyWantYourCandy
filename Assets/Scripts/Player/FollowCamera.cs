using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 12, -10);
    public float smoothness = 8f;

    void LateUpdate()
    {
        if (!target) return;
        transform.position = Vector3.Lerp(transform.position, target.position + offset, 1f - Mathf.Exp(-smoothness * Time.deltaTime));
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}
