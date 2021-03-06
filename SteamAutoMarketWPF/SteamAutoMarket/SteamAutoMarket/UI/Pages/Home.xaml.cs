﻿namespace SteamAutoMarket.UI.Pages
{
    using System.Diagnostics;
    using System.Windows;
    using SteamAutoMarket.AutoUpdater;
    using SteamAutoMarket.UI.Utils;

    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home
    {
        public Home()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private void Documentation_OnClick(object sender, RoutedEventArgs e) =>
            Process.Start("https://docs.google.com/document/d/17qZ8Y8wY60dLMxYyNM2kZE13JrU_iNRBHK8oOgVZgvM");

        private void LicenseStatus_OnClick(object sender, RoutedEventArgs e) =>
            AppUtils.OpenTab("/UI/Pages/Settings/License.xaml");

        private void SteamLogin_OnClick(object sender, RoutedEventArgs e) =>
            AppUtils.OpenTab("/UI/Pages/SteamAccountLogin.xaml");

        private void Telegram_OnClick(object sender, RoutedEventArgs e) => Process.Start("https://t.me/steamsolutions");

        private void Update_OnClick(object sender, RoutedEventArgs e) => AutoUpdater.ShowUpdateForm();

        private void Website_OnClick(object sender, RoutedEventArgs e) => Process.Start("http://shamanovski.pythonanywhere.com/");
    }
}