# BitFlyerDotNet
[English](README.md)  

BitFlyerDotNet は、.NET Standard 2.0 向け [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) ラッパーおよび周辺ライブラリです。

**BitFlyerDotNet は bitFlyer Lightning API の公式ライブラリではありません。**
### インストール
```
PM> Install-Package BitFlyerDotNet.LightningApi
```


### 環境
- Visual Studio 2017 / for Mac 2017 用ソリューション、プロジェクト
- .NET Standard 2.0 (ライブラリで使用)
- .NET Framework 4.71, .NET Core 2.1, Xamarin Forms (サンプルで使用)
- Xamrin Forms サンプルは iOS, Android, MacOS, Windows で動作確認済み。 
- [Reactive Extensions (Rx.NET)](http://reactivex.io/)
- [JSON.NET](https://www.newtonsoft.com/json)
- Entity Framework Core and SQLite

#### BitFlyerDotNet.LightningAPI
- bitFlyer Lightning API ラッパ
- Public/Private/Realtime 全 API をサポート
- Realtime API は Reactive Extensions でラップ
#### BitFlyerDotNet.Trading
- 取引アプリケーション構築用クラスライブラリ
- 取引約定のリアルタイム検出機能
- エラーリトライ、セーフキャンセル、二重発注防止
#### BitFlyerDotNet.Historical
- チャートアプリケーション構築用クラスライブラリ
- Reactive Extensions と Entity Framework Core によるスマートキャッシュ
- 四本値のリアルタイム更新とストリーミング
- Cryptowatch API のサポート、キャッシュ

## 既知の問題

- Private API の getcollateralhistory が常に InternalServerError を返す。

質問やリクエストがあればお気軽にお知らせください。

Fiats Inc.  
<https://www.fiats.asia/>  
Located in Tokyo, Japan.

