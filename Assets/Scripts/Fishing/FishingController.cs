using NiaIsland.Pier;
using UnityEngine;

namespace NiaIsland.Fishing
{
    public sealed class FishingController : MonoBehaviour
    {
        const float BobberDepth = -1.15f;
        const float MinimumDuration = 0.05f;

        [Header("Timing (seconds)")]
        [Tooltip("How long the cast flight lasts before Waiting.")]
        [Range(0.2f, 2f)]
        [SerializeField] float castDuration = 0.45f;

        [Tooltip("How long Waiting lasts before the bite cue.")]
        [Range(0.3f, 3f)]
        [SerializeField] float waitingDuration = 1.15f;

        [Tooltip("How long the hook can land. Slice 1 target is about 0.8 to 1.2 seconds.")]
        [Range(0.3f, 3f)]
        [SerializeField] float biteWindowDuration = 1f;

        [Tooltip("How long Miss or Resolved stays on screen before Ready.")]
        [Range(0.2f, 2f)]
        [SerializeField] float resultHoldDuration = 0.7f;

        [Header("Scene references")]
        [SerializeField] NiaMovement nia;
        [SerializeField] SpriteRenderer bobber;
        [SerializeField] Transform fishingLine;

        [Header("Art")]
        [SerializeField] Sprite castButtonSprite;
        [SerializeField] Sprite hookButtonSprite;
        [SerializeField] Sprite biteCueSprite;

        [Header("Runtime")]
        [SerializeField] FishingState currentState;

        FishingTiming _timing;
        FishingStateMachine _machine;
        FishingHud _hud;
        Vector3 _castTarget;
        static readonly Color BobberOrange = new Color(0.83f, 0.72f, 0.45f, 1f);

        void Awake()
        {
            ResolveReferences();
            _timing = new FishingTiming(castDuration, waitingDuration, biteWindowDuration, resultHoldDuration);
            _machine = new FishingStateMachine(_timing);
            _machine.Entered += HandleEntered;
            currentState = _machine.State;
        }

        void Start()
        {
            _hud = new FishingHud(this, castButtonSprite, hookButtonSprite, biteCueSprite);
            _hud.Refresh(_machine.State, 0f);
            UpdateVisuals();
        }

        public void OnFishButtonPressed()
        {
            if (_machine == null)
                return;
            _machine.PressFishButton();
            Publish();
        }

        void LateUpdate()
        {
            if (_machine == null)
                return;

            _timing.CastDuration = Mathf.Max(MinimumDuration, castDuration);
            _timing.WaitingDuration = Mathf.Max(MinimumDuration, waitingDuration);
            _timing.BiteWindowDuration = Mathf.Max(MinimumDuration, biteWindowDuration);
            _timing.ResultHoldDuration = Mathf.Max(MinimumDuration, resultHoldDuration);
            _machine.Tick(Time.deltaTime);
            Publish();
        }

        void HandleEntered(FishingState state)
        {
            if (state == FishingState.Cast && nia != null)
                _castTarget = new Vector3(nia.transform.position.x, BobberDepth, 0f);

            if (state == FishingState.Resolved)
                Debug.Log("Resolved");
            else if (state == FishingState.Miss)
                Debug.Log("Miss");
        }

        void Publish()
        {
            currentState = _machine.State;
            UpdateVisuals();
            if (_hud != null)
                _hud.Refresh(_machine.State, BitePulse());
        }

        float BitePulse()
        {
            if (_machine.State != FishingState.BiteWindow)
                return 0f;
            return Mathf.PingPong(Time.time * 4f, 1f);
        }

        void UpdateVisuals()
        {
            Vector3 bobberPosition;
            var show = TryGetBobberPosition(out bobberPosition);
            if (bobber != null)
            {
                if (bobber.gameObject.activeSelf != show)
                    bobber.gameObject.SetActive(show);
                if (show)
                {
                    bobber.transform.position = bobberPosition;
                    bobber.color = BobberColor();
                }
            }

            if (fishingLine != null)
            {
                var showLine = show && nia != null;
                if (fishingLine.gameObject.activeSelf != showLine)
                    fishingLine.gameObject.SetActive(showLine);
                if (showLine)
                    PlaceLine(HandPosition(), bobberPosition);
            }
        }

        bool TryGetBobberPosition(out Vector3 position)
        {
            position = _castTarget;
            if (nia == null)
                return false;

            var hand = HandPosition();
            switch (_machine.State)
            {
                case FishingState.Cast:
                    position = Arc(hand, _castTarget, _machine.NormalizedTime);
                    return true;
                case FishingState.Waiting:
                    position = _castTarget + new Vector3(0f, Mathf.Sin(Time.time * 5f) * 0.07f, 0f);
                    return true;
                case FishingState.BiteWindow:
                    var pull = 0.2f + 0.22f * Mathf.PingPong(Time.time * 9f, 1f);
                    position = Vector3.Lerp(_castTarget, hand, pull);
                    return true;
                case FishingState.Miss:
                    position = _castTarget + new Vector3(0f, -0.85f * _machine.NormalizedTime, 0f);
                    return true;
                case FishingState.Resolved:
                    position = _castTarget + new Vector3(0f, 0.4f * _machine.NormalizedTime, 0f);
                    return true;
                default:
                    return false;
            }
        }

        Color BobberColor()
        {
            switch (_machine.State)
            {
                case FishingState.BiteWindow:
                    return Color.Lerp(BobberOrange, new Color(1f, 0.95f, 0.4f, 1f), BitePulse());
                case FishingState.Resolved:
                    return new Color(0.45f, 0.85f, 0.4f, 1f);
                case FishingState.Miss:
                    var faded = BobberOrange;
                    faded.a = 1f - _machine.NormalizedTime;
                    return faded;
                default:
                    return BobberOrange;
            }
        }

        Vector3 HandPosition()
        {
            var facing = nia.transform.localScale.x < 0f ? -1f : 1f;
            return nia.transform.position + new Vector3(0.45f * facing, 1.15f, 0f);
        }

        static Vector3 Arc(Vector3 from, Vector3 to, float t)
        {
            var position = Vector3.Lerp(from, to, t);
            position.y += 1.35f * 4f * t * (1f - t);
            return position;
        }

        void PlaceLine(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            var length = delta.magnitude;
            if (length < 0.001f)
            {
                fishingLine.gameObject.SetActive(false);
                return;
            }

            fishingLine.position = (from + to) * 0.5f;
            fishingLine.right = delta;
            fishingLine.localScale = new Vector3(length, 0.06f, 1f);
        }

        void ResolveReferences()
        {
            if (nia == null)
                nia = FindOne<NiaMovement>();

            if (bobber != null && fishingLine != null)
                return;

            var transforms = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var child = transforms[i];
                if (bobber == null && child.name == "Bobber")
                    bobber = child.GetComponent<SpriteRenderer>();
                if (fishingLine == null && child.name == "Line")
                    fishingLine = child;
            }
        }

        static T FindOne<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }
    }
}
