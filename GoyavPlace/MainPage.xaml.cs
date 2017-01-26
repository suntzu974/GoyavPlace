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
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
// Facebook ID : 1562501297351445

//                        item.filename = "toto.jpg";
//                    item.picture = "toto.jpg";
//                    item.file = base64Content;
namespace GoyavPlace
{
    [DataContract]
    public class PictureData
    {
        [DataMember(Name = "picture")]
        public string picture { get; set; }
        [DataMember(Name = "filename")]
        public string filename { get; set; }
        [DataMember(Name = "file")]
        public string file { get; set; }
    }
    [DataContract]
    public class Picture : PictureData
    {
        [DataMember(Name = "id")]
        public string id { get; set; }
        [DataMember(Name = "encoded")]
        public string encoded { get; set; }
        public BitmapImage base64Decoded { get; set; }
    }
    [DataContract]
    public class LocationData
    {
        [DataMember(Name = "latitude")]
        public double latitude { get; set; }
        [DataMember(Name = "longitude")]
        public double longitude { get; set; }
        [DataMember(Name = "country")]
        public string country { get; set; }
        [DataMember(Name = "town")]
        public string town { get; set; }
        [DataMember(Name = "address")]
        public string address { get; set; }
    }
    [DataContract]
    public class EvaluationData
    {
        [DataMember(Name = "id")]
        public int id { get; set; }
        [DataMember(Name = "price")]
        public int price { get; set; }
        [DataMember(Name = "service")]
        public int service { get; set; }
        [DataMember(Name = "quality")]
        public int quality { get; set; }
        [DataMember(Name = "like")]
        public int like { get; set; }
    }
    [DataContract]
    public class CategoryData
    {
        [DataMember(Name = "id")]
        public int id { get; set; }
        [DataMember(Name = "category")]
        public int category { get; set; }
    }

    [DataContract]
    public class PlaceData
    {
        [DataMember(Name = "id")]
        public int id { get; set; }
        [DataMember(Name = "name")]
        public string name { get; set; }
        [DataMember(Name = "phone")]
        public string phone { get; set; }
        [DataMember(Name = "api_key_id")]
        public int api_key_id { get; set; }
        [DataMember(Name = "pictures")]
        public PictureData[] Pictures { get; set; }
        [DataMember(Name = "location")]
        public LocationData Location { get; set; }
        [DataMember(Name = "picture")]
        public Picture Picture { get; set; }
        [DataMember(Name = "evaluation")]
        public EvaluationData Evaluation { get; set; }
        [DataMember(Name = "category")]
        public CategoryData Category { get; set; }
        [DataMember(Name = "distance")]
        public double distance { get; set; }

    }

    [DataContract]
    public class ResponseData
    {
        [DataMember(Name = "success")]
        public string success { get; set; }
        [DataMember(Name = "status")]
        public string status { get; set; }
        [DataMember(Name = "places")]
        public PlaceData[] Places { get; set; }
    }
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
        }

        private async void searchPlace(object sender, KeyRoutedEventArgs e)
        {
            // Search place
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                progress.IsActive = true;
                try
                {
                    //Create HttpClient
                    HttpClient httpClient = new HttpClient();
                    //Define Http Headers
                    httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                    //Call
                    string resourceAddress = string.Format("https://www.goyav.com/api/v2/search_by_name.json?search_by_name={0}", this.PlaceForSearch.Text.ToUpper());
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage wcfResponse = await httpClient.GetAsync(resourceAddress);
                    var responseString = await wcfResponse.Content.ReadAsStringAsync();
                    //Replace current URL with your URL
                    ResponseData data = JsonConvert.DeserializeObject<ResponseData>(responseString);
                    if (data.Places.Count() > 0)
                    {
                        placeList.Visibility = Visibility.Visible;
                        foreach (var place in data.Places)
                        {
                            // item.distance = distance(latitude, longitude, item.latitude, item.longitude, 'K');
                            place.Picture.base64Decoded = getImage(place.Picture.encoded);
                            placeList.Items.Add(place);
                        }
                    }
                    else
                    {
                        // 
                    }
                    if (wcfResponse.IsSuccessStatusCode)
                    { progress.IsActive = false; }
                    #if DEBUG
                        System.Diagnostics.Debug.WriteLine("GET Response !!" + responseString.ToString());
                        System.Diagnostics.Debug.WriteLine("GET Response status code " + wcfResponse.StatusCode);
                    #endif
                }

                catch (Exception ex)
                {
                    //....
                }

            }
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

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts decimal degrees to radians             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts radians to decimal degrees             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }
        private void getPlace(object sender, ItemClickEventArgs e)
        {
            clicked_place = e.ClickedItem as PlaceData;
            System.Diagnostics.Debug.WriteLine("GET Place " + clicked_place.name.ToString() + " and Location" + clicked_place.Location.address.ToString());
            // Navigate to cocktail page with item you click/tap on
            Frame.Navigate(typeof(DetailPage), e.ClickedItem);
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

        private void showCriteria(object sender, RoutedEventArgs e)
        {
            searchPanel.Visibility = Visibility.Visible;
        }


        private void searchForDistance(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            displayDistance.Text = "Distance : " + slider.Value.ToString("n2") + " M";
            distance_from = (float)slider.Value;
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
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void searchPlaces(object sender, RoutedEventArgs e)
        {
            List<int>  categories = new List<int>();
            double latitude = 0;
            double longitude = 0;
            searchPanel.Visibility = Visibility.Collapsed;
            placeList.Visibility = Visibility.Collapsed;
            placeList.Items.Clear();
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
                        NotifyUser("Waiting for update...", NotifyType.StatusMessage);
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

            System.Diagnostics.Debug.WriteLine("distance !!" + searchDistance.Value.ToString());

            searchPanel.Visibility = Visibility.Collapsed;
            placeList.Visibility = Visibility.Visible;
            placeList.Items.Clear();
            // Search place
                try
                {
                progress.IsActive = true;
                //Create HttpClient
                HttpClient httpClient = new HttpClient();
                    //Define Http Headers
                    httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                //Call
                //string resourceAddress = string.Format("https://www.goyav.com/api/v2/search_by_name.json?search_by_name={0}", this.PlaceForSearch.Text.ToUpper());
                string resourceAddress = string.Format("https://www.goyav.com/api/v2/search.json?latitude={0}&longitude={1}&distance={2}&category={3}", latitude, longitude, (float)searchDistance.Value * 0.001 , string.Join(", ", categories));

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage wcfResponse = await httpClient.GetAsync(resourceAddress);
                    var responseString = await wcfResponse.Content.ReadAsStringAsync();
                    //Replace current URL with your URL
                    ResponseData data = JsonConvert.DeserializeObject<ResponseData>(responseString);
                    if (data.Places.Count() > 0)
                    {
                    placeList.Visibility = Visibility.Visible;
                        foreach (var place in data.Places)
                        {
                            // item.distance = distance(latitude, longitude, item.latitude, item.longitude, 'K');
                            place.Picture.base64Decoded = getImage(place.Picture.encoded);
                            place.distance = distance(latitude, longitude, place.Location.latitude, place.Location.longitude, 'K')*1000.00;
                            placeList.Items.Add(place);
                        }
                    }
                    else
                    {
                        // 
                    }
                    if (wcfResponse.IsSuccessStatusCode)
                    {
                    progress.IsActive = false; }
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("GET Response !!" + responseString.ToString());
                    System.Diagnostics.Debug.WriteLine("GET Response status code " + wcfResponse.StatusCode);
                    #endif
                }

                catch (Exception ex)
                {
                    //....
                }
        }
    }
}
