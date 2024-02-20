using UnityEngine;

namespace Core
{
    public class BaseMonoBehaviour : MonoBehaviour
    {
        public virtual void Initialize(params object[] list) 
        {
            RegisterEvents();
        }

        public virtual void RegisterEvents() { }

        public virtual void UnregisterEvents() { }

        public virtual void End() 
        {
            UnregisterEvents();
        }
    }
}
