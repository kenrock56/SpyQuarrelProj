using UnityEngine;

namespace SpyQuarrelRuntime
{
    public interface IBindablePageElement
    {
        public MainUIController MainUIController { get; }

        void BindToController(MainUIController controller);
    }
}
