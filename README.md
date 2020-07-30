# BitFlyerDotNet
[日本語](README.ja-JP.md)  
BitFlyerDotNet is [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) wrapper and libraries for .NET Standard 2.0.

**BitFlyerDotNet is NOT official library for bitFlyer Lightning APIs.**

### Updates
- Version 3.0.0 - [BitFlyerDotNet Site](https://scrapbox.io/BitFlyerDotNet/Updates)

### Environment 
- Solution and Projects are for Visual Studio 2019 and 2019 for Mac.
- .NET Standard 2.x for libraries.
- .NET Framework 4.71, .NET Core 3.0 and Xamarin Forms for sample applications.
- Sample applications are tested on iOS, Android, MacOS and Windows desktop. 
- [Reactive Extensions (Rx.NET)](http://reactivex.io/)
- [JSON.NET](https://www.newtonsoft.com/json)
- Entity Framework Core and [SQLite](https://www.sqlite.org/index.html)

### BitFlyerDotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.LightningApi
```
- bitFlyer Lightning API wrapper class library.
- Supports all of Public/Private/Realtime APIs.
- [Realtime APIs](https://scrapbox.io/BitFlyerDotNet/Realtime_APIs) are wrapped by Reactive Extensions.
- All of Realtime API subscribers (include private) share single WebSocket channel.
### BitFlyerDotNet.Trading
- BitFlyerDotNet.Trading contains BitFlyer.DotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.Trading
```
- Class library for trading applications
- Quick confirm executed order
- Error retry, safe cancel, prevent order duplication 
### BitFlyerDotNet.Historical
- BitFlyerDotNet.Historical contains BitFlyer.DotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.Historical
```
- Class library for charting applications
- Smart cache mechanism with Reactive Extensions and Entity Framework Core
- Realtime updating OHLC stream with execution cache
- Supports Cryptowatch API with cache

## Sample code

### Realtime API Public Channels
```
using BitFlyerDotNet.LightningApi;

// Display realtime executions from WebSocket
using (var factory = new RealtimeSourceFactory())
using (var source = factory.GetExecutionSource(BfProductCode.FXBTCJPY))
{
    source.Subscribe(exec =>
    {
        Console.WriteLine("{0} {1} {2} {3} {4} {5}",
            exec.ExecutionId,
            exec.Side,
            exec.Price,
            exec.Size,
            exec.ExecutedTime.ToLocalTime(),
            exec.ChildOrderAcceptanceId);
    });
    Console.ReadLine();
}
```
### Realtime API Private Channels
```
using BitFlyerDotNet.LightningApi;

// Input API key and secret
Console.Write("Key:"); var key = Console.ReadLine();
Console.Write("Secret:"); var secret = Console.ReadLine();

// Display child order event from WebSocket
using (var factory = new RealtimeSourceFactory(key, secret))
using (var source = factory.GetChildOrderEventsSource(BfProductCode.FXBTCJPY))
{
    source.Subscribe(e =>
    {
        Console.WriteLine($"{e.EventDate} {e.EventType}");
    });
    Console.ReadLine();
}
```
### Public API
```
using BitFlyerDotNet.LightningApi;

// Get supported currency pairs and aliases
using (var client = new BitFlyerClient())
{
    var resp = client.GetMarkets();
    if (resp.IsError)
    {
        Console.WriteLine("Error occured:{0}", resp.ErrorMessage);
    }
    else
    {
        foreach (var market in resp.GetMessage())
        {
            Console.WriteLine("{0} {1}", market.ProductCode, market.Alias);
        }
    }
}
```
### Private API  
```
using BitFlyerDotNet.LightningApi;

// Buy order
Console.Write("Key:"); var key = Console.ReadLine();
Console.Write("Secret:"); var secret = Console.ReadLine();

using (var client = new BitFlyerClient(key, secret))
{
    Console.Write("Price:"); var price = decimal.Parse(Console.ReadLine());
    client.SendChildOrder(BfProductCode.FXBTCJPY, BfOrderType.Limit, BfTradeSide.Buy, price, 0.001);
}
```
[More sample code from here ->](https://scrapbox.io/BitFlyerDotNet/Samples)

## Known issues
- 2020/07/27 GetParentOrders API is too slow or sometimes returns "Internal Server Error" if target period contains old order. Probably old parent orders are stored another slow database.

Let me know if you have any questions or requests. We could accept English and Japanese.

Fiats Inc.  
<https://www.fiats.asia/>  
Located in Tokyo, Japan.
