# BitFlyerDotNet
BitFlyerDotNet is bitFlyer Lightning API wrapper for .NET framework 4.5.1 or later.

**BitFlyerDotNet is NOT official library for bitFlyer Lightning APIs.**

Supports all of Public/Private/Realtime APIs.  
Supports all type of Realtime API interfaces (PubNub, Socket.IO and WebSocket).  
Realtime APIs are wrapped with Reactive Extensions.

## Sample code

### Realtime API
	// Display realtime executions from PubNub
    var factory = new BitFlyerRealtimeSourceFactory(BfRealtimeSourceKind.PubNub);
    factory.GetExecutionSource(BfProductCode.FXBTCJPY).Subscribe(tick =>
    {
        Console.WriteLine("{0} {1} {2} {3} {4} {5}",
            tick.ExecutionId,
            tick.Side,
            tick.Price,
            tick.Size,
            tick.ExecutedTime.ToLocalTime(),
            tick.ChildOrderAcceptanceId);
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
- Console application
- Trade FX_BTC_JPY, 5/15 minutes Simple Moving Average crossover signal.

### SFDTicker
- WPF application
- Displays FX_BTC_JPY/BTC_JPY rate of variance, SFD rate.
- Displays server health and indicates background color.


## Known issues

- Private API getcollateralhistory always returns InternalServerError.
- Realtime execution ID is often replaced by 0.
- PubNub library ver. 4.0.x does not work. Using 3.8.7. Let me know if you could work
  with recent 4.0.x version.
- PubNub execution tick is sometimes missing.
- WebSocket reconnection does not work correctly.


Let me know if you have any questions or requests. We could accept English and Japanese.

Fiats Inc.  
<http://www.fiats.asia/>  
Located in Tokyo, Japan.
