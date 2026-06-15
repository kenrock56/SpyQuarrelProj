using UnityEngine;

namespace SpyQuarrelRuntime
{
    public static class CursorManager
    {
        public static void SetCursor(bool show)
        {
            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
