

using BitFlyerDotNet.LightningApi;
using Newtonsoft.Json;
using System.Xml.Linq;

using static System.Formats.Asn1.AsnWriter;

var properties = XDocument.Load(args[0]).Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
var key = properties["ApiKey"];
var secret = properties["ApiSecret"];

static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
const char ESCAPE = (char)0x1b;

using (var client = new BitFlyerClient(key, secret))
{
    while (true)
    {
        try
        {
            Console.WriteLine("===================================================================");
            Console.WriteLine("1) Balance history");
            Console.WriteLine("2) Child orders");
            Console.WriteLine("3) Coin ins");
            Console.WriteLine("4) Coin outs");
            Console.WriteLine("5) Collateral history");
            Console.WriteLine("6) Deposits");
            Console.WriteLine("7) Parent order");
            Console.WriteLine("8) Private executions");
            Console.WriteLine("9) Withdrawrals");
            Console.Write(">");
            switch (GetCh())
            {
                case '1':
                    await foreach (var element in client.GetBalanceHistoryAsync(BfCurrencyCode.JPY, 0, 0, 0, null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case '2':
                    await foreach (var element in client.GetChildOrdersAsync(BfProductCode.FX_BTC_JPY, BfOrderState.Unknown, 10, 0, 0, "", "", "", null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case '3':
                    await foreach (var element in client.GetCoinInsAsync(0, 0, 0, null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case '4':
                    await foreach (var element in client.GetCoinOutsAsync(0, 0, 0, null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case '5':
                    await foreach (var element in client.GetCollateralHistoryAsync(10, 0, 0, null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case '6':
                    await foreach (var element in client.GetDepositsAsync(0, 0, 0, null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case '7':
                    await foreach (var element in client.GetParentOrdersAsync(BfProductCode.FX_BTC_JPY, BfOrderState.Unknown, 100, 0, 0, null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case '8':
                    await foreach (var element in client.GetPrivateExecutionsAsync(BfProductCode.FX_BTC_JPY, 100, 0, 0, "", "", null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case '9':
                    await foreach (var element in client.GetWithdrawalsAsync("", 0, 0, 0, null, CancellationToken.None))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
                    break;

                case ESCAPE:
                    return;
            }
        }
        catch (Exception ex)
        {

        }
    }
}
