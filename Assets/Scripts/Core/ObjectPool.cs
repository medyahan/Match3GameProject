using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class ObjectPool<T> where T : Component
    {
        private Queue<T> _pool = new Queue<T>();
        private Transform _poolParent; // Parent transform to hold pooled objects
        private T _prefab; // The prefab to create new objects

        public ObjectPool(Transform poolParent, T prefab, int initialSize)
        {
            _poolParent = poolParent;
            _prefab = prefab;

            // Instantiate and populate the pool with initialSize objects
            for (int i = 0; i < initialSize; i++)
            {
                CreateObject();
            }
        }

        private T CreateObject()
        {
            T newObj = GameObject.Instantiate(_prefab, _poolParent);
            newObj.gameObject.SetActive(false);
            _pool.Enqueue(newObj);
            return newObj;
        }

        public T GetObject(Vector3 position, Quaternion rotation)
        {
            if (_pool.Count == 0)
            {
                // If the pool is empty, create a new object and add it to the pool
                return CreateObject();
            }

            T obj = _pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void ReturnObject(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    
        public void ClearPool()
        {
            _pool.Clear();
        }
    }
}