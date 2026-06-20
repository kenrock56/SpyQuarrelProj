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
        
        private List<PageView> _menuPages;
        
        void Awake()
        {
            if(!TryGetComponent(out _document))return;
            
            _root = _document.rootVisualElement;
            
        }

        IEnumerator Start()
        {
            
            yield return new WaitForSeconds(1.2f * Time.deltaTime);
            _menuPages = _root.Query<PageView>().ToList();

            foreach (var page in _menuPages)
            {
                Debug.Log(page.name);
            }
        }
    }
}
