# BitFlyerDotNet
[日本語](README.md)  
BitFlyerDotNet is [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) wrapper and libraries for .NET Standard 2.0.

**BitFlyerDotNet is NOT official library for bitFlyer Lightning APIs.**

## Sample code
### Realtime API Public Channels
```
// Display realtime executions from WebSocket
using (var factory = new RealtimeSourceFactory())
using (var source = factory.GetExecutionSource("FX_BTC_JPY"))
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
using (var factory = new RealtimeSourceFactory(key, secret))
using (var source = factory.GetChildOrderEventsSource("FX_BTC_JPY"))
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
// Get supported currency pairs and aliases
using (var client = new BitFlyerClient())
{
    foreach (var market in await client.GetMarketsAsync())
    {
        Console.WriteLine("{0} {1}", market.ProductCode, market.Alias);
    }
}
```
### Private API  
```
// Market buy order
using (var client = new BitFlyerClient(key, secret))
{
    await client.SendChildOrderAsync("FX_BTC_JPY", BfOrderType.Market, BfTradeSide.Buy, 0.001);
}
```
[More sample code from here ->](https://scrapbox.io/BitFlyerDotNet/Samples)

### Updates
- [BitFlyerDotNet Site](https://scrapbox.io/BitFlyerDotNet/Updates)

### Environment 
- Solution and Projects are for Visual Studio 2022 and 2019 for Mac.
- .NET Standard 2.0 for libraries.
- .NET 6.0 for sample applications.
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
- Class library for trading applications.
- Transaction based order management. 
### BitFlyerDotNet.Historical
- BitFlyerDotNet.Historical contains BitFlyer.DotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.Historical
```
- Class library for charting applications
- Smart cache mechanism with Reactive Extensions and Entity Framework Core
- Realtime updating OHLC stream with execution cache
- Supports Cryptowatch API with cache

## Known issues
- 2020/07/27 GetParentOrders API is too slow or sometimes returns "Internal Server Error" if target period contains old order. Probably old parent orders are stored another slow database.

Let me know if you have any questions or requests. We could accept English and Japanese.

Fiats Inc.  
<https://www.fiats.asia/>  
Located in Tokyo, Japan.
