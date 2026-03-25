using UnityEngine;

namespace NetizenSphere.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -8f);
        [SerializeField] private float smoothSpeed = 8f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            Debug.Log("CameraFollow: target set to " + newTarget.name);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
                if (player != null)
                {
                    target = player.transform;
                    Debug.Log("CameraFollow: auto-found player.");
                }
                else
                {
                    return;
                }
            }

            transform.position = target.position + offset;
            transform.LookAt(target);
        }
    }
}
