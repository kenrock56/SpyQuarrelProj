using System;

namespace AutoSingleton
{
    [Serializable]
    struct ToggleableSingleton<T> where T: class
    {
        public bool enabled;
        public T value;

        public ToggleableSingleton(T value)
        {
            enabled = true;
            this.value = value;
        }
    }
}
