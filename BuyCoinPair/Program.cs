
using BuyCoinPair.Common;
using BuyCoinPair.Models;
using BuyCoinPair.Spot;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Websocket.Client;

namespace BuyCoinPair
{
    class Program
    {
        private List<CoinModel> CoinList = new List<CoinModel>();
        List<WebsocketCoinModel> CoinPairs = new List<WebsocketCoinModel>();
        private static decimal Balance = 100;
        private static decimal GreaterValue = (decimal)0.34;
        private static string apiKey = "";
        private static string apiSecret = "";
        public static void Main(string[] args) => new Program().InitData().GetAwaiter();

        private async Task InitData()
        {
            Console.WriteLine("Welcome to BuyCoinPair................version 14.09.22");
            Console.WriteLine("Loading coin data................");

            await LoadJson();
            await HandleWebsocket();
            //await GetCoinData();
        }

        private async Task HandleWebsocket()
        {
            ManualResetEvent ExitEvent = new ManualResetEvent(false);

            var factory = new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket
                {
                    Options =
                    {
                        KeepAliveInterval = TimeSpan.FromSeconds(30),
                    }
                };
                return client;
            });


            var url = new Uri("wss://stream.binance.com:9443/ws/BTCUSDT@trade");

            string[] requestCoinNames = CoinPairs.Select(pair => $"{pair.Name.ToLower()}@depth").ToArray();

            string[] pair1List = CoinList.Select(x => x.Pair.OrderBy(x => x.Order).First().Name).ToArray();

            // int numberLoops = CoinPairs.Count() / 100;

            // requestCoinNames = requestCoinNames.Take(200).ToArray();

