//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Xamarin.Forms;
using SFDTicker.ViewModels;

namespace SFDTicker
{
    public partial class MainPage : ContentPage
    {
        MainViewModel _vm = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();

            this.BindingContext = _vm;
        }
    }
}
