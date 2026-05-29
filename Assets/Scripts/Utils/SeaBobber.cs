using UnityEngine;

namespace TheAlchemistsCrypt.Utils
{
    public class SeaBobber : MonoBehaviour
    {
        public float speed = 0.8f;
        public float height = 0.25f;
        public float timeOffset = 0f;
        private float startY;

        private void Start()
        {
            startY = transform.position.y;
        }

        private void Update()
        {
            float newY = startY + Mathf.Sin(Time.time * speed + timeOffset) * height;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}
