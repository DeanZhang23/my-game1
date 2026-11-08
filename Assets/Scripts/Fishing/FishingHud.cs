using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NiaIsland.Fishing
{
    public sealed class FishingHud
    {
        readonly Text _state;
        readonly Text _bite;
        readonly Image _biteImage;
        readonly Text _buttonLabel;
        readonly Image _buttonImage;
        readonly Image _flash;
        readonly Sprite _castSprite;
        readonly Sprite _hookSprite;
        readonly bool _useArt;

        public FishingHud(FishingController controller, Sprite castSprite, Sprite hookSprite, Sprite biteCueSprite)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                Debug.LogError("Fishing HUD could not find a built-in font. State text will be blank.");

            var canvasObject = new GameObject(
                "FishingHUD",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _castSprite = castSprite;
            _hookSprite = hookSprite;
            _useArt = castSprite != null && hookSprite != null;

            if (!EventSystemExists())
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            _flash = CreateImage(canvasObject.transform, "BiteFlash", new Color(0.42f, 1f, 0.6f, 0f));
            Stretch(_flash.rectTransform);

            _state = CreateText(
                canvasObject.transform,
                font,
                "StateLabel",
                34,
                TextAnchor.UpperLeft,
                new Color(0.97f, 0.98f, 0.99f, 1f));
            PlaceTopLeft(_state.rectTransform, 28f, -24f, 900f, 52f);

            var hint = CreateText(
                canvasObject.transform,
                font,
                "Hint",
                24,
                TextAnchor.UpperLeft,
                new Color(0.9f, 0.94f, 0.96f, 0.95f));
            PlaceTopLeft(hint.rectTransform, 28f, -78f, 1200f, 40f);
            hint.text = "Move with A/D or Left/Right. Cast, then Hook when the green cue appears.";

            _biteImage = CreateImage(canvasObject.transform, "BiteCueArt", Color.white);
            var biteArtRect = _biteImage.rectTransform;
            biteArtRect.anchorMin = new Vector2(0.5f, 0.5f);
            biteArtRect.anchorMax = new Vector2(0.5f, 0.5f);
            biteArtRect.pivot = new Vector2(0.5f, 0.5f);
            biteArtRect.anchoredPosition = new Vector2(0f, 170f);
            biteArtRect.sizeDelta = new Vector2(280f, 280f);
            _biteImage.sprite = biteCueSprite;
            _biteImage.preserveAspect = true;
            _biteImage.gameObject.SetActive(false);

            _bite = CreateText(
                canvasObject.transform,
                font,
                "BiteCue",
                96,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.93f, 0.2f, 1f));
            var biteRect = _bite.rectTransform;
            biteRect.anchorMin = new Vector2(0.5f, 0.5f);
            biteRect.anchorMax = new Vector2(0.5f, 0.5f);
            biteRect.pivot = new Vector2(0.5f, 0.5f);
            biteRect.anchoredPosition = new Vector2(0f, 150f);
            biteRect.sizeDelta = new Vector2(900f, 160f);
            _bite.text = "BITE!";
            _bite.gameObject.SetActive(false);

            var buttonObject = new GameObject("FishButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 36f);
            buttonRect.sizeDelta = _useArt ? new Vector2(340f, 290f) : new Vector2(360f, 110f);

            _buttonImage = buttonObject.GetComponent<Image>();
            _buttonImage.sprite = _useArt ? castSprite : null;
            _buttonImage.preserveAspect = true;
            _buttonImage.color = _useArt ? Color.white : ReadyColor;
            _buttonImage.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = _buttonImage;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(controller.OnFishButtonPressed);

            _buttonLabel = CreateText(
                buttonObject.transform,
                font,
                "Label",
                42,
                TextAnchor.MiddleCenter,
                Color.white);
            Stretch(_buttonLabel.rectTransform);
            _buttonLabel.text = "Cast";
            if (_useArt)
                _buttonLabel.gameObject.SetActive(false);
        }

        public void Refresh(FishingState state, float bitePulse)
        {
            _state.text = "State: " + state;

            var biting = state == FishingState.BiteWindow;
            var showTextCue = biting && !_useArt;
            if (_bite.gameObject.activeSelf != showTextCue)
                _bite.gameObject.SetActive(showTextCue);
            if (_biteImage != null && _biteImage.sprite != null && _biteImage.gameObject.activeSelf != biting)
                _biteImage.gameObject.SetActive(biting);
            if (biting)
            {
                var scale = 1f + 0.12f * bitePulse;
                if (showTextCue)
                    _bite.rectTransform.localScale = new Vector3(scale, scale, 1f);
                if (_biteImage != null && _biteImage.sprite != null)
                    _biteImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            var flash = _flash.color;
            flash.a = biting ? 0.08f + 0.2f * bitePulse : 0f;
            _flash.color = flash;

            if (_useArt)
            {
                _buttonImage.sprite = biting ? _hookSprite : _castSprite;
                _buttonImage.color = state == FishingState.Ready || biting
                    ? Color.white
                    : new Color(0.62f, 0.62f, 0.62f, 1f);
            }
            else
            {
                _buttonLabel.text = ButtonLabel(state);
                _buttonImage.color = ButtonColor(state);
            }
        }

        static string ButtonLabel(FishingState state)
        {
            switch (state)
            {
                case FishingState.Ready:
                    return "Cast";
                case FishingState.Cast:
                    return "Casting";
                case FishingState.Waiting:
                    return "Waiting";
                case FishingState.BiteWindow:
                    return "Hook";
                case FishingState.Miss:
                    return "Miss";
                case FishingState.Resolved:
                    return "Resolved";
                default:
                    return "Cast";
            }
        }

        static Color ButtonColor(FishingState state)
        {
            switch (state)
            {
                case FishingState.BiteWindow:
                    return new Color(0.96f, 0.46f, 0.08f, 1f);
                case FishingState.Ready:
                    return ReadyColor;
                default:
                    return new Color(0.24f, 0.3f, 0.36f, 1f);
            }
        }

        static readonly Color ReadyColor = new Color(0.12f, 0.45f, 0.72f, 1f);

        static bool EventSystemExists()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<EventSystem>() != null;
#else
            return Object.FindObjectOfType<EventSystem>() != null;
#endif
        }

        static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text CreateText(Transform parent, Font font, string name, int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = false;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }
    }
}
