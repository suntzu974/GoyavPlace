using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Security.Authentication.Web;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GoyavPlace.ViewModels;
using Windows.Devices.Geolocation;
using System.Threading;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.ApplicationModel.Background;

namespace GoyavPlace
{
    
    sealed partial class App : Application
    {
        private uint _desireAccuracyInMetersValue = 0;
        private CancellationTokenSource _cts = null;
        private AuthToken authentication;
        public const string IP_ADDRESS = "https://www.goyav.com/api/v2";
//        public const string IP_ADDRESS = "https://www.goyav.re/api/v2";
        public const string APP_NAME = "GoyavPlace";
        DispatcherTimer quoteTimer;
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        ///  NEW CODE
        /// </summary>
        /// 

        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Navigated += RootFrame_Navigated;

                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage));
                }
                // Register User
                RegisterUser();
                await SetupDevice();
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                // Register a handler for BackRequested events and set the  
                // visibility of the Back button  
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                        rootFrame.CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // Each time a navigation event occurs, update the Back button's visibility  
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }

        private async void RegisterUser()
        {
            System.Diagnostics.Debug.WriteLine("FIRST RegisterUser");
            if (await Task.Run(() => NetworkInterface.GetIsNetworkAvailable()))
            {
                //Wifi or Cellular
                System.Diagnostics.Debug.WriteLine("SECOND RegisterUser");
                bool connect = await isAuthenticatedUser();
                if (!connect)
                {
                    System.Diagnostics.Debug.WriteLine("CONNECT TO RegisterUser");
                    DataAuthToken token_value = await register();
                    System.Diagnostics.Debug.WriteLine("TOKEN=NO:" + token_value);
                    System.Diagnostics.Debug.WriteLine("TOKEN_ID " + token_value.id);
                    System.Diagnostics.Debug.WriteLine("EN SORT A OU:");
                }
            }
            else
            {
                // No internet
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("NO INTERNET PROVIDED");
                #endif
                var dialog = new MessageDialog("Internet", "Pas de connexion ");
                await dialog.ShowAsync();
                Application.Current.Exit();
            }
        }
        //-------------------------------------
        public async Task<bool> isAuthenticatedUser()
        {
            System.Diagnostics.Debug.WriteLine("THIRD RegisterUser");
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            System.Diagnostics.Debug.WriteLine("THIRD STEP 0 RegisterUser");
            Object api_token = localSettings.Values["auth_token"];
            System.Diagnostics.Debug.WriteLine("THIRD STEP 1 RegisterUser");
            Object api_token_id = localSettings.Values["auth_token_id"];
            System.Diagnostics.Debug.WriteLine("THIRD STEP 2 RegisterUser");
            Object allowDate = localSettings.Values["allowDate"];
            if (allowDate == null)
            {
                localSettings.Values["allowDate"] = false;
            }
            Object mesureUnit = localSettings.Values["unit"];
            if (mesureUnit == null)
            {
                localSettings.Values["unit"] = false;
            }

            if (api_token != null && api_token_id != null)
            {
                // No data
                System.Diagnostics.Debug.WriteLine("BEGIN THIRD RegisterUser");
                #if DEBUG
                    System.Diagnostics.Debug.WriteLine("TOKEN_ID IS  !!" + api_token_id.ToString());
                #endif
            }
            try
            {
                System.Diagnostics.Debug.WriteLine("BEGIN THIRD RegisterUser in Try ");
                //Create HttpClient
                HttpClient httpClient = new HttpClient();
                //Define Http Headers
                httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                //Call
                string url = string.Format("{0}/get_token.json?api_token={1}", App.IP_ADDRESS,api_token);
                string ResponseString = await httpClient.GetStringAsync(
                    new Uri(url));
                //Replace current URL with your URL
                authentication = JsonConvert.DeserializeObject<AuthToken>(ResponseString);
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("GET TOKEN Response !!" + ResponseString.ToString());
                System.Diagnostics.Debug.WriteLine("SID :" + WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString());
                #endif
            }

            catch (Exception ex)
            {
                //....
            }
            if (authentication.success)
                return true;
            else
                return false;
        }
        //-------------------------------------
        private async Task<DataAuthToken> register()
        {
            System.Diagnostics.Debug.WriteLine("REGISTER RegisterUser");
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
            Dictionary<String, Object> ApiKey = new Dictionary<String, Object>();
            Dictionary<String, Object> ApiClient = new Dictionary<String, Object>();
            ApiClient["api_client"] = deviceInfo.SystemManufacturer;
            ApiClient["api_model"] = deviceInfo.SystemProductName;
            ApiClient["api_app"] = App.APP_NAME;

            ApiKey["api_key"] = ApiClient;

            HttpClient httpClient = new HttpClient();
            AuthToken authentication = new AuthToken();
            DataAuthToken token = new DataAuthToken();
            try
            {
                string postBody = JsonConvert.SerializeObject(ApiKey, Formatting.Indented);
                System.Diagnostics.Debug.WriteLine("postBody  :" + postBody);
                String resourceAddress = string.Format("{0}/registrations.json",App.IP_ADDRESS);
                System.Diagnostics.Debug.WriteLine("resourceAddress   :" + resourceAddress + ": Postbody :" + postBody);
                httpClient.BaseAddress = new Uri(resourceAddress);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage wcfResponse = await httpClient.PostAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                var responseString = await wcfResponse.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine("Response  :" + responseString);
                authentication = JsonConvert.DeserializeObject<AuthToken>(responseString);
                System.Diagnostics.Debug.WriteLine(responseString);
                token = authentication.token;
                if (wcfResponse.IsSuccessStatusCode)
                {
                    localSettings.Values["auth_token"] = authentication.token.auth_token.ToString();
                    localSettings.Values["auth_token_id"] = authentication.token.id;
                }
            }
            catch (HttpRequestException hre)
            {
                System.Diagnostics.Debug.WriteLine("Error:" + hre.Message);
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Request canceled.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (httpClient != null)
                {
                    httpClient.Dispose();
                    httpClient = null;
                }
            }
            return token;
        }
        private async Task SetupDevice()
        {

            this.quoteTimer = new DispatcherTimer();
            this.quoteTimer.Interval = TimeSpan.FromMinutes(2);
            this.quoteTimer.Tick += this.quoteTimer_Tick;
            this.quoteTimer.Start();
        }

        private void quoteTimer_Tick(object sender, object e)
        {
            updateUserPosition();
        }
        private async void updateUserPosition()
        {
            string resourceAddress = String.Empty;
            HttpClient httpClient = new HttpClient();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("auth_token"))
            {
                //overwrite the value if you need to
                Object api_token = localSettings.Values["auth_token"];
                Object api_token_id = localSettings.Values["auth_token_id"];
                Dictionary<String, Object> ApiKey = new Dictionary<String, Object>();
                Dictionary<String, Object> ApiPosition = new Dictionary<String, Object>();

                try
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken token = _cts.Token;
                    // If DesiredAccuracy or DesiredAccuracyInMeters are not set (or value is 0), DesiredAccuracy.Default is used.
                    Geolocator geolocator = new Geolocator { DesiredAccuracyInMeters = _desireAccuracyInMetersValue };
                    // Carry out the operation
                    Geoposition pos = await geolocator.GetGeopositionAsync().AsTask(token);
                    ApiPosition["id"] = api_token_id;
                    ApiPosition["latitude"] = pos.Coordinate.Point.Position.Latitude;
                    ApiPosition["longitude"] = pos.Coordinate.Point.Position.Longitude;
                    ApiPosition["online"] = true;
                    ApiKey["api_key"] = ApiPosition;

                    string postBody = JsonConvert.SerializeObject(ApiKey, Formatting.Indented);
                    // Update 
                    resourceAddress = string.Format("{0}/registrations/{1}.{2}", App.IP_ADDRESS, api_token_id, "json");
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage wcfResponse = await httpClient.PutAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                    var responseString = await wcfResponse.Content.ReadAsStringAsync();
                    AddResponse RetResponse = JsonConvert.DeserializeObject<AddResponse>(responseString);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Response String for updating api_key  " + responseString.ToString() + " Place :" + postBody);
                    System.Diagnostics.Debug.WriteLine("Response String for ressource  " + resourceAddress.ToString() + " Place :" + postBody);
#endif
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
        }
    }
}
