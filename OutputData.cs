namespace sky_quant_task;

using Price = Int32;
public class OutputData(
    int quantity,
    Price? bestBidPrice,
    long? bestBidQuantity,
    long? bestBidOrderCount,
    Price? bestAskPrice,
    long? bestAskQuantity,
    long? bestAskOrderCount)
{
    public int Quantity = quantity;
    public Price? BestBidPrice = bestBidPrice;
    public long? BestBidQuantity = bestBidQuantity;
    public long? BestBidOrderCount = bestBidOrderCount;
    public Price? BestAskPrice = bestAskPrice;
    public long? BestAskQuantity = bestAskQuantity;
    public long? BestAskOrderCount = bestAskOrderCount;
}