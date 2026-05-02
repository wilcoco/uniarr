using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// Firebase 콜백 등 비-메인 스레드에서 Unity API 호출이 필요할 때 사용
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        public static MainThreadDispatcher Instance { get; private set; }

        private readonly Queue<Action> queue = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            lock (queue)
            {
                while (queue.Count > 0)
                    queue.Dequeue()?.Invoke();
            }
        }

        public static void Enqueue(Action action)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[MainThreadDispatcher] Instance not found");
                return;
            }
            lock (Instance.queue)
                Instance.queue.Enqueue(action);
        }
    }
}
