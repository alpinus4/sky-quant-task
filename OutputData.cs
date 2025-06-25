namespace sky_quant_task;

using Price = System.Int32;

public class OutputData
{
    public int Quantity;
    public Price? BestBidPrice;
    public long? BestBidQuantity;
    public long? BestBidOrderCount;
    public Price? BestAskPrice;
    public long? BestAskQuantity;
    public long? BestAskOrderCount;

    public void Set(
        int quantity,
        Price? bestBidPrice,
        long? bestBidQuantity,
        long? bestBidOrderCount,
        Price? bestAskPrice,
        long? bestAskQuantity,
        long? bestAskOrderCount)
    {
        Quantity = quantity;
        BestBidPrice = bestBidPrice;
        BestBidQuantity = bestBidQuantity;
        BestBidOrderCount = bestBidOrderCount;
        BestAskPrice = bestAskPrice;
        BestAskQuantity = bestAskQuantity;
        BestAskOrderCount = bestAskOrderCount;
    }
}