using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    public class RenderScopeElement
    {
        private VisualElement _rootElement;
        private VisualElement _scopeRoot;
        private VisualElement _scopeElement;
        private VisualElement _scopeOverlay;

        private Color _scopeRootColor;

        public RenderScopeElement(VisualElement rootElement)
        {
            if (rootElement == null)
            {
                Debug.LogError("Null root element");
                return;
            }

            _rootElement = rootElement;
            Initialise();
        }

        private void Initialise()
        {
            Debug.Log("Initialising render scope element");

            if (_rootElement == null) return;

            _scopeRoot = _rootElement.Q<VisualElement>("scope-root");
            DebugElement(_scopeRoot, "scope-root");

            _scopeElement = _rootElement.Q<VisualElement>("scope");
            DebugElement(_scopeElement, "scope");

            _scopeOverlay = _rootElement.Q<VisualElement>("scope-overlay");
            DebugElement(_scopeOverlay, "scope-overlay");

            if (_scopeRoot != null)
            {
                _scopeRootColor = _scopeRoot.resolvedStyle.backgroundColor;
            }

            HideScope();
        }

        private void DebugElement(VisualElement element, string elementName)
        {
            if (element == null)
            {
                Debug.LogError($"Null element: {elementName}");
            }
            else
            {
                Debug.Log($"Found Element: ({element.name})");
            }
        }

        public void ShowScope()
        {
            if (_scopeRoot == null) return;
            FadeScopeIn(0.1f);
            
            _scopeElement.style.display = DisplayStyle.Flex;
            _scopeOverlay.style.display = DisplayStyle.Flex;
        }

        private async void FadeScopeIn(float duration = 0.15f)
        {
            if (_scopeRoot == null) return;

            Color endColor = _scopeRootColor;
            Color startColor = endColor;
            startColor.a = 0f;

            _scopeRoot.style.backgroundColor = startColor;

            _scopeRoot.style.display = DisplayStyle.Flex;
            
            float timeElapsed = 0f;

            while (timeElapsed < duration)
            {
                timeElapsed += Time.deltaTime;

                float t = Mathf.Clamp01(timeElapsed / duration);
                _scopeRoot.style.backgroundColor = Color.Lerp(startColor, endColor, t);

                await Awaitable.EndOfFrameAsync();
            }

            _scopeRoot.style.backgroundColor = endColor;
        }
        
        private async void FadeScopeOut(float duration = 0.15f)
        {
            if (_scopeRoot == null) return;

            Color startColor = _scopeRoot.style.backgroundColor.value;
            Color endColor = startColor;
            endColor.a = 0f;

            float timeElapsed = 0f;

            while (timeElapsed < duration)
            {
                timeElapsed += Time.deltaTime;

                float t = Mathf.Clamp01(timeElapsed / duration);
                _scopeRoot.style.backgroundColor = Color.Lerp(startColor, endColor, t);

                await Awaitable.EndOfFrameAsync();
            }

            _scopeRoot.style.backgroundColor = endColor;
            _scopeRoot.style.display = DisplayStyle.None;
        }

        public void HideScope()
        {
            if (_scopeRoot == null) return;

            _scopeElement.style.display = DisplayStyle.None;
            _scopeOverlay.style.display = DisplayStyle.None;
            FadeScopeOut();
            
        }
    }
}