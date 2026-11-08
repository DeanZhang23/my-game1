using UnityEngine;

namespace NiaIsland.Pier
{
    public sealed class NiaMovement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4.5f;
        [SerializeField] float minX = -5.2f;
        [SerializeField] float maxX = 5.2f;

        void Update()
        {
            var axis = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                axis -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                axis += 1f;

            var position = transform.position;
            position.x = PierMovement.Step(position.x, axis, moveSpeed, Time.deltaTime, minX, maxX);
            transform.position = position;

            if (axis < 0f)
                SetFacing(-1f);
            else if (axis > 0f)
                SetFacing(1f);
        }

        void SetFacing(float sign)
        {
            var scale = transform.localScale;
            var magnitude = Mathf.Abs(scale.x);
            if (magnitude < 0.001f)
                magnitude = 1f;
            scale.x = magnitude * sign;
            transform.localScale = scale;
        }

        void OnDrawGizmosSelected()
        {
            var y = transform.position.y;
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawLine(new Vector3(minX, y - 0.2f, 0f), new Vector3(minX, y + 1.4f, 0f));
            Gizmos.DrawLine(new Vector3(maxX, y - 0.2f, 0f), new Vector3(maxX, y + 1.4f, 0f));
        }
    }
}
