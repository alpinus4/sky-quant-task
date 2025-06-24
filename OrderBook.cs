namespace sky_quant_task;

using OrderId = Int64;
using Price = Int32;
class DescendingComparer<T> : IComparer<T> where T : IComparable<T> {
    public int Compare(T x, T y) {
        return y.CompareTo(x);
    }
}
public class OrderBook
{
    private readonly SortedDictionary<Price, OrderQueue> _buyMap = new (new DescendingComparer<Price>());
    private readonly SortedDictionary<Price, OrderQueue> _sellMap = new ();
    private readonly Dictionary<OrderId, Order> _orderIdToObjectMap = new ();

    private List<Order> ReadCsv(string path)
    {
        var orders = new List<Order>();
        foreach (var line in File.ReadLines(path))
        {
            try
            {
                var fields = line.Split(';');
                var sourceTime = long.Parse(fields[0]);
                OrderSide? side = string.IsNullOrWhiteSpace(fields[1])
                    ? null
                    : (int.Parse(fields[1]) == 1 ? OrderSide.Buy : OrderSide.Sell);
                var action = char.Parse(fields[2]);
                var orderId = OrderId.Parse(fields[3]);
                var price = Price.Parse(fields[4]);
                var quantity = int.Parse(fields[5]);
                orders.Add(new Order(sourceTime, side, action, orderId, price, quantity));
            }
            catch (FormatException e)
            {
                // empty value in row, go to next row
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return orders;
    }

    private Price? GetBestBuyPrice()
    {
        if (_buyMap.Count == 0) return null;
        return _buyMap.First().Key;
    }
    
    private long? GetBestBuyTotalQty()
    {
        if (_buyMap.Count == 0) return null;
        return _buyMap.First().Value.QtyCounter;
    }
    
    private long? GetBestBuyOrderCount()
    {
        if (_buyMap.Count == 0) return null;
        return _buyMap.First().Value.Count;
    }

    private Price? GetBestSellPrice()
    {
        if (_sellMap.Count == 0) return null;
        return _sellMap.First().Key;
    }
    
    private long? GetBestSellTotalQty()
    {
        if (_sellMap.Count == 0) return null;
        return _sellMap.First().Value.QtyCounter;
    }
    
    private long? GetBestSellOrderCount()
    {
        if (_sellMap.Count == 0) return null;
        return _sellMap.First().Value.Count;
    }

    private void Clear()
    {
        _buyMap.Clear();
        _sellMap.Clear();
        _orderIdToObjectMap.Clear();
    }

    private void ResolveOrdersPartial(Order newOrder, Func<bool> limit_condition, SortedDictionary<Price, OrderQueue> map)
    {
        // while price exceeds limit
        while (limit_condition())
        {
            // get best
            // take from it
            var (_, bestQueue) = map.First();
            var bestOrderId = bestQueue.Peek();
            if (_orderIdToObjectMap[bestOrderId].quantity >= newOrder.quantity)
            {
                // best order eats new order
                _orderIdToObjectMap[bestOrderId].quantity -= newOrder.quantity;
                DeleteOrder(newOrder);
                if (_orderIdToObjectMap[bestOrderId].quantity == 0)
                {
                    // quantities sometimes might be equal
                    DeleteOrder(_orderIdToObjectMap[bestOrderId]);
                }
                break;
            }
            // best order is eaten by new order
            _orderIdToObjectMap[newOrder.orderId].quantity -= _orderIdToObjectMap[bestOrderId].quantity;
            DeleteOrder(_orderIdToObjectMap[bestOrderId]);
        }
    }

    private void ResolveOrders(Order newOrder)
    {
        if (GetBestSellPrice() == null || GetBestBuyPrice() == null) return; // nothing to resolve

        if (newOrder.side == OrderSide.Buy)
        {
            ResolveOrdersPartial(newOrder, () => newOrder.price >= GetBestSellPrice(), _sellMap);
        }
        else
        {
            ResolveOrdersPartial(newOrder, () => newOrder.price <= GetBestBuyPrice(), _buyMap);
        }
    }
    
    private void AddOrder(Order order)
    {
        var map = order.side ==  OrderSide.Buy ? _buyMap : _sellMap;
        if (!map.TryGetValue(order.price, out OrderQueue? value))
        {
            value = new OrderQueue();
            map[order.price] = value;
        }

        value.Enqueue(order.orderId, order.quantity);

        _orderIdToObjectMap[order.orderId] = order;
        
        // if exceeds limit, resolve
        if (order.sourceTime >= 24300006000 && order.sourceTime <= 53400000000)
        {
            ResolveOrders(order);
        }
    }
    
    private void ModifyOrder(Order order)
    {
        if (!_orderIdToObjectMap.ContainsKey(order.orderId))
        {
            AddOrder(order);
            return;
        }
        
        var map = order.side ==  OrderSide.Buy ? _buyMap : _sellMap;
        var queue = map[order.price];
        var qty_diff = _orderIdToObjectMap[order.orderId].quantity - order.quantity;
        queue.QtyCounter -= qty_diff;
        _orderIdToObjectMap[order.orderId] = order;
        // if exceeds limit, resolve
        if (order.sourceTime >= 24300006000 && order.sourceTime <= 53400000000)
        {
            ResolveOrders(order);
        }
    }
    
    private void DeleteOrder(Order order)
    {
        var map = order.side ==  OrderSide.Buy ? _buyMap : _sellMap;
        try
        {
            map[order.price].Remove(order.orderId, _orderIdToObjectMap[order.orderId].quantity);
            if (map[order.price].Count == 0)
            {
                map.Remove(order.price);
            }
            _orderIdToObjectMap.Remove(order.orderId);
        }
        catch (KeyNotFoundException)
        {
            // do nothing doesn't matter
        }
    }

    private void OutputToCsv(string outPath, List<Order> orders, OutputData[] outputData)
    {
        using (var writer = new StreamWriter(outPath))
        {
            writer.WriteLine("SourceTime;Side;Action;OrderId;Price;Qty;B0;BQ0;BN0;A0;AQ0;AN0");
            
            for (int i = 0; i < orders.Count; i++)
            {
                var side = orders[i].side != null ? ((int)orders[i].side.Value).ToString() : "";
                string TryStringIfNotNull(long? val) => val is null ? "" : val.ToString();
                writer.WriteLine($"{orders[i].sourceTime};{side};{orders[i].action};" +
                                 $"{orders[i].orderId};{orders[i].price};{outputData[i].Quantity};" +
                                 $"{TryStringIfNotNull(outputData[i].BestBidPrice)};{TryStringIfNotNull(outputData[i].BestBidQuantity)};{TryStringIfNotNull(outputData[i].BestBidOrderCount)};" +
                                 $"{TryStringIfNotNull(outputData[i].BestAskPrice)};{TryStringIfNotNull(outputData[i].BestAskQuantity)};{TryStringIfNotNull(outputData[i].BestAskOrderCount)}");
            
            }
        }
    }

    public void BuildFromCsv(string csvPath)
    {
        var orders = ReadCsv(csvPath);
        Console.WriteLine(orders.Count);

        var output = new OutputData[orders.Count];
        
        var sw = new System.Diagnostics.Stopwatch();
        for (int k = 0; k < 5; k++)
        {
            sw.Restart();
            Clear();
            for (int i = 0; i < orders.Count; i++)
            {
                var qty = orders[i].quantity;
                if (orders[i].side != null)
                {
                    switch (orders[i].action)
                    {
                        case 'Y':
                        case 'F':
                            Clear();
                            break;
                        case 'A':
                            AddOrder(orders[i]);
                            break;
                        case 'M':
                            ModifyOrder(orders[i]);
                            break;
                        case 'D':
                            DeleteOrder(orders[i]);
                            break;
                    }
                }

                output[i] = new OutputData(qty, GetBestBuyPrice(), GetBestBuyTotalQty(), GetBestBuyOrderCount(),
                    GetBestSellPrice(), GetBestSellTotalQty(), GetBestSellOrderCount());
            }

            sw.Stop();

            Console.WriteLine($"Total time [us]: {sw.ElapsedMilliseconds * 1000.0:F3}");
            Console.WriteLine($"Time per tick [us]: {sw.ElapsedMilliseconds * 1000.0 / orders.Count:F3}");

            OutputToCsv("out.csv", orders, output);
        }
    }
}