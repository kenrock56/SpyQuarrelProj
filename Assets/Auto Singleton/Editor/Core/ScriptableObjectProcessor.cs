using AutoSingleton;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AutoSingletonEditor
{
    class ScriptableObjectProcessor : AssetProcessor
    {
        protected override string DisplayTitle => "Scriptable Object";

        protected override Type TargetType => typeof(ScriptableObject);

        protected override string DefaultFolder => "Singleton/Scriptable Object";

        protected override string FileExtension => "asset";

        protected override List<ToggleableSingleton<Object>> CatalogueList => SingletonCatalogue.ScriptableObjects;

        protected override Object CreateAndSaveToDisk(Type type, string path, string name)
        {
            ScriptableObject instance = ScriptableObject.CreateInstance(type);
            instance.name = name;

            AssetDatabase.CreateAsset(instance, path);

            return instance;
        }
    }
}
