using UnityEngine;

namespace NiaIsland.Pier
{
    /// <summary>
    /// Swaps idle and walk frames from the sliced Nia sheet. Facing is the root scale,
    /// which NiaMovement already sets.
    /// </summary>
    public sealed class NiaVisual : MonoBehaviour
    {
        [SerializeField] Sprite[] idleFrames;
        [SerializeField] Sprite[] walkFrames;
        [SerializeField] float idleFrameRate = 4f;
        [SerializeField] float walkFrameRate = 8f;

        SpriteRenderer _renderer;
        float _lastX;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _lastX = transform.position.x;
            ApplyFrame(idleFrames, 0);
        }

        void LateUpdate()
        {
            var x = transform.position.x;
            var moved = Mathf.Abs(x - _lastX) > 0.0001f;
            _lastX = x;

            var frames = moved && HasFrames(walkFrames) ? walkFrames : idleFrames;
            var rate = moved ? walkFrameRate : idleFrameRate;
            if (!HasFrames(frames) || rate <= 0f)
                return;

            var index = (int)(Time.time * rate) % frames.Length;
            if (index < 0)
                index = 0;
            ApplyFrame(frames, index);
        }

        void ApplyFrame(Sprite[] frames, int index)
        {
            if (_renderer == null || !HasFrames(frames))
                return;
            _renderer.sprite = frames[index];
        }

        static bool HasFrames(Sprite[] frames)
        {
            return frames != null && frames.Length > 0 && frames[0] != null;
        }
    }
}
