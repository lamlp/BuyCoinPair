
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
        private static decimal GreaterValue = (decimal)0.5;
        private static string apiKey = "hOjAjUks3KmWfAWKSBUCkyKx17GRYCKK73hpqJoEJRSfU6Jixhh2K3Iv4PO75hnT";
        private static string apiSecret = "yAunddYqfmxXixURCKNDNBUFMQxYBokMdCwdnjjOINStGnUAMdE4FNqPGUUNKnUV";
        private long MaxOrder = 0;
        public static void Main(string[] args) => new Program().InitData().GetAwaiter();

        private async Task InitData()
        {
            Console.WriteLine("Hello to BuyCoinPair................version 13.09.22");
            Console.WriteLine("Loading coin data................");
            await GetCoinData();
            Console.WriteLine("Loading coin data............DONE");
            //await LoadJson();
            while (true)
            {
                await CheckCoin();
                CoinList = CoinList.OrderBy(x => x.Order).ToList();
            }
        }

        private async Task GetCoinData()
        {
            long order = 0;
            List<CoinModel> data = new List<CoinModel>(); 
            Market market = new Market(
                new HttpClient(),
                apiKey: apiKey,
                apiSecret: apiSecret);
            string result = await market.SymbolPriceTicker();
            List<BinanceDataReceivedModel> coinResult = JsonConvert.DeserializeObject<List<BinanceDataReceivedModel>>(result);
            List<BinanceDataReceivedModel> coinEndWithUsdt = coinResult.Where(x => x.Symbol.EndsWith("USDT")).ToList();
            List<string> coinNames = coinEndWithUsdt.Select(x => x.Symbol.Remove(x.Symbol.Length-4, 4)).ToList();
            foreach(var coin in coinNames)
            {
                //List<string> middleCoins = new List<string>() { "BTC", "ETH", "BNB", "AUD", "BIDR", "BRL", "EUR", "GBP", "RUB", "TRY", "TUSD", "DAI", "UAH", "VAI", "IDRT", "NGN" };
                List<string> middleCoins = new List<string>() { "BTC", "ETH", "BNB" };

                foreach (var middleCoin in middleCoins)
                {
                    var alt = coinResult.FirstOrDefault(x => x.Symbol == $"{coin}{middleCoin}");
                    if (alt != null)
                    {
                        var coinData = new CoinModel();
                        var pair1 = new PairModel();
                        pair1.Name = $"{coin}USDT";
                        pair1.Order = 1;
                        var pair2 = new PairModel();
                        pair2.Name = $"{coin}{middleCoin}";
                        pair2.Order = 2;
                        var pair3 = new PairModel();
                        pair3.Name = $"{middleCoin}USDT";
                        pair3.Order = 3;
                        coinData.Pair = new List<PairModel>();
                        coinData.Pair.Add(pair1);
                        coinData.Pair.Add(pair2);
                        coinData.Pair.Add(pair3);

                       //check existing Coin
                        var task1 = market.OrderBook(pair1.Name, 1);
                        var task2 = market.OrderBook(pair2.Name, 1);
                        var task3 = market.OrderBook(pair3.Name, 1);
                        var resultOrders = await Task.WhenAll(task1, task2, task3);
                        if (resultOrders != null && resultOrders[0] != null && resultOrders[1] != null && resultOrders[2] != null)
                        {
                            OrderDepthModel? coin1Result = JsonConvert.DeserializeObject<OrderDepthModel>(resultOrders[0]);
                            OrderDepthModel? coin2Result = JsonConvert.DeserializeObject<OrderDepthModel>(resultOrders[1]);
                            OrderDepthModel? coin3Result = JsonConvert.DeserializeObject<OrderDepthModel>(resultOrders[2]);
                            if (coin1Result == null || coin2Result == null || coin3Result == null ||
                                coin1Result.Asks == null || coin2Result.Asks == null || coin3Result.Asks == null ||
                                coin1Result.Asks.Count() == 0 || coin2Result.Asks.Count() == 0 || coin3Result.Asks.Count() == 0)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        order++;
                        Console.Write("\r{0} coins was added...", order);
                        coinData.Order = order;
                        data.Add(coinData);
                    }
                }
            }

            MaxOrder = data.Max(x => x.Order);
            CoinList = data;
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
                    string firstPair = coin.Pair[0].Name;
                    string secondPair = coin.Pair[1].Name;
                    string thirdPair = coin.Pair[2].Name;

                    string symbolList = $"[\"{firstPair}\",\"{secondPair}\",\"{thirdPair}\"]";
                    var task1 = market.OrderBook(firstPair, 1);
                    var task2 = market.OrderBook(secondPair, 1);
                    var task3 = market.OrderBook(thirdPair, 1);
                    var result = await Task.WhenAll(task1, task2, task3);
                    if (result != null && result[0] != null && result[1] != null && result[2] != null)
                    {
                        OrderDepthModel? coin1Result = JsonConvert.DeserializeObject<OrderDepthModel>(result[0]);
                        OrderDepthModel? coin2Result = JsonConvert.DeserializeObject<OrderDepthModel>(result[1]);
                        OrderDepthModel? coin3Result = JsonConvert.DeserializeObject<OrderDepthModel>(result[2]);
                        if (coin1Result != null && coin2Result != null && coin3Result != null)
                        {
                            decimal interestValue = 0;
                            try
                            {
                                decimal firstValue = coin1Result.Asks[0][0];
                                decimal secondValue = coin2Result.Bids[0][0];
                                decimal thirdValue = coin3Result.Bids[0][0];
                                interestValue = ((Balance / firstValue) * secondValue * thirdValue) - Balance;
                                Console.WriteLine(symbolList + " : " + interestValue.ToString());
                            }
                            catch
                            {
                                continue;
                            }

                            if (interestValue < GreaterValue)
                            {
                                continue;
                            }

                            var exchangeInfoResult = await market.ExchangeInformation(symbols: symbolList);
                            ExchangeRecivedModel? exchangeInfos = JsonConvert.DeserializeObject<ExchangeRecivedModel>(exchangeInfoResult);
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