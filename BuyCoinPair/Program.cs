
using BuyCoinPair.Models;
using BuyCoinPair.Spot;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace BuyCoinPair
{
    class Program
    {
        private List<CoinModel> CoinList = new List<CoinModel>();
        private static decimal Balance = 100;
        private static decimal GreaterValue = (decimal)1.3;
        private static string apiKey = "hOjAjUks3KmWfAWKSBUCkyKx17GRYCKK73hpqJoEJRSfU6Jixhh2K3Iv4PO75hnT";
        private static string apiSecret = "yAunddYqfmxXixURCKNDNBUFMQxYBokMdCwdnjjOINStGnUAMdE4FNqPGUUNKnUV";
        private long MaxOrder = 0;
        public static void Main(string[] args) => new Program().InitData().GetAwaiter();

        private async Task InitData()
        {
            await LoadJson();
            while (true)
            {
                await CheckCoin();
                CoinList = CoinList.OrderBy(x => x.Order).ToList();
                Thread.Sleep(500);
            }
        }

        private async Task LoadJson()
        {
            List<CoinModel> items = new List<CoinModel>();
            using (StreamReader r = new StreamReader("coin.json"))
            {
                string json = r.ReadToEnd();
                var jsonResult = JsonConvert.DeserializeObject<List<CoinModel>>(json);
                if (jsonResult != null)
                {
                    foreach (var item in jsonResult)
                    {
                        item.Pair = item?.Pair?.OrderBy(x => x.Order).ToList();
                    }
                    items = jsonResult.OrderBy(x => x.Order).ToList();
                }
            }

            MaxOrder = items.Max(x => x.Order);
            CoinList = items;
        }

        private async Task CheckCoin()
        {
            try
            {
                Market market = new Market(
                    new HttpClient(),
                    apiKey: apiKey,
                    apiSecret: apiSecret);

                foreach (var coin in CoinList)
                {
                    MaxOrder++;
                    coin.Order = MaxOrder;
                    string firstPair = coin?.Pair?[0]?.Name ?? string.Empty;
                    string secondPair = coin?.Pair?[1]?.Name ?? string.Empty;
                    string thirdPair = coin?.Pair?[2]?.Name ?? string.Empty;

                    string symbolList = $"[\"{firstPair}\",\"{ secondPair }\",\"{ thirdPair}\"]";
                    string result = await market.SymbolPriceTicker(symbols: symbolList);
                    if (result != null)
                    {
                        List<BinanceDataReceivedModel> coinResults = JsonConvert.DeserializeObject<List<BinanceDataReceivedModel>>(result);
                        if (coinResults != null)
                        {
                            decimal firstValue = coinResults.FirstOrDefault(x => x.Symbol == firstPair).Price;
                            decimal secondValue = coinResults.FirstOrDefault(x => x.Symbol == secondPair).Price;
                            decimal thirdValue = coinResults.FirstOrDefault(x => x.Symbol == thirdPair).Price;
                            var interestValue = ((Balance / firstValue) * secondValue * thirdValue) - Balance;
                            // Console.WriteLine(symbolList + " : " + interestValue.ToString());
                            if (interestValue < GreaterValue)
                            {
                                continue;
                            }

                            var exchangeInfoResult = await market.ExchangeInformation(symbols: symbolList);
                            ExchangeRecivedModel exchangeInfos = JsonConvert.DeserializeObject<ExchangeRecivedModel>(exchangeInfoResult);
                            Console.WriteLine("Buying................" + symbolList + ": " + interestValue.ToString());

                            var isTraded = await TradeCoin(firstPair, secondPair, thirdPair, exchangeInfos);
                            if (isTraded != null)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private async Task<bool> TradeCoin(string pair1Name, string pair2Name, string pair3Name, ExchangeRecivedModel exchangeInfos)
        {
            try
            {
                SpotAccountTrade spotAccountTrade = new SpotAccountTrade(
                    new HttpClient(),
                    apiKey: apiKey,
                    apiSecret: apiSecret);

                var resultPair1 = await spotAccountTrade.NewOrder(pair1Name, Side.BUY, OrderType.MARKET, quoteOrderQty: 100);
                var resultPair1Object = JsonConvert.DeserializeObject<OrderModel>(resultPair1);
                var resultPair2 = await spotAccountTrade.NewOrder(pair2Name, Side.SELL, OrderType.MARKET, quantity: ConvertQuantity(resultPair1Object.OrigQty, pair2Name, exchangeInfos));
                var resultPair2Object = JsonConvert.DeserializeObject<OrderModel>(resultPair2);
                var resultPair3 = await spotAccountTrade.NewOrder(pair3Name, Side.SELL, OrderType.MARKET, quantity: ConvertQuantity(resultPair2Object.CummulativeQuoteQty, pair3Name, exchangeInfos));
                var resultPair3Object = JsonConvert.DeserializeObject<OrderModel>(resultPair3);
                Console.WriteLine("SELL TO USDT: " + resultPair3.ToString());
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private decimal ConvertQuantity(decimal quantity, string pairName, ExchangeRecivedModel exchangeInfos)
        {
            var symbol = exchangeInfos.Symbols.FirstOrDefault(x => x.Symbol == pairName);
            if (symbol == null)
            {
                return quantity;
            }
            var lotSize = symbol.Filters.FirstOrDefault(x => x.FilterType == "LOT_SIZE");
            
            if (lotSize == null || lotSize.MinQty == 0)
            {
                return quantity;
            }

            return ((int)(quantity / lotSize.MinQty) * lotSize.MinQty)/1.000000000000000000000000000000000m;
        }
    }
}