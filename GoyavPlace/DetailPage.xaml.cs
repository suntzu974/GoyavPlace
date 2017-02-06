using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GoyavPlace
{
    public class FBReturnObject
    {
        public string Id { get; set; }
        public string Post_Id { get; set; }
    }

    [DataContract]
    public class TweetAuthor
    {
        [DataMember(Name = "id")]
        public int id { get; set; }
        [DataMember(Name = "api_token")]
        public string api_token { get; set; }
    }
    [DataContract]
    public class TweetData
    {
        [DataMember(Name = "id")]
        public int id { get; set; }
        [DataMember(Name = "tweet")]
        public string tweet { get; set; }
    }
    [DataContract]
    public class Tweet : TweetData
    {
        [DataMember(Name = "place_id")]
        public int place_id { get; set; }
        [DataMember(Name = "api_key_id")]
        public int api_key_id { get; set; }
        [DataMember(Name = "updated_at")]
        public string updated_at { get; set; }
        [DataMember(Name ="author")]
        public TweetAuthor author { get; set; }
    }
    [DataContract]
    public class ResponseTweet
    {
        [DataMember(Name = "success")]
        public string success { get; set; }
        [DataMember(Name = "status")]
        public string status { get; set; }
        [DataMember(Name = "tweets")]
        public Tweet[] tweets { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DetailPage : Page
    {

        PlaceData place = new PlaceData();
        Evaluation evaluation = new Evaluation();
        CategoryData category = new CategoryData();
        Tweet tweet = new Tweet();

        public DetailPage()
        {
            this.InitializeComponent();
            this.likeButton.Visibility = Visibility.Collapsed;
            this.refreshButton.Visibility = Visibility.Collapsed;
        }

        private async void getTweets()
        {
            noteGrid.Items.Clear();
            // Search place
            try
            {
                //progress.IsActive = true;
                //Create HttpClient
                HttpClient httpClient = new HttpClient();
                //Define Http Headers
                httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                String addressForTweets  = string.Format("https://www.goyav.com/api/v2/search_tweets.json?place_id={0}", place.id);

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
                        // item.distance = distance(latitude, longitude, item.latitude, item.longitude, 'K');
                        noteGrid.Items.Add(tweet);
                    }
                }
                else
                {
                    // 
                }
                if (wcfResponse.IsSuccessStatusCode)
                {
                    //progress.IsActive = false;
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            place = e.Parameter as PlaceData;
            #if DEBUG
                System.Diagnostics.Debug.WriteLine("GET Place from parameter :" + place.name.ToString());
            #endif
            this.name.Text = place.name.ToString();
            this.address.Text = place.Location.address.ToString();
            this.phone.Text = place.phone.ToString();
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
            Windows.ApplicationModel.Calls.PhoneCallManager.ShowPhoneCallUI(place.phone.ToString(), place.name.ToString());
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
                    place.phone != String.Empty &&
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
                            resourceAddress = string.Format("https://www.goyav.com/api/v2/evaluations/{0}.{1}", place.Evaluation.id, "json");
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

        private async void getShare(object sender, RoutedEventArgs e)
        {
            FBSession sess = FBSession.ActiveSession;
            sess.FBAppId = "204650499987919";
            sess.WinAppId = "s-1-15-2-4184798398-3214885102-1243361708-2144477766-3872497592-3011963963-154036082";

            // Add permissions required by the app
            List<String> permissionList = new List<String>();
            permissionList.Add("public_profile");
            permissionList.Add("user_friends");
            permissionList.Add("user_likes");
            permissionList.Add("user_groups");
            permissionList.Add("user_location");
            permissionList.Add("user_photos");
            permissionList.Add("publish_actions");
            FBPermissions permissions = new FBPermissions(permissionList);

            // Login to Facebook
            FBResult result = await sess.LoginAsync(permissions);

            if (result.Succeeded)
            {
                //Login successful
                // Get current user
                FBUser user = sess.User;
                // Set caption, link and description parameters
                PropertySet parameters = new PropertySet();
                parameters.Add("title", "Microsoft");
                parameters.Add("link", "https://www.microsoft.com/en-us/default.aspx");
                parameters.Add("description", "Microsoft home page");
                // Add post message
                parameters.Add("message", "Posting from my Universal Windows app.");
                // Set Graph api path
                string path = "/" + user.Id + "/feed";
                var factory = new FBJsonClassFactory(s =>
                {
                    return JsonConvert.DeserializeObject<FBReturnObject>(s);
                });
                var singleValue = new FBSingleValue(path, parameters, factory);
                var resultat = await singleValue.PostAsync();
                if (resultat.Succeeded)
                {
                    var response = resultat.Object as FBReturnObject;
                }
                else
                {
                    // Posting failed
                #if DEBUG
                    System.Diagnostics.Debug.WriteLine("Post to facebook failed");
                #endif
                }
            }
            else
            {
                //Login failed
                #if DEBUG
                    System.Diagnostics.Debug.WriteLine("Login to facebook failed");
                #endif
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
                    place.phone != String.Empty &&
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
                            resourceAddress = string.Format("https://www.goyav.com/api/v2/evaluations/{0}.{1}", place.Evaluation.id, "json");
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
                            resourceAddress = string.Format("https://www.goyav.com/api/v2/evaluations.json");
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
                resourceAddress = string.Format("https://www.goyav.com/api/v2/categories/{0}.{1}", place.Category.id, "json");
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
                System.Diagnostics.Debug.WriteLine("Index Horaires");
                this.likeButton.Visibility = Visibility.Visible;
                this.refreshButton.Visibility = Visibility.Collapsed;

            }
            if (pivot == 3)
            {
                System.Diagnostics.Debug.WriteLine("Index Tarif");
                this.refreshButton.Visibility = Visibility.Collapsed;
            }
            if (pivot == 4)
            {
                System.Diagnostics.Debug.WriteLine("Index Calendrier");
                this.refreshButton.Visibility = Visibility.Collapsed;

            }

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
                Dictionary<String, Object> TWEET= new Dictionary<String, Object>();
                tweet.tweet = this.noteText.Text.ToString();
                tweet.api_key_id = Convert.ToInt16(api_token_id);
                tweet.place_id = place.id;
                TWEET["tweet"] = tweet;

                if (tweet.tweet != String.Empty )
                {

                    string postBody = JsonConvert.SerializeObject(TWEET, Formatting.Indented);
                    try
                    {
                        //overwrite the value if you need to
                        resourceAddress = string.Format("https://www.goyav.com/api/v2/tweets.json?place_id={0}?api_key_id={1}&tweet{2}", tweet.place_id,tweet.api_key_id,tweet.tweet);
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
    }

}
