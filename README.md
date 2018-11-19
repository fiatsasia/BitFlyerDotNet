# BitFlyerDotNet
BitFlyerDotNet is bitFlyer Lightning API wrapper for .NET Standard 2.0.

**BitFlyerDotNet is NOT official library for bitFlyer Lightning APIs.**

Supported platform is changed from .NET Framework to .NET Standard.

Supports all of Public/Private/Realtime APIs.  
Supports Realtime API interfaces (WebSocket).  
 -Socket.IO support was removed because Socket.IO library does not support TLS1.2.  
 -PubNub support was removed because bitFlyer will not support PubNub officially.
Realtime APIs are wrapped with Reactive Extensions.

## Sample code

### Realtime API
	// Display realtime executions from WebSocket
    var factory = new BitFlyerRealtimeSourceFactory();
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
### Public API
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
### Private API  
    // Buy order
    Console.Write("Key:"); var key = Console.ReadLine();
    Console.Write("Secret:"); var secret = Console.ReadLine();
    var client = new BitFlyerClient(key, secret);

    Console.Write("Price:"); var price = double.Parse(Console.ReadLine());
    client.SendChildOrder(BfProductCode.FXBTCJPY, BfOrderType.Limit, BfTradeSide.Buy, price, 0.001);


## Sample applications

### BfAutomatedTradeSample
- Console application (.NET Core 2.0)
- Trade FX_BTC_JPY, 5/15 minutes Simple Moving Average crossover signal.

### SFDTicker
![SFDTickerScreen](https://user-images.githubusercontent.com/39668702/40870381-1b24e4f4-6669-11e8-8498-c6c519d567b2.png)
- WPF application (.NET Framework 4.6.1)
- Displays FX_BTC_JPY/BTC_JPY rate of variance, SFD rate.
- Displays server health and indicates background color.


## Known issues

- Private API getcollateralhistory always returns InternalServerError.

Let me know if you have any questions or requests. We could accept English and Japanese.

Fiats Inc.  
<http://www.fiats.asia/>  
Located in Tokyo, Japan.
