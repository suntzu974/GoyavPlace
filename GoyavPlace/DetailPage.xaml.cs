using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using winsdkfb;
using winsdkfb.Graph;
using static GoyavPlace.MainPage;
using GoyavPlace.ViewModels;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Data;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Media.Capture;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Foundation;
using Windows.ApplicationModel.DataTransfer;
using System.IO;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GoyavPlace
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class DetailPage : Page
    {

        PlaceData place = new PlaceData();
        Evaluation evaluation = new Evaluation();
        CategoryData category = new CategoryData();
        Tweet tweet = new Tweet();
        Photo photo = new Photo();
        SharedPicture shared = new SharedPicture();
        string base64Content = String.Empty;
        private DataTransferManager dataTransferManager;
        public DetailPage()
        {
            this.InitializeComponent();
            this.likeButton.Visibility = Visibility.Collapsed;
            this.refreshButton.Visibility = Visibility.Collapsed;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            dataTransferManager.DataRequested -= DataTransferManager_DataRequested;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            place = e.Parameter as PlaceData;
            #if DEBUG
                System.Diagnostics.Debug.WriteLine("GET Place from parameter :" + place.name.ToString());
            #endif
            this.name.Text = place.name.ToString();
            this.address.Text = place.Location.address.ToString();
            //this.phone.Text = place.phone.ToString();
            this.town.Text = place.Location.town.ToString();
            this.country.Text = place.Location.country.ToString();
            this.geolocation.Text = "Lat:"+place.Location.latitude.ToString("n2")+",Lng :"+place.Location.longitude.ToString("n2");
            this.picture.Source = place.Picture.base64Decoded;
            switch(place.Category.category)
            {
                case 1:
                    // Place
                    this.rbPlace.IsChecked = true;
                    break;
                case 2:
                    // Leisure
                    this.rbLeisure.IsChecked = true;
                    break;
                case 3:
                    // Bed
                    this.rbSleep.IsChecked = true;
                    break;
                case 4:
                    // Eat
                    this.rbEat.IsChecked = true;
                    break;
                default:
                    break;
            }
            // Evaluation.
            try
            {
                this.slPrice.Value = place.Evaluation.price;
                this.slService.Value = place.Evaluation.service;
                this.slQuality.Value = place.Evaluation.quality;
            }
            catch (Exception ex)
            {
                //logging goes here
                System.Diagnostics.Debug.WriteLine("Failled to associate object");
            }
            getTweets();
            getPhotos();
            getShared();
            dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;

        }


        void NavigateBackForWideState(bool useTransition)
        {
            // Evict this page from the cache as we may not need it again.
            NavigationCacheMode = NavigationCacheMode.Disabled;

            if (useTransition)
            {
                Frame.GoBack(new EntranceNavigationTransitionInfo());
            }
            else
            {
                Frame.GoBack(new SuppressNavigationTransitionInfo());
            }
        }

        private bool ShouldGoToWideState()
        {
            return Window.Current.Bounds.Width >= 720;
        }

        private void PageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            if (ShouldGoToWideState())
            {
                // We shouldn't see this page since we are in "wide master-detail" mode.
                // Play a transition as we are navigating from a separate page.
                NavigateBackForWideState(useTransition: true);
            }
            else
            {
                // Realize the main page content.
                FindName("RootPanel");
            }

            Window.Current.SizeChanged += Window_SizeChanged;
        }

        private void PageRoot_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Window_SizeChanged;
        }

        private void Window_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            if (ShouldGoToWideState())
            {
                // Make sure we are no longer listening to window change events.
                Window.Current.SizeChanged -= Window_SizeChanged;

                // We shouldn't see this page since we are in "wide master-detail" mode.
                NavigateBackForWideState(useTransition: false);
            }
        }
        private async void getShared()
        {
            try
            {
                progress.IsActive = true;
                //Create HttpClient
                HttpClient httpClient = new HttpClient();
                //Define Http Headers
                httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                String addressForShared = string.Format("{0}/shared.json?place_id={1}", App.IP_ADDRESS, place.id);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Address shared !!" + addressForShared.ToString());
#endif
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage wcfResponse = await httpClient.GetAsync(addressForShared);
                var responseString = await wcfResponse.Content.ReadAsStringAsync();
#if DEBUG
                System.Diagnostics.Debug.WriteLine("GET Response shared !!" + responseString.ToString());
#endif
                //Replace current URL with your URL
                ResponseShared data = JsonConvert.DeserializeObject<ResponseShared>(responseString);
                shared.shared = data.shared.shared;
                if (wcfResponse.IsSuccessStatusCode)
                {
                    progress.IsActive = false;
                }
            }

            catch (Exception ex)
            {
                //....
            }

        }
        private async void getPhotos()
        {
            photoGrid.Items.Clear();
            try
            {
                progress.IsActive = true;
                //Create HttpClient
                HttpClient httpClient = new HttpClient();
                //Define Http Headers
                httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                String addressForPhotos = string.Format("{0}/search_photos.json?place_id={1}", App.IP_ADDRESS, place.id);

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage wcfResponse = await httpClient.GetAsync(addressForPhotos);
                var responseString = await wcfResponse.Content.ReadAsStringAsync();
                //Replace current URL with your URL
                ResponsePhoto data = JsonConvert.DeserializeObject<ResponsePhoto>(responseString);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Get Response Photos  !!" + responseString.ToString());
#endif
                if (data.photos.Count() > 0)
                {
                    noteGrid.Visibility = Visibility.Visible;
                    foreach (var photo in data.photos)
                    {
                        photo.base64Decoded = getImage(photo.encoded);
                        photoGrid.Items.Add(photo);
                    }
                }
                else
                {
                    // 
                }
                if (wcfResponse.IsSuccessStatusCode)
                {
                    progress.IsActive = false;
                }
            }

            catch (Exception ex)
            {
                //....
            }

        }
        private async void getTweets()
        {
            noteGrid.Items.Clear();
            // Search place
            try
            {
                progress.IsActive = true;
                //Create HttpClient
                HttpClient httpClient = new HttpClient();
                //Define Http Headers
                httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                String addressForTweets = string.Format("{0}/search_tweets.json?place_id={1}", App.IP_ADDRESS, place.id);

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage wcfResponse = await httpClient.GetAsync(addressForTweets);
                var responseString = await wcfResponse.Content.ReadAsStringAsync();
                //Replace current URL with your URL
                ResponseTweet data = JsonConvert.DeserializeObject<ResponseTweet>(responseString);
                if (data.tweets.Count() > 0)
                {
                    noteGrid.Visibility = Visibility.Visible;
                    foreach (var tweet in data.tweets)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Tweets.tweet !!" + tweet.tweet.ToString());
#endif
//                        TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
//                        tweet.updated_at = TimeZoneInfo.ConvertTime(tweet.updated_at, tzi);
                        noteGrid.Items.Add(tweet);
                    }
                }
                else
                {
                    // 
                }
                if (wcfResponse.IsSuccessStatusCode)
                {
                    progress.IsActive = false;
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("GET Response from Tweets !!" + responseString.ToString());
                System.Diagnostics.Debug.WriteLine("GET Response status code " + wcfResponse.StatusCode);
#endif
            }

            catch (Exception ex)
            {
                //....
            }

        }

        private async void getDrive(object sender, RoutedEventArgs e)
        {
            // Drive
            // Get the values required to specify the destination.
            string latitude = place.Location.latitude.ToString();
            string longitude = place.Location.longitude.ToString();
            string name = place.name;

            // Assemble the Uri to launch.
            Uri uri = new Uri("ms-drive-to:?destination.latitude=" + latitude +
                "&destination.longitude=" + longitude + "&destination.name=" + name);
            // The resulting Uri is: "ms-drive-to:?destination.latitude=47.6451413797194
            //  &destination.longitude=-122.141964733601&destination.name=Redmond, WA")

            // Launch the Uri.
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);

            if (success)
            {
                // Uri launched.
            }
            else
            {
                // Uri failed to launch.
            }

        }

        private void getCall(object sender, RoutedEventArgs e)
        {
           // Windows.ApplicationModel.Calls.PhoneCallManager.ShowPhoneCallUI(place.phone.ToString(), place.name.ToString());
        }

        private async void getLike(object sender, RoutedEventArgs e)
        {
            // Like only place despite the place's owner
            string resourceAddress = String.Empty;
            // Save item
            HttpClient httpClient = new HttpClient();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("auth_token"))
            {
                //overwrite the value if you need to
                Object api_token = localSettings.Values["auth_token"];
                Object api_token_id = localSettings.Values["auth_token_id"];
                Dictionary<String, Object> EVALUATION = new Dictionary<String, Object>();

                evaluation.place_id = place.id;
                evaluation.api_key_id = Convert.ToInt16(api_token_id);
                evaluation.like += 1;
                EVALUATION["evaluation"] = evaluation;


                if (place.name != String.Empty &&
                    //place.phone != String.Empty &&
                    place.Location.address != String.Empty &&
                    place.Location.town != String.Empty &&
                    place.Location.country != String.Empty)
                {

                    string postBody = JsonConvert.SerializeObject(EVALUATION, Formatting.Indented);
                    try
                    {
                        if (Convert.ToInt16(api_token_id) == place.api_key_id)
                        {
                            // Update 
                            resourceAddress = string.Format("{0}/evaluations/{1}.{2}", App.IP_ADDRESS,place.Evaluation.id, "json");
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage wcfResponse = await httpClient.PutAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                            var responseString = await wcfResponse.Content.ReadAsStringAsync();
                            AddResponse RetResponse = JsonConvert.DeserializeObject<AddResponse>(responseString);
                            // Navigate to cocktail page with item you click/tap on
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("Response String for updating evaluation  " + responseString.ToString() + "Evaluation :" + postBody);
#endif
                            NotifyUser("Status code for like ." + wcfResponse.StatusCode, NotifyType.StatusMessage);
                        }
                        // Update values
                    }
                    catch (HttpRequestException hre)
                    {
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        if (httpClient != null)
                        {
                            httpClient.Dispose();
                            httpClient = null;
                        }
                    }
                }
                else
                {
                    NotifyUser("all fields must be filled.", NotifyType.ErrorMessage);
                }
            }

        }

        private void slPriceChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider.Value < 2)
            {
                slider.Header = "Low price";
            }
            if (slider.Value == 2)
            {
                slider.Header = "Middle price";
            }
            if (slider.Value > 2)
            {
                slider.Header = "High price";
            }
        }
        private void slQualityChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider.Value < 2)
            {
                slider.Header = "Low quality";
            }
            if (slider.Value == 2)
            {
                slider.Header = "Middle quality";
            }
            if (slider.Value > 2)
            {
                slider.Header = "High quality";
            }
        }
        private void slServiceChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider.Value < 2)
            {
                slider.Header = "Low service";
            }
            if (slider.Value == 2)
            {
                slider.Header = "Middle service";
            }
            if (slider.Value > 2)
            {
                slider.Header = "High service";
            }
        }
        // Notify User

        // Save changes
        private async void saveEvalation()
        {
            string resourceAddress = String.Empty;
            // Save item
            HttpClient httpClient = new HttpClient();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("auth_token"))
            {
                //overwrite the value if you need to
                Object api_token = localSettings.Values["auth_token"];
                Object api_token_id = localSettings.Values["auth_token_id"];
                Dictionary<String, Object> EVALUATION = new Dictionary<String, Object>();

                evaluation.place_id = place.id;
                evaluation.api_key_id = Convert.ToInt16(api_token_id);
                evaluation.price = (int)slPrice.Value;
                evaluation.service = (int)slService.Value;
                evaluation.quality = (int)slQuality.Value;
                EVALUATION["evaluation"] = evaluation;


                if (place.name != String.Empty &&
                    //place.phone != String.Empty &&
                    place.Location.address != String.Empty &&
                    place.Location.town != String.Empty &&
                    place.Location.country != String.Empty)
                {

                    string postBody = JsonConvert.SerializeObject(EVALUATION, Formatting.Indented);
                    try
                    {
                        if (Convert.ToInt16(api_token_id) == place.api_key_id)
                        {
                            // Update 
                            resourceAddress = string.Format("{0}/evaluations/{1}.{2}", App.IP_ADDRESS,place.Evaluation.id, "json");
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage wcfResponse = await httpClient.PutAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                            var responseString = await wcfResponse.Content.ReadAsStringAsync();
                            AddResponse RetResponse = JsonConvert.DeserializeObject<AddResponse>(responseString);
                            // Navigate to cocktail page with item you click/tap on
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("Response String for updating evaluation  " + responseString.ToString() + "Evaluation :" + postBody);
#endif
                            NotifyUser("Status code for evaluation." + wcfResponse.StatusCode, NotifyType.StatusMessage);
                        }
                        else
                        {
                            // Create
                            resourceAddress = string.Format("{0}/evaluations.json",App.IP_ADDRESS);
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage wcfResponse = await httpClient.PostAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                            var responseString = await wcfResponse.Content.ReadAsStringAsync();
                            AddResponse RetResponse = JsonConvert.DeserializeObject<AddResponse>(responseString);
                            NotifyUser("Status code for evaluation ." + wcfResponse.StatusCode, NotifyType.StatusMessage);
                            // Navigate to cocktail page with item you click/tap on
#if DEBUG
                            System.Diagnostics.Debug.WriteLine("Response String for new evaluation  " + responseString.ToString());
#endif

                        }
                        // Update values
                    }
                    catch (HttpRequestException hre)
                    {
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        if (httpClient != null)
                        {
                            httpClient.Dispose();
                            httpClient = null;
                        }
                    }
                }
                else
                {
                    NotifyUser("all fields must be filled.", NotifyType.ErrorMessage);
                }
            }
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

        private void doSave(object sender, RoutedEventArgs e)
        {
            saveEvalation();
        }

        private void getCategory(object sender, RoutedEventArgs e)
        {
            // check
            if (rbPlace.IsChecked == true)
                place.Category.category = 1;
            if (rbLeisure.IsChecked == true)
                place.Category.category = 2;
            if (rbSleep.IsChecked == true)
                place.Category.category = 3;
            if (rbEat.IsChecked == true)
                place.Category.category = 4;
            // Synchronize category 
            updateCategory();
        }

        private async void updateCategory()
        {
            string resourceAddress = String.Empty;
            // Save item
            HttpClient httpClient = new HttpClient();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            //overwrite the value if you need to
            Object api_token = localSettings.Values["auth_token"];
            Object api_token_id = localSettings.Values["auth_token_id"];
            Dictionary<String, Object> CATEGORY = new Dictionary<String, Object>();
            // check
            if (rbPlace.IsChecked == true)
                category.category = 1;
            if (rbLeisure.IsChecked == true)
                category.category = 2;
            if (rbSleep.IsChecked == true)
                category.category = 3;
            if (rbEat.IsChecked == true)
                category.category = 4;


            CATEGORY["category"] = place.Category;

            try
            {
                string postBody = JsonConvert.SerializeObject(CATEGORY, Formatting.Indented);
                // Update 
                resourceAddress = string.Format("{0}/categories/{1}.{2}", App.IP_ADDRESS,place.Category.id, "json");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage wcfResponse = await httpClient.PutAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                var responseString = await wcfResponse.Content.ReadAsStringAsync();
                AddResponse RetResponse = JsonConvert.DeserializeObject<AddResponse>(responseString);
                // Navigate to cocktail page with item you click/tap on
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Response String for updating place  " + responseString.ToString() + " Place :" + postBody);
#endif
                NotifyUser("Status code for place updated ." + wcfResponse.StatusCode, NotifyType.StatusMessage);

            }
            catch (HttpRequestException hre)
            {
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (httpClient != null)
                {
                    httpClient.Dispose();
                    httpClient = null;
                }
            }
        }

        private void getIndexFromPivot(object sender, SelectionChangedEventArgs e)
        {
            var pivot = this.goyavPivot.SelectedIndex;
            if (pivot == 0)
            {
                System.Diagnostics.Debug.WriteLine("Index Home");
                this.likeButton.Visibility = Visibility.Collapsed;
                this.refreshButton.Visibility = Visibility.Collapsed;
            }
            if (pivot == 1)
            {
                System.Diagnostics.Debug.WriteLine("Index Notes");
                this.likeButton.Visibility = Visibility.Collapsed;
                this.refreshButton.Visibility = Visibility.Visible;

            }
            if (pivot == 2)
            {
                System.Diagnostics.Debug.WriteLine("Index Photos");
                this.likeButton.Visibility = Visibility.Visible;
                this.refreshButton.Visibility = Visibility.Collapsed;

            }
            if (pivot == 3)
            {
                System.Diagnostics.Debug.WriteLine("Index Map");
                this.refreshButton.Visibility = Visibility.Collapsed;
                        mapDetail.MapServiceToken = "hhsGCt9NnTPzFSDLqtQE~-lHPBfyxJXelWFsjueAo-w~AuWF2cq1VKSnPzf9w6DtpnSKE20eL8AcuDHVRTJglKs671qn6bU9QVUao4QT_-Js";
                        BasicGeoposition snPosition = new BasicGeoposition() { Latitude = place.Location.latitude, Longitude = place.Location.longitude };
                        Geopoint snPoint = new Geopoint(snPosition);
                        MapIcon myPOI = new MapIcon { Location = snPoint, NormalizedAnchorPoint = new Point(0.5, 1.0), Title = place.name.ToString(), ZIndex = 0 };
                        // add to map and center it
                        mapDetail.MapElements.Add(myPOI);
                        mapDetail.Center = snPoint;
                        // Center the map over the POI.
                        mapDetail.ZoomLevel = 16;
            }
            if (pivot == 4)
            {
                System.Diagnostics.Debug.WriteLine("Index Calendrier");
                this.refreshButton.Visibility = Visibility.Collapsed;

            }

        }
        private async void sendPhoto(object sender, RoutedEventArgs e)
        {
            // httpclient
            // Send Tweet

            string resourceAddress = String.Empty;
            // Save item
            HttpClient httpClient = new HttpClient();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("auth_token"))
            {
                //overwrite the value if you need to
                Object api_token = localSettings.Values["auth_token"];
                Object api_token_id = localSettings.Values["auth_token_id"];
                Dictionary<String, Object> PHOTO = new Dictionary<String, Object>();
                photo.api_key_id = Convert.ToInt16(api_token_id);
                photo.place_id = place.id;
                PHOTO["photo"] = photo;

                if (photo.filename != String.Empty)
                {

                    string postBody = JsonConvert.SerializeObject(PHOTO, Formatting.Indented);
                    try
                    {
                        progress.IsActive = true;

                        //overwrite the value if you need to
                        resourceAddress = string.Format("{0}/photos.json", App.IP_ADDRESS);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage wcfResponse = await httpClient.PostAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                        var responseString = await wcfResponse.Content.ReadAsStringAsync();
                        ResponsePhoto RetResponse = JsonConvert.DeserializeObject<ResponsePhoto>(responseString);
                        // Navigate to cocktail page with item you click/tap on
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Response String for new tweet " + responseString.ToString());
#endif
                        NotifyUser("http response status code ." + wcfResponse.StatusCode, NotifyType.StatusMessage);
                        if (wcfResponse.IsSuccessStatusCode)
                        {
                            progress.IsActive = false;
                            // Clean Fields
                            getPhotos();
                        }
                    }
                    catch (HttpRequestException hre)
                    {
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        if (httpClient != null)
                        {
                            httpClient.Dispose();
                            httpClient = null;
                        }
                    }
                }
                else
                {
                    NotifyUser("all fields must be filled.", NotifyType.ErrorMessage);
                }
            }

        }
        private async void getLibrary(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                var stream = await file.OpenAsync(FileAccessMode.Read);
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                var decoder = await BitmapDecoder.CreateAsync(stream);
                base64Content = Convert.ToBase64String(await CreateScaledImage2(decoder, 1024, 760));
                photo.file = base64Content;
                photo.filename = "goyav.png";
                photo.picture = "goyav.png";
                sendPhoto(sender, e);

            }
            else
            {
                this.geolocation.Text = "Operation cancelled.";
            }

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
        private async void getCamera(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            //captureUI.PhotoSettings.CroppedSizeInPixels = new Size(100, 100);
            StorageFile photoStore = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (photoStore != null)
            {
                // Application now has read/write access to the picked file
                var stream = await photoStore.OpenAsync(FileAccessMode.Read);
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                var decoder = await BitmapDecoder.CreateAsync(stream);
                base64Content = Convert.ToBase64String(await CreateScaledImage2(decoder, 760, 1024));
                //ImageList.Add(new ImageItem(bitmapImage, base64Content));
                photo.file = base64Content;
                photo.filename = "goyav.png";
                photo.picture = "goyav.png";
                sendPhoto(sender, e);
            }
            else
            {
                this.geolocation.Text = "Operation cancelled.";
                // User cancelled photo capture
                return;
            }
        }
        public Task<byte[]> CreateScaledImage2(BitmapDecoder decoder, uint newWidth, uint newHeight)
        {
            return Task.Run<byte[]>(async () =>
            {
                var originalPixelWidth = decoder.PixelWidth;
                var originalPixelHeight = decoder.PixelHeight;

                long start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                InMemoryRandomAccessStream ras = null;
                try
                {

                    ras = new InMemoryRandomAccessStream();
                    BitmapEncoder enc = await BitmapEncoder.CreateForTranscodingAsync(ras, decoder);

                    enc.BitmapTransform.ScaledHeight = newHeight;
                    enc.BitmapTransform.ScaledWidth = newWidth;

                    await enc.FlushAsync();


                    byte[] previewByteArray = new byte[ras.Size];
                    DataReader dataReader = new DataReader(ras.GetInputStreamAt(0));
                    await dataReader.LoadAsync((uint)ras.Size);

                    dataReader.ReadBytes(previewByteArray);
                    return previewByteArray;
                }
                catch (Exception ex)
                {
                    string s = ex.ToString();
                    throw ex;
                }
                finally
                {

                    if (ras != null)
                    {
                        ras.Dispose();
                    }

                }
            });
        }
        private async void sendTweet(object sender, RoutedEventArgs e)
        {
            // Send Tweet

            string resourceAddress = String.Empty;
            // Save item
            HttpClient httpClient = new HttpClient();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("auth_token"))
            {
                //overwrite the value if you need to
                Object api_token = localSettings.Values["auth_token"];
                Object api_token_id = localSettings.Values["auth_token_id"];
                Dictionary<String, Object> TWEET = new Dictionary<String, Object>();
                tweet.tweet = this.noteText.Text.ToString();
                tweet.api_key_id = Convert.ToInt16(api_token_id);
                tweet.place_id = place.id;
                TWEET["tweet"] = tweet;

                if (tweet.tweet != String.Empty)
                {

                    string postBody = JsonConvert.SerializeObject(TWEET, Formatting.Indented);
                    try
                    {
                        //overwrite the value if you need to
                        resourceAddress = string.Format("{0}/tweets.json?place_id={1}?api_key_id={2}&tweet{3}", App.IP_ADDRESS,tweet.place_id, tweet.api_key_id, tweet.tweet);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage wcfResponse = await httpClient.PostAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                        var responseString = await wcfResponse.Content.ReadAsStringAsync();
                        AddResponse RetResponse = JsonConvert.DeserializeObject<AddResponse>(responseString);
                        // Navigate to cocktail page with item you click/tap on
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Response String for new tweet " + responseString.ToString());
#endif
                        NotifyUser("http response status code ." + wcfResponse.StatusCode, NotifyType.StatusMessage);
                        if (wcfResponse.IsSuccessStatusCode)
                        {
                            // Clean Fields
                            this.noteText.Text = String.Empty;
                            getTweets();
                        }
                    }
                    catch (HttpRequestException hre)
                    {
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        if (httpClient != null)
                        {
                            httpClient.Dispose();
                            httpClient = null;
                        }
                    }
                }
                else
                {
                    NotifyUser("all fields must be filled.", NotifyType.ErrorMessage);
                }
            }

        }

        private void refreshNotes(object sender, RoutedEventArgs e)
        {
            getTweets();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            ShareText(request);
        }

        /// <summary>
        /// set text to request data
        /// </summary>
        /// <param name="request"></param>
        private void ShareText(DataRequest request)
        {
            if (this.country.Text.Trim().Equals(string.Empty))
            {
                request.FailWithDisplayText("Enter text you would like to share and try again.");
            }
            else
            {
                Uri uri = new Uri("https://www.goyav.re");
                request.Data.SetBitmap(RandomAccessStreamReference.CreateFromStream(ConvertTo(Convert.FromBase64String(shared.shared))));
                request.Data.SetText(this.name.Text.Trim()+"\n"
                                        +this.address.Text.Trim()+"\n"
                                        +this.town.Text.Trim()+"\t"
                                        +this.country.Text.Trim());
                request.Data.SetWebLink(uri);
                request.Data.Properties.Title = "Goyav Inc";
            }
        }
        internal static IRandomAccessStream ConvertTo(byte[] arr)
        {
            MemoryStream stream = new MemoryStream(arr);
            return stream.AsRandomAccessStream();
        }
        private void shareAppData(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        /// <summary>
    }

}
