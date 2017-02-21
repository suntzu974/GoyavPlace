using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using GoyavPlace.ViewModels;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
// Facebook ID : 1562501297351445

//                        item.filename = "toto.jpg";
//                    item.picture = "toto.jpg";
//                    item.file = base64Content;
namespace GoyavPlace
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        PlaceData clicked_place = new PlaceData();
        float distance_from = 0;
        private uint _desireAccuracyInMetersValue = 0;
        private CancellationTokenSource _cts = null;
        public static MainPage Current;
        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            DateTime thisDay = DateTime.Today;
            this.begindate.Date = thisDay;
        }

        // Distance
        private double distance(double lat1, double lon1, double lat2, double lon2, char unit)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if (unit == 'K')
            {
                dist = dist * 1.609344;
            }
            else if (unit == 'N')
            {
                dist = dist * 0.8684;
            }
            return (dist);
        }

        private double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        private double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }
        private void addPlace(object sender, RoutedEventArgs e)
        {
            //
			// Clear the status block when navigating scenarios.
            this.Frame.Navigate(typeof(NewPlacePage));
        }

        private void editSettings(object sender, RoutedEventArgs e)
        { 
            this.Frame.Navigate(typeof(SettingsPage));
        }
        public static BitmapImage getImage(string img)
        {
            var imageBytes = Convert.FromBase64String(img);
            using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(ms.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes((byte[])imageBytes);
                    writer.StoreAsync().GetResults();
                }

                var image = new BitmapImage();
                image.SetSource(ms);
                return image;
            }
        }

        private void searchForDistance(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            distance_from = (float)slider.Value;

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Object unit = localSettings.Values["unit"];
            if (!(bool)unit)
                displayDistance.Text = "Distance : " + slider.Value.ToString("n2") + " M";
            else
                displayDistance.Text = "Distance : " + slider.Value.ToString("n2") + " KM";

            System.Diagnostics.Debug.WriteLine("distance from !!" + distance_from.ToString());
        }

        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                //StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                //StatusPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void searchPlaces(object sender, RoutedEventArgs e)
        {
            List<int>  categories = new List<int>();
            double latitude = 0;
            double longitude = 0;
            String addressForSearch = String.Empty;
            DateTime searchDate = this.begindate.Date.Value.DateTime;

            // Get location
            try
            {
                // Request permission to access location
                var accessStatus = await Geolocator.RequestAccessAsync();

                switch (accessStatus)
                {
                    case GeolocationAccessStatus.Allowed:
                        _cts = new CancellationTokenSource();
                        CancellationToken token = _cts.Token;
                        NotifyUser("Waiting for retrieve current location...", NotifyType.StatusMessage);
                        // If DesiredAccuracy or DesiredAccuracyInMeters are not set (or value is 0), DesiredAccuracy.Default is used.
                        Geolocator geolocator = new Geolocator { DesiredAccuracyInMeters = _desireAccuracyInMetersValue };

                        // Carry out the operation
                        Geoposition pos = await geolocator.GetGeopositionAsync().AsTask(token);
                        latitude = pos.Coordinate.Point.Position.Latitude;
                        longitude = pos.Coordinate.Point.Position.Longitude;

                        NotifyUser("Location updated.", NotifyType.StatusMessage);
                        break;

                    case GeolocationAccessStatus.Denied:
                        NotifyUser("Access to location is denied.", NotifyType.ErrorMessage);
                        //LocationDisabledMessage.Visibility = Visibility.Visible;
                        break;

                    case GeolocationAccessStatus.Unspecified:
                        NotifyUser("Unspecified error.", NotifyType.ErrorMessage);
                        break;
                }
            }
            catch (TaskCanceledException)
            {
                NotifyUser("Canceled.", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
            }
            finally
            {
                _cts = null;
            }


            // Category
            // check
            if (searchStation.IsChecked == true)
                categories.Add(1);
            if (searchLeisure.IsChecked == true)
                categories.Add(2);
            if (searchHotel.IsChecked == true)
                categories.Add(3);
            if (searchRestaurant.IsChecked == true)
                categories.Add(4);

            // Search place
                try
                {
                    progress.IsActive = true;
                    List<PlaceData> ListOfPlaces = new List<PlaceData>();
                    //Create HttpClient
                    HttpClient httpClient = new HttpClient();
                    //Define Http Headers
                    httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    Object unit = localSettings.Values["unit"];
                    if (!(bool)unit)
                        distance_from = distance_from / 1000;

                    if (distance_from == 0)
                        addressForSearch = string.Format("{0}/v2/search.json?latitude={1}&longitude={2}&category={3}&start_date={4}", App.IP_ADDRESS,latitude, longitude, string.Join(", ", categories), searchDate.ToString("yyyy-MM-dd"));
                    else
                        addressForSearch = string.Format("{0}/v2/search.json?latitude={1}&longitude={2}&distance={3}&category={4}&start_date={5}", App.IP_ADDRESS,latitude, longitude, distance_from, string.Join(", ", categories), searchDate.ToString("yyyy-MM-dd"));

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage wcfResponse = await httpClient.GetAsync(addressForSearch);
                    var responseString = await wcfResponse.Content.ReadAsStringAsync();
                    //Replace current URL with your URL
                    ResponseData data = JsonConvert.DeserializeObject<ResponseData>(responseString);
                    if (data.Places.Count() > 0)
                    {
                        foreach (var place in data.Places)
                        {
                            // item.distance = distance(latitude, longitude, item.latitude, item.longitude, 'K');
                            place.Picture.base64Decoded = getImage(place.Picture.encoded);
                            place.distance = distance(latitude, longitude, place.Location.latitude, place.Location.longitude, 'K')*1000.00;
                            ListOfPlaces.Add(place);
                        }
                    }
                    else
                    {
                        // 
                    }
                    if (wcfResponse.IsSuccessStatusCode)
                    {
                        progress.IsActive = false;
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("GET Response !!" + responseString.ToString());
                        System.Diagnostics.Debug.WriteLine("GET Response status code " + wcfResponse.StatusCode);
#endif
                        Frame.Navigate(typeof(MasterDetailPage), ListOfPlaces);
                    }
                }

                catch (Exception ex)
                {
                    //....
                }
        }

        private void manageRates(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(RatePage));
        }
    }
}
