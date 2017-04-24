using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GoyavPlace
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Object mesure = localSettings.Values["unit"];
            Object apiKey = localSettings.Values["auth_token"];
            Object allowDate = localSettings.Values["allowDate"];
            Object allowMeeting = localSettings.Values["allowMeeting"];
            if (mesure != null && ApiKey != null && allowDate != null && allowMeeting != null)
            {
                this.ApiKey.Text = "ApiKey : " + apiKey.ToString();
                if ((bool)mesure)
                    this.unit.IsOn = true;
                if ((bool)allowDate)
                    this.allowDate.IsOn = true;
                if ((bool)allowMeeting)
                    this.allowMeeting.IsOn = true;
            }

        }

        private void changeUnit(object sender, RoutedEventArgs e)
        {
            //
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    // Meter
                    localSettings.Values["unit"] = true;
                }
                else
                {
                    // Kilometer
                    localSettings.Values["unit"] = false;
                }
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Unit :" + unit);
#endif

        }

        private void changeAllowDate(object sender, RoutedEventArgs e)
        {
            //
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    // Meter
                    localSettings.Values["allowDate"] = true;
                }
                else
                {
                    // Kilometer
                    localSettings.Values["allowDate"] = false;
                }
            }
        }

        private void changeAllowMeeting(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    // Meter
                    localSettings.Values["allowMeeting"] = true;
                }
                else
                {
                    // Kilometer
                    localSettings.Values["allowMeeting"] = false;
                }
            }
        }
    }
}
