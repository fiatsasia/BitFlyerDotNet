//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PCLStorage;

namespace BinTrade
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LoginPage : ContentPage
	{
        const string KeySecretFileName = "key.txt";

		public LoginPage ()
		{
			InitializeComponent ();
            _saveKeySecret.IsToggled = LoadKeySecret();
        }

        bool LoadKeySecret()
        {
            var folder = FileSystem.Current.LocalStorage;
            if (folder.CheckExistsAsync(KeySecretFileName).Result != ExistenceCheckResult.FileExists)
            {
                return false;
            }

            var file = folder.GetFileAsync(KeySecretFileName).Result;
            var text = file.ReadAllTextAsync().Result;
            var keyAndSecret = text.Split(';');
            _apiKey.Text = keyAndSecret[0];
            _apiSecret.Text = keyAndSecret[1];
            return true;
        }

        void SaveKeySecret()
        {
            var folder = FileSystem.Current.LocalStorage;
            var file = default(IFile);
            if (folder.CheckExistsAsync(KeySecretFileName).Result == ExistenceCheckResult.FileExists)
            {
                file = folder.GetFileAsync(KeySecretFileName).Result;
            }
            else
            {
                file = folder.CreateFileAsync(KeySecretFileName, CreationCollisionOption.ReplaceExisting).Result;
            }

            if (string.IsNullOrEmpty(_apiKey.Text) || string.IsNullOrEmpty(_apiSecret.Text))
            {
                file.DeleteAsync();
            }
            else
            {
                file.WriteAllTextAsync(_apiKey.Text + ";" + _apiSecret.Text);
            }
        }

        private void OnLoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_apiKey.Text) || string.IsNullOrEmpty(_apiSecret.Text))
            {
                DisplayAlert("Alert", "API key and secret required.", "OK");
                return;
            }

            var app = App.Current as App;
            app.Account.Login(_apiKey.Text, _apiSecret.Text);

            if (app.Account.ActiveParentOrders.Count > 0 ||
                app.Account.ActiveChildOrders.Count > 0 ||
                app.Account.Positions.Count > 0)
            {
                if (!DisplayAlert("Alert", "Active orders/positions found. Continue?", "Yes", "No").Result)
                {
                    app.Account.Logout();
                    return;
                }
            }

            if (!_saveKeySecret.IsToggled)
            {
                _apiKey.Text = "";
                _apiSecret.Text = "";
            }
            SaveKeySecret();

            app.MainPage = new MainPage();
        }
    }
}