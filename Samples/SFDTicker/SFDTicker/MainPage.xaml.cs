using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            priceFXBTCJPY.BindingContext = _vm;
            sFDDifference.BindingContext = _vm;
            priceBTCJPY.BindingContext = _vm;
            sFDRate.BindingContext = _vm;
            exchangeStatus.BindingContext = _vm;
        }
    }
}
