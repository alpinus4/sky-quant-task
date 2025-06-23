namespace sky_quant_task;

using OrderId = Int64;

public class OrderQueue
{
    private readonly QueueWithRemove<OrderId> _queueWithRemove = new ();
    public long QtyCounter { get; set; }
    
    public int Count => _queueWithRemove.Count;

    public void Enqueue(OrderId orderId, long quantity)
    {
        _queueWithRemove.Enqueue(orderId);
        QtyCounter += quantity;
    }
    
    public bool Remove(OrderId orderId, long quantity)
    {
        if (!_queueWithRemove.Remove(orderId))
        {
            return false;
        }
        QtyCounter -= quantity;
        return true;
    }
    
    public OrderId Peek()
    {
        return _queueWithRemove.Peek();
    }
    
    public void Clear()
    {
        _queueWithRemove.Clear();
        QtyCounter = 0;
    }
}