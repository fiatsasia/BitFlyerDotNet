# BitFlyerDotNet
[English](README.md)  

BitFlyerDotNet �́A.NET Standard 2.0 ���� [bitFlyer](https://bitflyer.com/en-jp/) [Lightning API](https://lightning.bitflyer.com/docs?lang=en) ���b�p�[����ю��Ӄ��C�u�����ł��B

**BitFlyerDotNet �� bitFlyer Lightning API �̌������C�u�����ł͂���܂���B**

### �X�V����
- 2019/05/12 ���i�ƃT�C�Y�S�ʂ̌^��`��double����decimal�ɕύX���܂����B
             �r���h���� Visual Studio 2017 ���� 2019 �ɕύX���܂����B
- 2019/03/31 RealtimeExecutionSource��hot/cold�I�v�V������ǉ����A�f�t�H���g��hot(Subscribe�Ɠ����ɊJ�n)�ɕύX���܂����B
- 2019/03/21 Realtime Ticker API �ɁABTCUSD�ABTCEUR �Ή���ǉ����܂����B

### ��
- Visual Studio 2017 / for Mac 2017 �p�\�����[�V�����A�v���W�F�N�g
- .NET Standard 2.0 (���C�u�����Ŏg�p)
- .NET Framework 4.71, .NET Core 2.1, Xamarin Forms (�T���v���Ŏg�p)
- Xamrin Forms �T���v���� iOS, Android, MacOS, Windows �œ���m�F�ς݁B 
- [Reactive Extensions (Rx.NET)](http://reactivex.io/)
- [JSON.NET](https://www.newtonsoft.com/json)
- Entity Framework Core and SQLite

### BitFlyerDotNet.LightningAPI
```
PM> Install-Package BitFlyerDotNet.LightningApi
```
- bitFlyer Lightning API ���b�p
- Public/Private/Realtime �S API ���T�|�[�g
- Realtime API �� Reactive Extensions �Ń��b�v
### BitFlyerDotNet.Trading
- BitFlyerDotNet.Trading �� BitFlyer.DotNet.LightningAPI ���܂݂܂��B
```
PM> Install-Package BitFlyerDotNet.Trading
```
- ����A�v���P�[�V�����\�z�p�N���X���C�u����
- ������̃��A���^�C�����o�@�\
- �G���[���g���C�A�Z�[�t�L�����Z���A��d�����h�~
### BitFlyerDotNet.Historical
- BitFlyerDotNet.Historical �� BitFlyer.DotNet.LightningAPI ���܂݂܂��B
```
PM> Install-Package BitFlyerDotNet.Historical
```
- �`���[�g�A�v���P�[�V�����\�z�p�N���X���C�u����
- Reactive Extensions �� Entity Framework Core �ɂ��X�}�[�g�L���b�V��
- �l�{�l�̃��A���^�C���X�V�ƃX�g���[�~���O
- Cryptowatch API �̃T�|�[�g�A�L���b�V��
## �T���v���A�v���P�[�V����

### SFDTicker
- Xamarin.Forms �A�v���P�[�V����
- WPF/UWP/MacOS/iOS/Android ���T�|�[�g
- FX_BTC_JPY/BTC_JPY �̘������ASFD ���[�g��\�����܂��B
- �T�[�o�[�̃r�W�[��Ԃ�w�i�F�ŕ\�����܂��B  
![SFDTickerScreen](https://i.gyazo.com/74f3e351c2ab5d75785b25db902b81ff.png)

## ���m�̖��

- Private API �� getcollateralhistory ����� InternalServerError ��Ԃ��B

����⃊�N�G�X�g������΂��C�y�ɂ��m�点���������B

Fiats Inc.  
<https://www.fiats.asia/>  
Located in Tokyo, Japan.