            using (IWebsocketClient client = new WebsocketClient(url, factory))
            {


                client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ErrorReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ReconnectionHappened.Subscribe(info =>
                {
                    Console.WriteLine($"Reconnection happened, type: {info.Type}, url: {client.Url}");
                });
                client.DisconnectionHappened.Subscribe(info =>
                    Console.WriteLine($"Disconnection happened, type: {info.Type}"));

                client.MessageReceived.Subscribe(msg =>
                {
                    var result = JsonConvert.DeserializeObject<WebsocketDepthModel>(msg?.Text);
                    if (result != null && result.Name != null && result.Asks != null && result.Asks.Count() > 3 && result.Bids != null && result.Bids.Count() > 3)
                    {
                        var coin = CoinPairs.FirstOrDefault(x => x.Name == result.Name);
                        if (coin != null)
                        {
                            coin.AskPrice = (result.Asks[0][0] + result.Asks[1][0]) / 2;
                            coin.BidPrice = (result.Bids[0][0] + result.Bids[1][0]) / 2;
                                // Console.WriteLine($"{result.Name}: {coin.AskPrice}, {coin.BidPrice}");
                            if (pair1List.Contains(coin.Name))
                            {
                                var altName = coin.Name.Remove(coin.Name.Length - 4);
                                CheckCoin(coin.Name, altName+"BNB", "BNBBUSD");
                            }
                        }
                    }
                });

                Console.WriteLine("Starting...");
                client.Start().Wait();
                Console.WriteLine("Started.");

                //for (int i = 0; i <= numberLoops; i++)
                //{
                //    var loopRequestCoins = requestCoinNames.Skip(i * 100).Take(100).ToArray();
                //    var requestObj = new WebsocketRequestModel() { method = "SUBSCRIBE", id = 1, Params = loopRequestCoins };
                //    Task.Run(() => client.Send(JsonConvert.SerializeObject(requestObj)));
                //}
                var requestObj = new WebsocketRequestModel() { method = "SUBSCRIBE", id = 1, Params = requestCoinNames };
                Task.Run(() => client.Send(JsonConvert.SerializeObject(requestObj)));

                ExitEvent.WaitOne();
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
                        foreach (var coinInPair in item?.Pair)
                        {
                            if (!string.IsNullOrEmpty(coinInPair.Name) && !CoinPairs.Exists(x => x.Name == coinInPair.Name))
                            {
                                CoinPairs.Add(new WebsocketCoinModel { Name = coinInPair.Name });
                            }
                        }
                    }
                    items = jsonResult.OrderBy(x => x.Order).ToList();
                }
            }

            CoinList = items;
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
            List<BinanceDataReceivedModel> coinEndWithBusd = coinResult.Where(x => x.Symbol.EndsWith("BUSD")).ToList();
            List<string> coinNameUsdts = coinEndWithUsdt.Select(x => x.Symbol.Remove(x.Symbol.Length - 4, 4)).ToList();
            List<string> coinNameBusds = coinEndWithBusd.Select(x => x.Symbol.Remove(x.Symbol.Length - 4, 4)).ToList();

            // List<string> middleCoins = new List<string>() { "BTC", "ETH", "BNB", "AUD", "BIDR", "BRL", "EUR", "GBP", "RUB", "TRY", "TUSD", "DAI", "UAH", "VAI", "IDRT", "NGN" };
            // List<string> middleCoins = new List<string>() { "BTC", "ETH", "BNB" };
            List<string> middleCoins = new List<string>() { "BNB" };

            foreach (var coin in coinNameBusds)
            {
                foreach (var middleCoin in middleCoins)
                {
                    var alt = coinResult.FirstOrDefault(x => x.Symbol == $"{coin}{middleCoin}");
                    if (alt != null)
                    {
                        var coinData = new CoinModel();
                        var pair1 = new PairModel();
                        pair1.Name = $"{coin}BUSD";
                        pair1.Order = 1;
                        var pair2 = new PairModel();
                        pair2.Name = $"{coin}{middleCoin}";
                        pair2.Order = 2;
                        var pair3 = new PairModel();
                        pair3.Name = $"{middleCoin}BUSD";
                        pair3.Order = 3;
                        coinData.Pair = new List<PairModel>();
                        coinData.Pair.Add(pair1);
                        coinData.Pair.Add(pair2);
                        coinData.Pair.Add(pair3);

                        //check existing Coin
                        try
                        {
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
                        }
                        catch
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

            foreach (var coin in coinNameUsdts)
            {
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
                        try
                        {
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
                        }
                        catch
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

            CoinList = data;
        }

        private async Task CheckCoin(string coinName, string coin2, string coin3)
        {
            try
            {
                //var coin = CoinList.FirstOrDefault(x => x.Pair.OrderBy(x => x.Order).First().Name == coinName);
                //if (coin == null)
                //{
                //    return;
                //}

                //foreach (var coin in listCoinPairContain)
                //{
                string firstPair = coinName;
                string secondPair = coin2;
                string thirdPair = coin3;

                string symbolList = $"[\"{firstPair}\",\"{secondPair}\",\"{thirdPair}\"]";


                var coin1Result = CoinPairs.FirstOrDefault(x => x.Name == firstPair);
                var coin2Result = CoinPairs.FirstOrDefault(x => x.Name == secondPair);
                var coin3Result = CoinPairs.FirstOrDefault(x => x.Name == thirdPair);
                if (coin1Result != null && coin2Result != null && coin3Result != null && coin1Result.AskPrice > 0 && coin2Result.AskPrice > 0 && coin3Result.AskPrice > 0)
                {
                    decimal interestValue = 0;
                    try
                    {
                        decimal firstValue = coin1Result.AskPrice;
                        decimal secondValue = coin2Result.BidPrice;
                        decimal thirdValue = coin3Result.BidPrice;
                        interestValue = ((Balance / firstValue) * secondValue * thirdValue) - Balance;
                        //Show pair
                        if (interestValue > 0)
                        {
                            Console.WriteLine(symbolList + " : " + interestValue.ToString());
                        }
                        //Console.WriteLine(symbolList + " : " + interestValue.ToString());
                    }
                    catch
                    {
                        return;
                    }

                    if (interestValue < GreaterValue)
                    {
                        return;
                    }


                    Console.WriteLine("\nBuying................" + symbolList + ": " + interestValue.ToString());

                    var isTraded = await TradeCoin(firstPair, secondPair, thirdPair, symbolList);
                }
                //}
                //};
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR: ----" + ex.Message);
                return;
            }
        }

        private async Task<bool> TradeCoin(string pair1Name, string pair2Name, string pair3Name, string symbolList)
        {
            try
            {
                SpotAccountTrade spotAccountTrade = new SpotAccountTrade(
                    new HttpClient(),
                    apiKey: apiKey,
                    apiSecret: apiSecret);

                Market market = new Market(
                    new HttpClient(),
                    apiKey: apiKey,
                    apiSecret: apiSecret);

                var exchangeInfoResult = await market.ExchangeInformation(symbols: symbolList);
                ExchangeRecivedModel exchangeInfos = JsonConvert.DeserializeObject<ExchangeRecivedModel>(exchangeInfoResult);
                var resultPair1 = await spotAccountTrade.NewOrder(pair1Name, Side.BUY, OrderType.MARKET, quoteOrderQty: Balance);
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

            return ((int)(quantity / lotSize.MinQty) * lotSize.MinQty) / 1.000000000000000000000000000000000m;
        }

        private double GetPercent(long count, long total)
        {
            if (count == 0 || total == 0)
                return 0;
            return Math.Round((double)((double)count / (double)total) * 100, 0);
        }
    }
}