using System;

namespace sky_quant_task;

class Program
{
    public static void Main(string[] args)
    {
        var filePath = "ticks.csv";
        var orderBook = new OrderBook();
        orderBook.BuildFromCsv(filePath);
    }
}