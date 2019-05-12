# BitFlyerDotNet
[English](README.md)  

BitFlyerDotNet は、.NET Standard 2.0 向け [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) ラッパーおよび周辺ライブラリです。

**BitFlyerDotNet は bitFlyer Lightning API の公式ライブラリではありません。**

### 更新履歴
- 2019/05/12 価格とサイズ全般の型定義をdoubleからdecimalに変更しました。
             ビルド環境を Visual Studio 2017 から 2019 に変更しました。
- 2019/03/31 RealtimeExecutionSourceにhot/coldオプションを追加し、デフォルトをhot(Subscribeと同時に開始)に変更しました。
- 2019/03/21 Realtime Ticker API に、BTCUSD、BTCEUR 対応を追加しました。

### 環境
- Visual Studio 2017 / for Mac 2017 用ソリューション、プロジェクト
- .NET Standard 2.0 (ライブラリで使用)
- .NET Framework 4.71, .NET Core 2.1, Xamarin Forms (サンプルで使用)
- Xamrin Forms サンプルは iOS, Android, MacOS, Windows で動作確認済み。 
- [Reactive Extensions (Rx.NET)](http://reactivex.io/)
- [JSON.NET](https://www.newtonsoft.com/json)
- Entity Framework Core and SQLite

### BitFlyerDotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.LightningApi
```
- bitFlyer Lightning API ラッパ
- Public/Private/Realtime 全 API をサポート
- Realtime API は Reactive Extensions でラップ
### BitFlyerDotNet.Trading
- BitFlyerDotNet.Trading は BitFlyer.DotNet.LightningAPI を含みます。
```
PM> Install-Package BitFlyerDotNet.Trading
```
- 取引アプリケーション構築用クラスライブラリ
- 取引約定のリアルタイム検出機能
- エラーリトライ、セーフキャンセル、二重発注防止
### BitFlyerDotNet.Historical
- BitFlyerDotNet.Historical は BitFlyer.DotNet.LightningAPI を含みます。
```
PM> Install-Package BitFlyerDotNet.Historical
```
- チャートアプリケーション構築用クラスライブラリ
- Reactive Extensions と Entity Framework Core によるスマートキャッシュ
- 四本値のリアルタイム更新とストリーミング
- Cryptowatch API のサポート、キャッシュ
## サンプルアプリケーション

### SFDTicker
- Xamarin.Forms アプリケーション
- WPF/UWP/MacOS/iOS/Android をサポート
- FX_BTC_JPY/BTC_JPY の乖離率、SFD レートを表示します。
- サーバーのビジー状態を背景色で表示します。  
![SFDTickerScreen](https://i.gyazo.com/74f3e351c2ab5d75785b25db902b81ff.png)

## 既知の問題

- Private API の getcollateralhistory が常に InternalServerError を返す。

質問やリクエストがあればお気軽にお知らせください。

Fiats Inc.  
<https://www.fiats.asia/>  
Located in Tokyo, Japan.

