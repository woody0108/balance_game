using System.Collections.Generic;
using UnityEngine;

public class UIObjectPool<T> where T : Component
{
    private readonly Queue<T> pool = new();
    private readonly T prefab;
    private readonly Transform parent;

    public UIObjectPool(T prefab, Transform parent, int preload = 10)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < preload; i++)
            Release(Create());
    }

    private T Create()
    {
        return Object.Instantiate(prefab, parent);
    }

    public T Get()
    {
        var item = pool.Count > 0 ? pool.Dequeue() : Create();
        item.gameObject.SetActive(true);
        return item;
    }

    public void Release(T item)
    {
        item.gameObject.SetActive(false);
        pool.Enqueue(item);
    }

    public void ReleaseAll(IEnumerable<T> items)
    {
        foreach (var item in items)
            Release(item);
    }
}
