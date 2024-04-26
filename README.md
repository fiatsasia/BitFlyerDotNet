# BitFlyerDotNet
[English](README.en-US.md)  

BitFlyerDotNet は、.NET Standard 2.0 向け [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) ラッパーおよび周辺ライブラリです。

**BitFlyerDotNet は bitFlyer Lightning API の公式ライブラリではありません。**

**[BitFlyerDotNet日本語版解説サイト](https://scrapbox.io/BitFlyerDotNet/)**

## サンプルコード
### Realtime API Public Channels
```
// WebSocketからリアルタイム執行情報を取得する。
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
// WebSocketからリアルタイム子注文イベント情報を取得する。
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
// マーケット一覧を取得する。
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
// 成行買注文を送信する。
using (var client = new BitFlyerClient(key, secret))
{
    await client.SendChildOrderAsync(BfOrderFactory.Market("FX_BTC_JPY", BfTradeSide.Buy, 0.001m));
}
```

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

[その他サンプルコードはこちら→](https://scrapbox.io/BitFlyerDotNet/Samples)


質問やリクエストがあればお気軽にお知らせください。

Fiats Inc.  
<https://www.fiats.jp/>  
Located in Tokyo, Japan.
