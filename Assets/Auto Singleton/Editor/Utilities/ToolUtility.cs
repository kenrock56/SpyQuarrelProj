using System.Runtime.CompilerServices;

namespace AutoSingletonEditor
{
    static class ToolUtility
    {
        const string ThisScriptPathFromToolRoot = "Editor/Utilities/ToolUtility.cs";

        const string ProjectRootFolder = "Assets";

        static string _absolutePath;
        static string _relativePath;

        public static string AbsolutePath
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (_absolutePath == null)
                    SetPaths();

                return _absolutePath;
            }
        }

        public static string RelativePath
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (_relativePath == null)
                    SetPaths();

                return _relativePath;
            }
        }

        static void SetPaths([CallerFilePath] string callerFilePath = "")
        {
            _absolutePath = callerFilePath.Remove(callerFilePath.Length - (ThisScriptPathFromToolRoot.Length + 1));

            _relativePath = _absolutePath.Substring(_absolutePath.LastIndexOf(ProjectRootFolder))
                                         .Replace('\\', '/');
        }
    }
}
