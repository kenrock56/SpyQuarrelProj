using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace AutoSingletonEditor
{
    static class StaticDataSerializer
    {
        const bool DebugLogSerializedFields         = false;

        static readonly string SaveFolder           = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "Auto Singleton");
        const string SaveExtension                  = "tmp";

        const BindingFlags FieldsBindingFlags       = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static void WarningSerializationFail(string className, string filePath, string msg)         => Debug.LogWarning($"[Auto Singleton] Couldn't serialize '{className}' into '{filePath}': {msg}.");
        static void WarningDeserializationFail(string className, string filePath, string msg)       => Debug.LogWarning($"[Auto Singleton] Couldn't deserialize '{className}' from '{filePath}': {msg}.");

        static void LogSerialization(string className, string fieldName)                            => Debug.Log($"[Auto Singleton] Serializing '{className}.{fieldName}'.");

        static List<(Type classType, string filePath)> _toSerialize = new List<(Type, string)>();

        static StaticDataSerializer()
        {
            if (Directory.Exists(SaveFolder) == false)
            {
                Directory.CreateDirectory(SaveFolder);
            }

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            EditorApplication.quitting += OnEditorApplicationQuitting;
        }

        /// <summary>
        /// <para> Call this method in a <see langword="class"/> <see langword="static"/> constructor to serialize its <see langword="static"/> fields.                                             </para>
        /// <para> The <see langword="class"/> should be marked with the <see cref="InitializeOnLoadAttribute"/> to ensure save files are deleted.                                                  </para>
        /// <para> By default, all fields will be serialized, including backing fields of auto property and <see langword="event"/>. Use the <see cref="NonSerializedAttribute"/> to exclude one.   </para>
        /// </summary>
        /// <param name="classType"> <see cref="Type"/> of the class we will serialize, usually <see langword="typeof"/>('class name').                                                             </param>
        /// <param name="fileName"> Name of the save file, you may want to NOT use <see langword="nameof"/>('class name') so that the <see langword="class"/> can be renamed without losing data.  </param>
        public static void MaintainDataBetweenAssemblyReload(Type classType, string fileName)
        {
            string filePath = Path.Combine(SaveFolder, $"{fileName}.{SaveExtension}");

            if (File.Exists(filePath))
            {
                DeserializeClass(classType, filePath);

                File.Delete(filePath);
            }

            if (_toSerialize.Any(ts => ts.filePath == filePath))
            {
                throw new InvalidOperationException($"Two classes are trying to serialize their data in the file: '{filePath}'.");
            }
            _toSerialize.Add((classType, filePath));
        }

        static void OnBeforeAssemblyReload()
        {
            foreach ((Type classType, string filePath) in _toSerialize)
            {
                SerializeClass(classType, filePath);
            }
        }

        static void SerializeClass(Type classType, string filePath)
        {
            try
            {
                FieldInfo[] fields = GetSerializableFields(classType);
                object[,] serialized = new object[fields.Length, 2];

                for (int i = 0; i < fields.Length; i++)
                {
#pragma warning disable CS0162
                    if (DebugLogSerializedFields)
                    {
                        LogSerialization(classType.Name, fields[i].Name);
                    }
#pragma warning restore CS0162

                    serialized[i, 0] = fields[i].Name;
                    serialized[i, 1] = fields[i].GetValue(null);
                }

                WriteFileFromObjectArray(filePath, serialized);
            }
            catch (Exception e)
            {
                WarningSerializationFail(classType.Name, filePath, e.Message);
            }
        }

        static void DeserializeClass(Type classType, string filePath)
        {
            try
            {
                object[,] serialized = ReadFileToObjectArray(filePath);
                FieldInfo[] fields = GetSerializableFields(classType);

                if (serialized.GetLength(0) != fields.Length)
                {
                    WarningDeserializationFail(classType.Name, filePath, "Saved data didn't have the same count of fields as the serialized class");
                    return;
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].Name != serialized[i, 0] as string)
                    {
                        WarningDeserializationFail(classType.Name, filePath, "A saved field's name didn't match the class one.");
                        return;
                    }
                    else
                    {
                        fields[i].SetValue(null, serialized[i, 1]);
                    }
                }
            }
            catch (Exception e)
            {
                WarningDeserializationFail(classType.Name, filePath, e.Message);
                return;
            }
        }

        static void WriteFileFromObjectArray(string filePath, object[,] serialized)
        {
            using (FileStream stream = File.Open(filePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, serialized);
            }
        }

        static object[,] ReadFileToObjectArray(string filePath)
        {
            using (FileStream stream = File.Open(filePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(stream) as object[,];
            }
        }

        static FieldInfo[] GetSerializableFields(Type classType)
        {
            FieldInfo[] fields = classType.GetFields(FieldsBindingFlags);

            return fields.Where(f => f.FieldType.IsSerializable)    // accept only serializable fields (class marked as Serializable or primitive types)
                         .Where(f => f.IsNotSerialized == false)    // ignore fields with the NonSerialized attribute
                         .Where(f => f.IsLiteral == false)          // ignore const
                         .Where(f => f.IsInitOnly == false)         // ignore readonly
                         .ToArray();
        }

        static void OnEditorApplicationQuitting()
        {
            foreach (FileInfo file in new DirectoryInfo(SaveFolder).EnumerateFiles())
            {
                file.Delete();
            }
        }
    }
}