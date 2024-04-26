# BitFlyerDotNet
[English](README.en-US.md)  

BitFlyerDotNet �́A.NET Standard 2.0 ���� [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) ���b�p�[����ю��Ӄ��C�u�����ł��B

**BitFlyerDotNet �� bitFlyer Lightning API �̌������C�u�����ł͂���܂���B**

**[BitFlyerDotNet���{��ŉ���T�C�g](https://scrapbox.io/BitFlyerDotNet/)**

## �T���v���R�[�h
### Realtime API Public Channels
```
// WebSocket���烊�A���^�C�����s�����擾����B
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
// WebSocket���烊�A���^�C���q�����C�x���g�����擾����B
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
// �}�[�P�b�g�ꗗ���擾����B
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
// ���s�������𑗐M����B
using (var client = new BitFlyerClient(key, secret))
{
    await client.SendChildOrderAsync(BfOrderFactory.Market("FX_BTC_JPY", BfTradeSide.Buy, 0.001m));
}
```

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

[���̑��T���v���R�[�h�͂����灨](https://scrapbox.io/BitFlyerDotNet/Samples)


����⃊�N�G�X�g������΂��C�y�ɂ��m�点���������B

Fiats Inc.  
<https://www.fiats.jp/>  
Located in Tokyo, Japan.
