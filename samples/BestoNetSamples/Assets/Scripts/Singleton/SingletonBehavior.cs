using UnityEngine;

namespace BestoNetSamples.Singleton
{
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        public static T Instance { get; private set; }

        [SerializeField] private bool dontDestroyOnLoad = true;
        
        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = (T)this;
                if (dontDestroyOnLoad)
                {
                    transform.parent = null; // Detach from parent if any
                    DontDestroyOnLoad(gameObject);
                }
                OnAwake();
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Multiple instances of {typeof(T)} detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnAwake() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}