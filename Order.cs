namespace sky_quant_task;

using OrderId = Int64;
using Price = Int32;

public enum OrderSide
{
    Buy = 1,
    Sell = 2
}

public class Order
{
    public long sourceTime {get; set;}
    public OrderSide? side {get; set;}
    public char action {get; set;}
    public OrderId orderId {get; set;}
    public Price price {get; set;}
    public int quantity {get; set;}

    public Order(long sourceTime, OrderSide? side, char action, OrderId orderId, Price price, int quantity)
    {
        this.sourceTime = sourceTime;
        this.side = side;
        this.action = action;
        this.orderId = orderId;
        this.price = price;
        this.quantity = quantity;
    }
}