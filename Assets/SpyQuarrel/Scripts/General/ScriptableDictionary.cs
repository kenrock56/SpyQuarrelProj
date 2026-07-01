using System;
using System.Collections.Generic;
using UnityEngine;


public abstract class ScriptableDictionary<TKey, TValue> : ScriptableObject where TKey : Enum
{
    [SerializeField] private GenericEntry<TKey, TValue>[] _entries;
    
    private Dictionary<TKey, TValue> _dictionary;

    protected virtual void OnEnable()
    {
        BuildDictionary();
    }

    public void BuildDictionary()
    {
        _dictionary = new Dictionary<TKey, TValue>();

        if (_entries == null)
            return;

        foreach (var entry in _entries)
        {
            if (_dictionary.ContainsKey(entry.Key))
            {
                Debug.LogWarning(
                    $"Duplicate key '{entry.Key}' found in {name}. Keeping first value.",
                    this
                );
                continue;
            }

            _dictionary.Add(entry.Key, entry.Value);
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_dictionary == null)
            BuildDictionary();

        return _dictionary.TryGetValue(key, out value);
    }

    public TValue GetValue(TKey key)
    {
        if (_dictionary == null)
            BuildDictionary();

        if (_dictionary.TryGetValue(key, out var value))
            return value;

        throw new KeyNotFoundException($"Key '{key}' was not found in {name}.");
    }

    public bool ContainsKey(TKey key)
    {
        if (_dictionary == null)
            BuildDictionary();

        return _dictionary.ContainsKey(key);
    }

    public TValue this[TKey key] => GetValue(key);

    public IReadOnlyDictionary<TKey, TValue> Dictionary
    {
        get
        {
            if (_dictionary == null)
                BuildDictionary();

            return _dictionary;
        }
    }
}