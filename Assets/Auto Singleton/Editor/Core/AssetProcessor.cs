using AutoSingleton;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AutoSingletonEditor
{
    abstract class AssetProcessor
    {
        struct SingletonParameter
        {
            public string folder;
            public string name;

            public SingletonParameter(string folder, string name)
            {
                this.folder = folder;
                this.name = name;
            }
        }

        const string AssetFolderPrefix = "Assets/";

        protected abstract string DisplayTitle { get; }

        protected abstract Type TargetType { get; }

        protected abstract string DefaultFolder { get; }

        protected abstract string FileExtension { get; }

        protected abstract List<ToggleableSingleton<Object>> CatalogueList { get; }

        protected abstract Object CreateAndSaveToDisk(Type type, string path, string name);

        public void Execute()
        {
            ProcessorLogger logger = new ProcessorLogger($"[Auto Singleton] {DisplayTitle}");

            List<Type> parentTypes = new List<Type>();

            FindAndOrderParentTypes(parentTypes);

            Dictionary<Type, SingletonParameter> singletonParameters = new Dictionary<Type, SingletonParameter>();

            FindSingletonParameters(singletonParameters, parentTypes);

            DeleteNullInCatalogueList(logger);

            DeleteExtraFromCatalogueList(singletonParameters, logger);

            CreateAndAddToCatalogueList(singletonParameters, logger);

            SortCatalogueList();

            if (Options.LogChanges)
                logger.Dump();
        }

        void FindAndOrderParentTypes(List<Type> parentTypes)
        {
            foreach (Type type in TypeCache.GetTypesWithAttribute<SingletonAttribute>())
            {
                if (type.IsSubclassOf(TargetType) == false)
                    continue;

                int i;
                for (i = 0; i < parentTypes.Count; i++)
                    if (type.IsSubclassOf(parentTypes[i]))
                        break;

                parentTypes.Insert(i, type);
            }
        }

        void FindSingletonParameters(Dictionary<Type, SingletonParameter> singletonParameters, List<Type> parentTypes)
        {
            Dictionary<Type, SingletonAttribute> parentToAttribute = parentTypes.ToDictionary(t => t, t => t.GetCustomAttribute<SingletonAttribute>());

            foreach (Type parent in parentTypes)
            {
                SingletonAttribute parentAttribute = parentToAttribute[parent];
                singletonParameters.Add(parent, new SingletonParameter(AssetFolderPrefix + (parentAttribute.folderPath ?? DefaultFolder), parentAttribute.displayName ?? parent.Name));
            }
            
            foreach (Type parent in parentTypes)
            {
                SingletonAttribute parentAttribute = parentToAttribute[parent];

                if (parentAttribute.inherited == false)
                    continue;

                foreach (Type derived in TypeCache.GetTypesDerivedFrom(parent))
                    if (singletonParameters.ContainsKey(derived) == false)
                        singletonParameters.Add(derived, new SingletonParameter(AssetFolderPrefix + (parentAttribute.folderPath ?? DefaultFolder), derived.Name));
            }
        }

        void DeleteNullInCatalogueList(ProcessorLogger logger)
        {
            int removedCount = CatalogueList.RemoveAll(ts => ts.value == null);
            
            if (removedCount > 0)
            {
                EditorUtility.SetDirty(SingletonCatalogue.Asset);

                logger.Append($"Removed {removedCount} null entries in {SingletonCatalogue.AssetName}.", LogLevel.Message);
            }
        }

        void DeleteExtraFromCatalogueList(Dictionary<Type, SingletonParameter> singletonParameters, ProcessorLogger logger)
        {
            foreach (ToggleableSingleton<Object> ts in CatalogueList.ToArray())
                if (singletonParameters.ContainsKey(ts.value.GetType()) == false)
                {
                    try
                    {
                        string assetPath = AssetDatabase.GetAssetPath(ts.value);
                        AssetDatabase.DeleteAsset(assetPath);

                        CatalogueList.Remove(ts);

                        EditorUtility.SetDirty(SingletonCatalogue.Asset);

                        logger.Append($"Deleted '{ts.value.name}' asset.", LogLevel.Message);
                    }
                    catch (Exception e)
                    {
                        logger.Append($"Could not delete '{ts.value.name}' asset: {e.Message}", LogLevel.Error);

                        Debug.LogException(e);
                    }
                }
        }

        void CreateAndAddToCatalogueList(Dictionary<Type, SingletonParameter> singletonParameters, ProcessorLogger logger)
        {
            HashSet<Type> alreadyExisting = new HashSet<Type>(CatalogueList.Select(ts => ts.value.GetType()));

            foreach (KeyValuePair<Type, SingletonParameter> kvPair in singletonParameters)
            {
                if (IsInstantiable(kvPair.Key) == false || alreadyExisting.Contains(kvPair.Key))
                    continue;

                try
                {
                    FileIOHelper.EnsureFolderExists(kvPair.Value.folder);

                    string path = Path.Combine(kvPair.Value.folder, $"{kvPair.Value.name}.{FileExtension}").Replace('\\', '/');

                    Object obj = CreateAndSaveToDisk(kvPair.Key, path, kvPair.Value.name);

                    CatalogueList.Add(new ToggleableSingleton<Object>(obj));

                    EditorUtility.SetDirty(SingletonCatalogue.Asset);
                    EditorGUIUtility.PingObject(obj);

                    logger.Append($"Created '{obj.name}' asset.", LogLevel.Message);
                }
                catch (Exception e)
                {
                    logger.Append($"Could not create '{kvPair.Value.name}' asset: {e.Message}", LogLevel.Error);

                    Debug.LogException(e);
                }
            }
        }

        void SortCatalogueList()
        {
            List<ToggleableSingleton<Object>> list = CatalogueList;
            List<ToggleableSingleton<Object>> original = new List<ToggleableSingleton<Object>>(list);
            list.Sort((a, b) => string.Compare(
                AssetDatabase.GetAssetPath(a.value),
                AssetDatabase.GetAssetPath(b.value),
                StringComparison.Ordinal));

            for (int i = 0; i < list.Count; i++)
                if (list[i].value != original[i].value)
                {
                    EditorUtility.SetDirty(SingletonCatalogue.Asset);
                    return;
                }
        }

        static bool IsInstantiable(Type type) => (type.IsAbstract == false && type.IsGenericTypeDefinition == false);
    }
}
