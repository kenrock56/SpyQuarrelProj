using UnityEngine;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    [UxmlElement]
    public partial class OptionCardElement : UxmlView
    {
        protected override string AssetPath => "OptionCard";

        protected override VisualElement GetBaseElement(VisualElement root)
        {
            return root.Q("option-card-button");
        }
    }
}
