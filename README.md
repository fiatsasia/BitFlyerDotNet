# BitFlyerDotNet
[日本語](README.ja-JP.md)  
BitFlyerDotNet is [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) wrapper and libraries for .NET Standard 2.0.

**BitFlyerDotNet is NOT official library for bitFlyer Lightning APIs.**

### Updates
- 2019/06/02
  - Added GetOrderBookSource() to RealtimeSource. This functionality provides order book feed which integrates by BoardSnapshot and Board realtime APIs. [See sample code.](Samples/RealtimeApiSample/Program.cs)
  - Some of property definitions are changed. (ex. Status -> State)
  - BitflyerDotNet.Trading is revised. [Check changes from here](Samples/TradingApiSample/Program.cs)
  - Some of functionality was moved to [Financial.Extensions](https://github.com/fiatsasia/Financial.Extensions).
  - Xamarin application samples are removed to shorten time of build solution.
- 2019/05/12
  - All of prices and sizes are changed definition type from double to decimal.
  - Changed build environment Visual Studio from 2017 to 2019.
- 2019/03/31
  - Added hot/cold start option to RealtimeExecutionSource and chaged default to hot (hot start on subscribe).
- 2019/03/21
  - Added BTCUSD and BTCEUR support in realtime ticker API. 

### Environment 
- Solution and Projects are for Visual Studio 2019 and 2019 for Mac.
- .NET Standard 2.0 for libraries.
- .NET Framework 4.71, .NET Core 2.1 and Xamarin Forms for sample applications.
- Sample applications are tested on iOS, Android, MacOS and Windows desktop. 
- [Reactive Extensions (Rx.NET)](http://reactivex.io/)
- [JSON.NET](https://www.newtonsoft.com/json)
- Entity Framework Core and SQLite

### BitFlyerDotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.LightningApi
```
- bitFlyer Lightning API wrapper class library
- Supports all of Public/Private/Realtime APIs
- Realtime APIs are wrapped by Reactive Extensions
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

### Realtime API
[To see sample console application from here.](Samples/RealTimeApiSample/Program.cs)
```
using BitFlyerDotNet.LightningApi;

// Display realtime executions from WebSocket
var factory = new RealtimeSourceFactory();
factory.GetExecutionSource(BfProductCode.FXBTCJPY).Subscribe(exec =>
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
```
### Public API
```
using BitFlyerDotNet.LightningApi;

// Get supported currency pairs and aliases
var client = new BitFlyerClient();
var resp = client.GetMarkets();
if (resp.IsError)
{
    Console.WriteLine("Error occured:{0}", resp.ErrorMessage);
}
else
{
    foreach (var market in resp.GetResult())
    {
        Console.WriteLine("{0} {1}", market.ProductCode, market.Alias);
    }
}
```
### Private API  
```
using BitFlyerDotNet.LightningApi;

// Buy order
Console.Write("Key:"); var key = Console.ReadLine();
Console.Write("Secret:"); var secret = Console.ReadLine();
var client = new BitFlyerClient(key, secret);

Console.Write("Price:"); var price = decimal.Parse(Console.ReadLine());
client.SendChildOrder(BfProductCode.FXBTCJPY, BfOrderType.Limit, BfTradeSide.Buy, price, 0.001);
```
## Sample applications

### Realtime API Sample
- .NET Core console application.
[To see sample application from here.](Samples/RealtimeApiSample/Program.cs)

### Trading API Sample
- .NET Core console application.
[To see sample application from here.](Samples/TradingApiSample/Program.cs)

## Known issues

- Private API getcollateralhistory always returns InternalServerError.

Let me know if you have any questions or requests. We could accept English and Japanese.

Fiats Inc.  
<https://www.fiats.asia/>  
Located in Tokyo, Japan.
