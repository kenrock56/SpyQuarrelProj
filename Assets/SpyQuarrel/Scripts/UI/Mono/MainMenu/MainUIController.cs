using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    public class MainUIController : MonoBehaviour
    {
        private UIDocument _document;
        private VisualElement _root;

        private readonly List<MenuPageView> _menuPages = new();
        public readonly Dictionary<MenuPages, MenuPageView> Pages = new();

        private OptionsPageElement _optionsPage;
        private StartMenuElement _startMenu;
        private RoleSelectPageElement _roleSelectPage;

        [SerializeField]private bool _isInitialized;

        private void Awake()
        {
            if (!TryGetComponent(out _document))
            {
                Debug.LogError($"{nameof(MainUIController)} requires a UIDocument component.");
                enabled = false;
                return;
            }

            _root = _document.rootVisualElement;

            if (_root == null)
            {
                Debug.LogError($"{nameof(MainUIController)} could not find rootVisualElement.");
                enabled = false;
            }
        }

        private IEnumerator Start()
        {
            yield return InitializeElements();

            RegisterPages();
            BindPages();

            _isInitialized = true;

            Debug.Log("All menu pages initialized.");

            if (_startMenu != null)
            {
                GoToPage(_startMenu);
            }
        }

        private IEnumerator InitializeElements()
        {
            _startMenu = _root.Q<StartMenuElement>();
            _optionsPage = _root.Q<OptionsPageElement>();
            _roleSelectPage = _root.Q<RoleSelectPageElement>();

            _menuPages.Clear();
            _menuPages.AddRange(_root.Query<MenuPageView>().ToList());

            if (_menuPages.Count == 0)
            {
                Debug.LogWarning($"{nameof(MainUIController)} found no MenuPageView elements.");
                yield break;
            }

            yield return new WaitUntil(AllPagesInitialized);
        }

        private bool AllPagesInitialized()
        {
            for (int i = 0; i < _menuPages.Count; i++)
            {
                if (_menuPages[i] == null || !_menuPages[i].IsInitialized)
                {
                    return false;
                }
            }

            return true;
        }

        private void RegisterPages()
        {
            Pages.Clear();

            foreach (var page in _menuPages)
            {
                if (page == null) continue;

                var definition = page.MenuPageDefinition;

                if (Pages.ContainsKey(definition))
                {
                    continue;
                }

                Debug.Log($"Registering page {page.name} + {page.MenuPageDefinition}");
                Pages.Add(definition, page);
            }
        }

        private void BindPages()
        {
            foreach (var page in _menuPages)
            {
                page?.BindToController(this);
            }
        }

        public void SwitchPage(MenuPages page)
        {
            if (!_isInitialized)
            {
                return;
            }
            
            if (!Pages.TryGetValue(page, out var element) || element is not { IsInitialized: true } )
            {
                return;
            }
            

            GoToPage(element);
        }

        private void GoToPage(MenuPageView targetPage)
        {
            if (targetPage == null)
            {
                return;
            }

            foreach (var page in _menuPages)
            {
                if (page == null)
                {
                    continue;
                }

                if (page == targetPage)
                {
                    page.Show();
                }
                else
                {
                    page.Hide();
                }
            }
        }
    }
}