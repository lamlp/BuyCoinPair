
using BuyCoinPair.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace BuyCoinPair
{
    class Program
    {
        private List<CoinModel> CoinList = new List<CoinModel>();
        private static decimal Balance = 100;
        public static void Main(string[] args) => new Program().InitData().GetAwaiter();

        private async Task InitData()
        {
            await LoadJson();
            while (true)
            {
                await CheckCoin();
                Thread.Sleep(2000);
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
            CoinList = items;
        }

        private async Task CheckCoin()
        {
            List<CoinModel> coinListResult = new List<CoinModel>();

            foreach (var coin in CoinList)
            {
                string firstPair = coin?.Pair?[0]?.Name ?? string.Empty;
                string secondPair = coin?.Pair?[1]?.Name ?? string.Empty;
                string thirdPair = coin?.Pair?[2]?.Name ?? string.Empty;

                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri("https://api.binance.com/");

                client.DefaultRequestHeaders.Clear();

                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PostmanRuntime/7.28.2");

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string symbolList = $"[\"{firstPair}\",\"{ secondPair }\",\"{ thirdPair}\"]";
                HttpResponseMessage response = client.GetAsync($"api/v3/ticker/price?symbols={symbolList}").Result;
                if (response.IsSuccessStatusCode)
                {
                    string contents = await response.Content.ReadAsStringAsync();
                    List<BinanceDataReceivedModel>? coinResults = JsonConvert.DeserializeObject<List<BinanceDataReceivedModel>>(contents);
                    client.Dispose();
                    if (coinResults != null)
                    {
                        decimal firstValue = coinResults.FirstOrDefault(x => x.Symbol == firstPair).Price;
                        decimal secondValue = coinResults.FirstOrDefault(x => x.Symbol == secondPair).Price;
                        decimal thirdValue = coinResults.FirstOrDefault(x => x.Symbol == thirdPair).Price;
                        var interestValue = ((Balance/firstValue)*secondValue*thirdValue)-Balance;
                        Console.WriteLine("interestValue of " + symbolList + ": " + interestValue.ToString());
                        // Todo
                        // await TradeCoin();
                    }
                }
                client.Dispose();
            }
        }

        private async Task TradeCoin()
        {
            // Todo
        }
    }
}