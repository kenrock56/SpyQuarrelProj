using UnityEditor;

namespace AutoSingletonEditor
{
    static class FileIOHelper
    {
        public static void EnsureFolderExists(string relativeFolder)
        {
            if (AssetDatabase.IsValidFolder(relativeFolder) == false)
            {
                string parent = relativeFolder.Substring(0, relativeFolder.LastIndexOf('/'));

                EnsureFolderExists(parent);
                AssetDatabase.CreateFolder(parent, relativeFolder.Substring(relativeFolder.LastIndexOf('/') + 1));
            }
        }
    }
}
