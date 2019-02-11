//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Xamarin.Forms;
using BinTrade.ViewModels;

namespace BinTrade
{
    public partial class MainPage : ContentPage
    {
        MainViewModel _vm = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
            this.BindingContext = _vm;
        }

        private void OnCallClicked(object sender, EventArgs e)
        {
            _vm.CallOrder();
        }

        private void OnPutClicked(object sender, EventArgs e)
        {
            _vm.PutOrder();
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            _vm.CloseTrade();
        }
    }
}

