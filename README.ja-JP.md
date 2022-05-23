# BitFlyerDotNet
[English](README.md)  

BitFlyerDotNet �́A.NET Standard 2.0 ���� [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) ���b�p�[����ю��Ӄ��C�u�����ł��B

**BitFlyerDotNet �� bitFlyer Lightning API �̌������C�u�����ł͂���܂���B**

**[BitFlyerDotNet���{��ŉ���T�C�g](https://scrapbox.io/BitFlyerDotNet/)**

### �X�V����
- [BitFlyerDotNet�T�C�g](https://scrapbox.io/BitFlyerDotNet/�X�V����)

### ��
- Visual Studio 2022 / for Mac 2019 �p�\�����[�V�����A�v���W�F�N�g
- .NET Standard 2.0 (���C�u�����Ŏg�p)
- .NET 6.0 (�T���v���Ŏg�p)
- [Reactive Extensions (Rx.NET)](http://reactivex.io/)
- [JSON.NET](https://www.newtonsoft.com/json)
- Entity Framework Core and [SQLite](https://www.sqlite.org/index.html)

### BitFlyerDotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.LightningApi
```
- bitFlyer Lightning API ���b�p
- Public/Private/Realtime �S API ���T�|�[�g
- [Realtime APIs](https://scrapbox.io/BitFlyerDotNet/Realtime_APIs) �� Reactive Extensions �`��
### BitFlyerDotNet.Trading
- BitFlyerDotNet.Trading �� BitFlyer.DotNet.LightningAPI ���܂݂܂��B
```
PM> Install-Package BitFlyerDotNet.Trading
```
- ����A�v���P�[�V�����\�z�p�N���X���C�u����
- �g�����U�N�V�����x�[�X�̎���Ǘ�
### BitFlyerDotNet.Historical
- BitFlyerDotNet.Historical �� BitFlyer.DotNet.LightningAPI ���܂݂܂��B
```
PM> Install-Package BitFlyerDotNet.Historical
```
- �`���[�g�A�v���P�[�V�����\�z�p�N���X���C�u����
- Reactive Extensions �� Entity Framework Core �ɂ��X�}�[�g�L���b�V��
- �l�{�l�̃��A���^�C���X�V�ƃX�g���[�~���O
- Cryptowatch API �̃T�|�[�g�A�L���b�V��
## �T���v���R�[�h
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
[���̑��T���v���R�[�h�͂����灨](https://scrapbox.io/BitFlyerDotNet/Samples)


## ���m�̖��

- 2020/07/27 GetParentOrders API ���A�擾�ΏۂɈ��ȏ�Â����R�[�h���܂ޏꍇ�A"Internal Server Error" ��Ԃ�����A���擾�܂ł�10�b�ȏ�̎��Ԃ�������ꍇ���m�F����Ă��܂��B

����⃊�N�G�X�g������΂��C�y�ɂ��m�点���������B

Fiats Inc.  
<https://www.fiats.asia/>  
Located in Tokyo, Japan.

