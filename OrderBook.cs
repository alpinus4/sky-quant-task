namespace sky_quant_task;

using OrderId = Int64;
using Price = Int32;

class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
{
    public int Compare(T x, T y)
    {
        return y.CompareTo(x);
    }
}

public class OrderBook
{
    private Price? _bestBidPrice;
    private Price? _bestAskPrice;
    private readonly SortedDictionary<Price, OrderQueue> _buyMap = new(new DescendingComparer<Price>());
    private readonly SortedDictionary<Price, OrderQueue> _sellMap = new();
    private readonly Dictionary<OrderId, Order> _orderIdToObjectMap = new();

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

    private long? GetBestBuyTotalQty()
    {
        if (_buyMap.Count == 0 || _bestBidPrice == null) return null;
        return _buyMap[(Price)_bestBidPrice].QtyCounter;
    }

    private long? GetBestBuyOrderCount()
    {
        if (_buyMap.Count == 0 || _bestBidPrice == null) return null;
        return _buyMap[(Price)_bestBidPrice].Count;
    }

    private long? GetBestSellTotalQty()
    {
        if (_sellMap.Count == 0  || _bestAskPrice == null) return null;
        return _sellMap[(Price)_bestAskPrice].QtyCounter;
    }

    private long? GetBestSellOrderCount()
    {
        if (_sellMap.Count == 0  || _bestAskPrice == null) return null;
        return _sellMap[(Price)_bestAskPrice].Count;
    }

    private void Clear()
    {
        _buyMap.Clear();
        _sellMap.Clear();
        _orderIdToObjectMap.Clear();
        _bestBidPrice = null;
        _bestAskPrice = null;
    }

    private void ResolveOrdersPartial(Order newOrder, Func<bool> limit_condition,
        SortedDictionary<Price, OrderQueue> map)
    {
        // while price exceeds limit
        while (limit_condition())
        {
            // get best
            // take from it
            if (_bestAskPrice == null || _bestBidPrice == null)
            {
                return;
            }
            var bestQueue = map[newOrder.side == OrderSide.Buy ? (Price)_bestAskPrice : (Price)_bestBidPrice];
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
        if (_bestBidPrice == null || _bestAskPrice == null) return; // nothing to resolve

        if (newOrder.side == OrderSide.Buy)
        {
            ResolveOrdersPartial(newOrder, () => newOrder.price >= _bestBidPrice, _sellMap);
        }
        else
        {
            ResolveOrdersPartial(newOrder, () => newOrder.price <= _bestAskPrice, _buyMap);
        }
    }

    private void AddOrder(Order order, bool to_resolve)
    {
        var map = order.side == OrderSide.Buy ? _buyMap : _sellMap;
        if (!map.TryGetValue(order.price, out OrderQueue? value))
        {
            value = new OrderQueue();
            map[order.price] = value;
            if (order.side == OrderSide.Buy)
            {
                if (order.price > _bestBidPrice || _bestBidPrice == null)
                {
                    _bestBidPrice = order.price;
                }
            }
            else
            {
                if (order.price < _bestAskPrice || _bestAskPrice == null)
                {
                    _bestAskPrice = order.price;
                }
            }
        }

        value.Enqueue(order.orderId, order.quantity);

        _orderIdToObjectMap[order.orderId] = order;

        // if exceeds limit, resolve
        if (to_resolve)
        {
            ResolveOrders(order);
        }
    }

    private void ModifyOrder(Order order, bool to_resolve)
    {
        if (!_orderIdToObjectMap.TryGetValue(order.orderId, out var existingOrder))
        {
            AddOrder(order, to_resolve);
            return;
        }

        var map = order.side == OrderSide.Buy ? _buyMap : _sellMap;
        if (!map.TryGetValue(order.price, out OrderQueue? queue))
        {
            // we change queue
            map[existingOrder.price].Remove(existingOrder.orderId, existingOrder.quantity);
            queue = new OrderQueue();
            map[order.price] = queue;
            queue.Enqueue(order.orderId, order.quantity);
            if (order.side == OrderSide.Buy)
            {
                if (order.price > _bestBidPrice || _bestBidPrice == null)
                {
                    _bestBidPrice = order.price;
                }
            }
            else
            {
                _bestAskPrice = order.price;
            }
        }

        var qty_diff = existingOrder.quantity - order.quantity;
        queue.QtyCounter -= qty_diff;
        _orderIdToObjectMap[order.orderId] = order;
        // if exceeds limit, resolve
        if (to_resolve)
        {
            ResolveOrders(order);
        }
    }

    private void DeleteOrder(Order order)
    {
        var map = order.side == OrderSide.Buy ? _buyMap : _sellMap;
        try
        {
            map[order.price].Remove(order.orderId, _orderIdToObjectMap[order.orderId].quantity);
            if (map[order.price].Count == 0)
            {
                map.Remove(order.price);
                if (order.side == OrderSide.Buy)
                {
                    if (order.price == _bestBidPrice)
                    {
                        if (_buyMap.Count == 0)
                        {
                            _bestBidPrice = null;
                        }
                        else
                        {
                            _bestBidPrice = _buyMap.First().Key;
                        }
                    }
                }
                else
                {
                    if (order.price == _bestAskPrice)
                    {
                        if (_sellMap.Count == 0)
                        {
                            _bestAskPrice = null;
                        }
                        else
                        {
                            _bestAskPrice = _sellMap.First().Key;
                        }
                    }
                }
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
        var sw = new System.Diagnostics.Stopwatch();
        for (int k = 0; k < 10; k++)
        {
            Clear();

            var orders = ReadCsv(csvPath);
            Console.WriteLine(orders.Count);

            var output = new OutputData[orders.Count];
            for (int i = 0; i < orders.Count; i++)
            {
                output[i] = new OutputData(); // Allocate before main loop
            }

            sw.Restart();
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
                            var bestPrice = orders[i].side == OrderSide.Buy ? _bestAskPrice : _bestBidPrice;
                            var exceeds = orders[i].side == OrderSide.Buy ? orders[i].price > bestPrice : orders[i].price < bestPrice;
                            AddOrder(orders[i], orders[i].sourceTime >= 24300006000 && orders[i].sourceTime <= 53400000000 && exceeds);
                            break;
                        case 'M':
                            var bestPrice2 = orders[i].side == OrderSide.Buy ? _bestAskPrice : _bestBidPrice;
                            var exceeds2 = orders[i].side == OrderSide.Buy ? orders[i].price > bestPrice2 : orders[i].price < bestPrice2;
                            ModifyOrder(orders[i], orders[i].sourceTime >= 24300006000 && orders[i].sourceTime <= 53400000000 && exceeds2);
                            break;
                        case 'D':
                            DeleteOrder(orders[i]);
                            break;
                    }
                }

                output[i].Set(qty, _bestBidPrice, GetBestBuyTotalQty(), GetBestBuyOrderCount(),
                    _bestAskPrice, GetBestSellTotalQty(), GetBestSellOrderCount());
            }

            sw.Stop();

            Console.WriteLine($"Total time [us]: {sw.ElapsedMilliseconds * 1000.0:F3}");
            Console.WriteLine($"Time per tick [us]: {sw.ElapsedMilliseconds * 1000.0 / orders.Count:F3}");

            OutputToCsv("out.csv", orders, output);
        }
    }
}