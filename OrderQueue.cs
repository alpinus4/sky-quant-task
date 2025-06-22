namespace sky_quant_task;

using OrderId = Int64;

public class OrderQueue
{
    private readonly RemovableQueue<OrderId> _queue = new ();
    public long QtyCounter { get; private set; }
    
    public int Count => _queue.Count;

    public void Enqueue(OrderId orderId, long quantity)
    {
        _queue.Enqueue(orderId);
        QtyCounter += quantity;
    }
    
    public bool Remove(OrderId orderId, long quantity)
    {
        if (!_queue.Remove(orderId))
        {
            return false;
        }
        QtyCounter -= quantity;
        return true;
    }
    
    public OrderId Peek()
    {
        return _queue.Peek();
    }
    
    public void Clear()
    {
        _queue.Clear();
        QtyCounter = 0;
    }
}