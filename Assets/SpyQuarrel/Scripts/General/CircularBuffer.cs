using System;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    /// <summary>
    /// This class allows to add values without thinking about the index or resizing the array;
    /// adding over the size will wrap around back to the beginning
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class CircularBuffer<T> 
    {
        [SerializeField]private T[] _buffer;
        [SerializeField]private int _bufferSize;

        public CircularBuffer(int bufferSize)
        {
            _bufferSize = bufferSize;
            _buffer = new T[_bufferSize];
        }

        public void Add(T item, int index)
        {
            _buffer[index % _bufferSize] = item;
        }

        public T Get(int index)
        {
            return _buffer[index % _bufferSize];
        }

        public void Clear()
        {
           _buffer = new T[_bufferSize];
        }

        public T this[int index]
        {
            get => _buffer[index % _bufferSize];
            set => _buffer[index % _bufferSize] = value;
        }
    }
}
