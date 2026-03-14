using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance;

    public static UnityMainThreadDispatcher Instance()
    {
        return _instance;
    }

    public static UnityMainThreadDispatcher EnsureInstance()
    {
        if (_instance == null)
        {
            GameObject dispatcherObject = new GameObject("UnityMainThreadDispatcher");
            _instance = dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
        }

        return _instance;
    }

    // Unity APIs must run on the main thread.
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        while (true)
        {
            Action action;
            lock (_executionQueue)
            {
                if (_executionQueue.Count == 0)
                {
                    break;
                }

                action = _executionQueue.Dequeue();
            }

            action?.Invoke();
        }
    }
}
