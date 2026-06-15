using AutoSingleton;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AutoSingletonEditor
{
    [InitializeOnLoad]
    static class ProcessorsManager
    {
        const string CatalogueFolder = "Assets/Resources";

        static readonly IReadOnlyCollection<AssetProcessor> AssetsProcessors = new AssetProcessor[]
        {
            new ScriptableObjectProcessor(),
            new MonoBehaviourProcessor(),
        };

        static ProcessorsManager()
        {
            if (Options.AutomaticRefresh && CompilationTracker.HasAnyCompilationError == false)
                EditorApplication.delayCall += ExecuteAllProcessors;
        }

        public static void ExecuteAllProcessors()
        {
            EnsureCatalogueExists();

            foreach (AssetProcessor processor in AssetsProcessors)
                processor.Execute();

            AssetDatabase.SaveAssetIfDirty(SingletonCatalogue.Asset);
            AssetDatabase.Refresh();

            IconDisplay.RefreshSingletonList();
        }

        internal static void EnsureCatalogueExists()
        {
            if (SingletonCatalogue.Asset != null)
                return;

            FileIOHelper.EnsureFolderExists(CatalogueFolder);

            string path = CatalogueFolder + "/" + SingletonCatalogue.AssetName + ".asset";
            SingletonCatalogue catalogue = ScriptableObject.CreateInstance<SingletonCatalogue>();
            AssetDatabase.CreateAsset(catalogue, path);

            Debug.Log($"[Auto Singleton] Created '{SingletonCatalogue.AssetName}' asset at '{path}'.");
        }
    }
}
