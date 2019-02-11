//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using BitFlyerDotNet.LightningApi;
using BitFlyerDotNet.Trading;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace BinTrade
{
    public partial class App : Application
    {
        public TradingAccount Account { get; private set; } = new TradingAccount(BfProductCode.FXBTCJPY);

        public App()
        {
            switch (Device.RuntimePlatform)
            {
                case Device.Android:
                    break;

                case Device.iOS:
                    break;

                case Device.UWP:
                    break;

                case Device.macOS:
                    break;

                case Device.WPF:
                    break;
            }

            InitializeComponent();

            MainPage = new LoginPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
