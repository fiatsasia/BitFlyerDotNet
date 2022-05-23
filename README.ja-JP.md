# BitFlyerDotNet
[English](README.md)  

BitFlyerDotNet は、.NET Standard 2.0 向け [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) ラッパーおよび周辺ライブラリです。

**BitFlyerDotNet は bitFlyer Lightning API の公式ライブラリではありません。**

**[BitFlyerDotNet日本語版解説サイト](https://scrapbox.io/BitFlyerDotNet/)**

### 更新履歴
- [BitFlyerDotNetサイト](https://scrapbox.io/BitFlyerDotNet/更新履歴)

### 環境
- Visual Studio 2022 / for Mac 2019 用ソリューション、プロジェクト
- .NET Standard 2.0 (ライブラリで使用)
- .NET 6.0 (サンプルで使用)
- [Reactive Extensions (Rx.NET)](http://reactivex.io/)
- [JSON.NET](https://www.newtonsoft.com/json)
- Entity Framework Core and [SQLite](https://www.sqlite.org/index.html)

### BitFlyerDotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.LightningApi
```
- bitFlyer Lightning API ラッパ
- Public/Private/Realtime 全 API をサポート
- [Realtime APIs](https://scrapbox.io/BitFlyerDotNet/Realtime_APIs) は Reactive Extensions 形式
### BitFlyerDotNet.Trading
- BitFlyerDotNet.Trading は BitFlyer.DotNet.LightningAPI を含みます。
```
PM> Install-Package BitFlyerDotNet.Trading
```
- 取引アプリケーション構築用クラスライブラリ
- トランザクションベースの取引管理
### BitFlyerDotNet.Historical
- BitFlyerDotNet.Historical は BitFlyer.DotNet.LightningAPI を含みます。
```
PM> Install-Package BitFlyerDotNet.Historical
```
- チャートアプリケーション構築用クラスライブラリ
- Reactive Extensions と Entity Framework Core によるスマートキャッシュ
- 四本値のリアルタイム更新とストリーミング
- Cryptowatch API のサポート、キャッシュ
## サンプルコード
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
[その他サンプルコードはこちら→](https://scrapbox.io/BitFlyerDotNet/Samples)


## 既知の問題

- 2020/07/27 GetParentOrders API が、取得対象に一定以上古いレコードを含む場合、"Internal Server Error" を返したり、情報取得までに10秒以上の時間がかかる場合が確認されています。

質問やリクエストがあればお気軽にお知らせください。

Fiats Inc.  
<https://www.fiats.asia/>  
Located in Tokyo, Japan.

