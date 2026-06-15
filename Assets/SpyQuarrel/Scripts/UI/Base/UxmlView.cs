using System;
using System.Threading.Tasks;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;


public abstract class UxmlView : VisualElement
{
    protected abstract string AssetPath { get; }
    
    protected VisualTreeAsset DisplayAsset { get; private set; }

    public bool IsInitialized { get; private set; }
    public bool IsLoading { get; private set; }

    public VisualElement RootElement => _rootElement;
    protected VisualElement _rootElement;
    
    protected UxmlView()
    {
        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    private async void OnAttachToPanel(AttachToPanelEvent evt)
    {
        await InitializeAsync();
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        OnDispose();
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized || IsLoading)
            return;

        IsLoading = true;

        try
        {
            DisplayAsset = await AssetLoader.Get<VisualTreeAsset>(AssetPath);
            
            if (DisplayAsset == null)
            {
                Debug.LogError($"Failed to load VisualTreeAsset at path: {AssetPath}");
                return;
            }

            Clear();

            TemplateContainer root = DisplayAsset.CloneTree();

            VisualElement rootToAdd = GetBaseElement(root);

            if (rootToAdd != null)
            {
                for (int i = 0; i < root.styleSheets.count; i++)
                    rootToAdd.styleSheets.Add(root.styleSheets[i]);

                Debug.Log($"{this.GetType().Name} Successfully loaded based at {Time.time}");
            }
            else
            {
                Debug.Log("Failed to load based, defaulting to root");
                rootToAdd = root;
            }
            
            _rootElement = rootToAdd;
            
            Add(rootToAdd);

            Debug.Log($"Calling On Initialize for {this.GetType().Name} at {Time.time}");
            OnInitialize();

            IsInitialized = true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected virtual VisualElement GetBaseElement(VisualElement root)
    {
        return null;
    }

    protected virtual void OnInitialize()
    {
    }

    protected virtual void OnDispose()
    {
    }

    protected T GetElement<T>(string name) where T : VisualElement
    {
        var ele = this.Q<T>(name);
        
        if (ele == null)
            Debug.LogError($"Failed to find element: {name}");
        
        return this.Q<T>(name);
    }

    public void Show()
    {
        style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        style.display = DisplayStyle.None;
    }

    public void SetVisible(bool visible)
    {
        style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public virtual void OnElementOpen(){}

}

public abstract class PageView : UxmlView
{
    protected PageView()
    {
        Debug.Log($"{this.GetType().Name} Constructor called at {Time.time}");
        
        style.position = Position.Relative;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;

        style.minWidth = Length.Percent(100);
        style.minHeight = Length.Percent(100);
        
        style.width = Length.Percent(100);
        style.height = Length.Percent(100);

        style.flexGrow = 1f;
        style.flexShrink = 1f;
        
        Debug.Log($"{this.GetType().Name}  Constructor finished at {Time.time}");
    }
}

