using System.Xml.Linq;
using Newtonsoft.Json;
using BitFlyerDotNet.DataSource;
using BitFlyerDotNet.LightningApi;

var properties = XDocument.Load(args[0]).Element("RunSettings").Element("TestRunParameters").Elements("Parameter").ToDictionary(e => e.Attribute("name").Value, e => e.Attribute("value").Value);
var key = properties["ApiKey"];
var secret = properties["ApiSecret"];

static char GetCh(bool echo = true) { var ch = Char.ToUpper(Console.ReadKey(true).KeyChar); if (echo) Console.WriteLine(ch); return ch; }
const char ESCAPE = (char)0x1b;

using var client = new BitFlyerClient(key, secret);
using var ds = new BfPrivateDataSource(client);

while (true)
{
    try
    {
        Console.WriteLine("===================================================================");
        Console.WriteLine("1) Recent orders");
        Console.Write(">");
        switch (GetCh())
        {
            case '1':
                {
                    var span = DateTime.UtcNow - new DateTime(2020, 11, 20, 0, 0, 0, DateTimeKind.Utc);
                    await foreach (var element in ds.GetRecentOrderContextsAsync(BfProductCode.FX_BTC_JPY, span))
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(element));
                    }
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
