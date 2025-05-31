using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher instance;
        private static readonly Queue<Action> executionQueue = new Queue<Action>();
        private static readonly object queueLock = new object();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance == null)
            {
                var obj = new GameObject("UnityMainThreadDispatcher");
                instance = obj.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
        }

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (instance == null)
                {
                    Initialize();
                }
                return instance;
            }
        }

        public void Enqueue(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("Attempted to enqueue a null action");
                return;
            }

            lock (queueLock)
            {
                executionQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (queueLock)
            {
                while (executionQueue.Count > 0)
                {
                    try
                    {
                        var action = executionQueue.Dequeue();
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error executing action on main thread: {ex}");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}