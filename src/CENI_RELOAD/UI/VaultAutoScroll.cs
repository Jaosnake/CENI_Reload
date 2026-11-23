using RoR2;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jaosnake.CeniReload.UI
{
    internal class VaultAutoScroll : MonoBehaviour, ISelectHandler
    {
        public ScrollRect scrollRect;
        public RectTransform targetRect;
        public float viewportPadding = 8f;
        public float smoothSpeed = 14f;

        private bool _isScrolling;
        private float _targetNormalizedPos;
        private readonly Vector3[] _cardCorners = new Vector3[4];

        public void Initialize(ScrollRect scroll, RectTransform rect)
        {
            scrollRect = scroll;
            targetRect = rect;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (scrollRect == null || scrollRect.content == null) return;

            var viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
            var target = targetRect != null ? targetRect : GetComponent<RectTransform>();
            if (viewport == null || target == null) return;

            Canvas.ForceUpdateCanvases();

            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = viewport.rect.height;
            float scrollableHeight = contentHeight - viewportHeight;
            if (scrollableHeight <= 1f)
            {
                _isScrolling = false;
                return;
            }

            target.GetWorldCorners(_cardCorners);
            // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
            float cardBottom = viewport.InverseTransformPoint(_cardCorners[0]).y;
            float cardTop = viewport.InverseTransformPoint(_cardCorners[1]).y;
            float viewBottom = viewport.rect.yMin;
            float viewTop = viewport.rect.yMax;

            float currentNorm = scrollRect.verticalNormalizedPosition;
            if (cardTop > viewTop - viewportPadding)
            {
                float delta = (cardTop + viewportPadding) - viewTop;
                _targetNormalizedPos = Mathf.Clamp01(currentNorm + (delta / scrollableHeight));
                _isScrolling = true;
            }
            else if (cardBottom < viewBottom + viewportPadding)
            {
                float delta = (viewBottom + viewportPadding) - cardBottom;
                _targetNormalizedPos = Mathf.Clamp01(currentNorm - (delta / scrollableHeight));
                _isScrolling = true;
            }
            else
            {
                _isScrolling = false;
            }
        }

        private void Update()
        {
            if (!_isScrolling || scrollRect == null) return;

            EventSystem es = MPEventSystemManager.primaryEventSystem ?? EventSystem.current;
            if (es != null && es.currentSelectedGameObject != gameObject)
            {
                _isScrolling = false;
                return;
            }

            float current = scrollRect.verticalNormalizedPosition;
            float next = Mathf.Lerp(current, _targetNormalizedPos, Mathf.Clamp01(Time.unscaledDeltaTime * smoothSpeed));
            if (Mathf.Abs(next - _targetNormalizedPos) < 0.001f)
            {
                scrollRect.verticalNormalizedPosition = _targetNormalizedPos;
                _isScrolling = false;
            }
            else
            {
                scrollRect.verticalNormalizedPosition = next;
            }
        }

        private void OnDisable()
        {
            _isScrolling = false;
        }
    }
}
