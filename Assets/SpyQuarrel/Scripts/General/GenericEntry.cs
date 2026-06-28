using System;
using UnityEngine;

[Serializable]
public class GenericEntryBase<TKey, TValue> 
{
    public TKey Key;
    public TValue Value;

    public GenericEntryBase() { }

    public GenericEntryBase(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
    
    public TValue this[TKey key] => Value;
}

[System.Serializable]
public class GenericEntry<TKey, TValue> : GenericEntryBase<TKey, TValue> where TKey : Enum
{ 
    public GenericEntry(TKey key, TValue value) : base(key, value) { }
}

