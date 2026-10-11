using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
    private static MainThreadDispatcher instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        queue.Enqueue(action);
    }

    private void Update()
    {
        while (queue.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("MainThreadDispatcher");
                instance = go.AddComponent<MainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
}
