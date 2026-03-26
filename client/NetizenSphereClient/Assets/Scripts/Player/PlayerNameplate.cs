using TMPro;
using UnityEngine;

namespace NetizenSphere.Player
{
    public class PlayerNameplate : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Transform target;

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        public void SetName(string displayName)
        {
            if (nameText != null)
            {
                nameText.text = displayName;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + Vector3.up * 2.2f;

            if (_mainCamera != null)
            {
                transform.forward = _mainCamera.transform.forward;
            }
        }
    }
}
