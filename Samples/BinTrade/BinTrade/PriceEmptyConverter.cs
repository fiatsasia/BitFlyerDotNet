//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Globalization;
using Xamarin.Forms;

namespace BinTrade
{
    public class PriceEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var d = (decimal)value;
            var f = ((string)parameter).Replace("{}", "");
            return decimal.Zero == d ? "" : string.Format(f, d);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
