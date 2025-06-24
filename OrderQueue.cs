namespace sky_quant_task;

using OrderId = Int64;

public class OrderQueue
{
    private readonly LinkedList<OrderId> _list = new();
    private readonly Dictionary<OrderId, LinkedListNode<OrderId>> _map = new();
    public long QtyCounter { get; set; }
    
    public int Count => _list.Count;

    public void Enqueue(OrderId orderId, long quantity)
    {
        if (_map.ContainsKey(orderId))
            throw new ArgumentException("Item already exists in queue.");

        var node = _list.AddLast(orderId);
        _map[orderId] = node;
        QtyCounter += quantity;
    }
    
    public bool Remove(OrderId orderId, long quantity)
    {
        if (!_map.TryGetValue(orderId, out var node))
            return false;

        _list.Remove(node);
        _map.Remove(orderId);
        QtyCounter -= quantity;
        return true;
    }
    
    public OrderId Peek()
    {
        if (_list.Count == 0)
            throw new InvalidOperationException("Queue is empty.");
        return _list.First!.Value;
    }
    
    public void Clear()
    {
        _list.Clear();
        _map.Clear();
        QtyCounter = 0;
    }
}