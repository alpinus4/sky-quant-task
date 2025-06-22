namespace sky_quant_task;

using Price = Int32;
public class OutputData
{
    public Price BestBidPrice;
    public long BestBidQuantity;
    public long BestBidOrderCount;
    public Price BestAskPrice;
    public long BestAskQuantity;
    public long BestAskOrderCount;

    public OutputData(Price bestBidPrice, long bestBidQuantity, long bestBidOrderCount, Price bestAskPrice,
        long bestAskQuantity,
        long bestAskOrderCount)
    {
        BestBidPrice = bestBidPrice;
        BestBidQuantity = bestBidQuantity;
        BestBidOrderCount = bestBidOrderCount;
        BestAskPrice = bestAskPrice;
        BestAskQuantity = bestAskQuantity;
        BestAskOrderCount = bestAskOrderCount;
    }
}