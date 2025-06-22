namespace sky_quant_task;

using System;
using System.Collections;
using System.Collections.Generic;

public class RemovableQueue<T> : IEnumerable<T>
{
    private readonly LinkedList<T> _list = new();
    private readonly Dictionary<T, LinkedListNode<T>> _map = new();

    public int Count => _list.Count;

    public void Enqueue(T item)
    {
        if (_map.ContainsKey(item))
            throw new ArgumentException("Item already exists in queue.");

        var node = _list.AddLast(item);
        _map[item] = node;
    }

    public bool Remove(T item)
    {
        if (!_map.TryGetValue(item, out var node))
            return false;

        _list.Remove(node);
        _map.Remove(item);
        return true;
    }

    public T Peek()
    {
        if (_list.Count == 0)
            throw new InvalidOperationException("Queue is empty.");
        return _list.First!.Value;
    }

    public void Clear()
    {
        _list.Clear();
        _map.Clear();
    }

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
