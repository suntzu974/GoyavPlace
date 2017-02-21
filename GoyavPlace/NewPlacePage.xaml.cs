using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static GoyavPlace.MainPage;
using GoyavPlace.ViewModels;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GoyavPlace
{
   
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewPlacePage : Page
    {
        List<ImageItem> ImageList = new List<ImageItem>();
        string base64Content = String.Empty;
        private uint _desireAccuracyInMetersValue = 0;
        private CancellationTokenSource _cts = null;
        NewPlaceData place = new NewPlaceData();
        LocationData location = new LocationData();
        PictureData photo = new PictureData();
        Evaluation evaluation = new Evaluation();
        CategoryData category = new CategoryData();


        public NewPlacePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }

            base.OnNavigatingFrom(e);
        }
        async private void getLocation(object sender, RoutedEventArgs e)
        {
            LocationDisabledMessage.Visibility = Visibility.Collapsed;

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

                        UpdateLocationData(pos);
                        NotifyUser("Location updated.", NotifyType.StatusMessage);
                        break;

                    case GeolocationAccessStatus.Denied:
                        UpdateLocationData(null);
                        NotifyUser("Access to location is denied.", NotifyType.ErrorMessage);
                        //LocationDisabledMessage.Visibility = Visibility.Visible;
                        break;

                    case GeolocationAccessStatus.Unspecified:
                        NotifyUser("Unspecified error.", NotifyType.ErrorMessage);
                        UpdateLocationData(null);
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


        }
        private async void UpdateLocationData(Geoposition position)
        {
            if (position == null)
            {
                geolocation.Text = "No data";
                accuracy.Text = "No data";
            }
            else
            {

                MapLocationFinderResult result =
                await MapLocationFinder.FindLocationsAtAsync(new Geopoint(position.Coordinate.Point.Position));

                if (result.Status == MapLocationFinderStatus.Success)
                {
                    address.Text = result.Locations[0].Address.Street;
                    town.Text = result.Locations[0].Address.Town;
                    country.Text = result.Locations[0].Address.Country;
                    location.latitude = position.Coordinate.Point.Position.Latitude;
                    location.longitude = position.Coordinate.Point.Position.Longitude;
                    geolocation.Text = "Lat:" + position.Coordinate.Point.Position.Latitude.ToString("n2") + ",Lng:" + position.Coordinate.Point.Position.Longitude.ToString("n2") + ",Accuracy:" + position.Coordinate.Accuracy.ToString("n2");
                    accuracy.Text = position.Coordinate.Accuracy.ToString();
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

        

        private async void letSave(object sender, RoutedEventArgs e)
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
                Dictionary<String, Object> PLACE = new Dictionary<String, Object>();
                place.name = this.name.Text.ToUpper();
                place.phone = this.phone.Text.ToUpper();
                location.town = this.town.Text.ToString();
                location.address = this.address.Text.ToString();
                location.country = this.country.Text.ToString();
                photo.file = base64Content;
                photo.filename = "goyav.png";
                photo.picture = "goyav.png";
                // check
                if(rbPlace.IsChecked == true)
                    category.category = 1;
                if (rbLeisure.IsChecked == true)
                    category.category = 2;
                if (rbSleep.IsChecked == true)
                    category.category = 3;
                if (rbEat.IsChecked == true)
                    category.category = 4;

                evaluation.price = (int)slPrice.Value;
                evaluation.service = (int)slService.Value;
                evaluation.quality = (int)slQuality.Value;
                evaluation.like = 0;
                evaluation.api_key_id = Convert.ToInt16(api_token_id);

                place.api_key_id = Convert.ToInt16(api_token_id);
                place.Location = location;
                place.Picture = photo;
                place.Category = category;
                place.Evaluation = evaluation;
                PLACE["place"] = place;

                if (place.name != String.Empty && 
                    place.phone != String.Empty && 
                    place.Location.address != String.Empty &&
                    place.Location.town != String.Empty &&
                    place.Location.country != String.Empty)
                {

                    string postBody = JsonConvert.SerializeObject(PLACE, Formatting.Indented);
                    try
                    {
                        this.progress.IsActive = true;
                        //overwrite the value if you need to
                        resourceAddress = string.Format("{0}/v2/places.json?auth_token={1}", App.IP_ADDRESS,api_token.ToString());
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage wcfResponse = await httpClient.PostAsync(resourceAddress, new StringContent(postBody, Encoding.UTF8, "application/json"));
                        var responseString = await wcfResponse.Content.ReadAsStringAsync();
                        AddResponse RetResponse = JsonConvert.DeserializeObject<AddResponse>(responseString);
                        // Navigate to cocktail page with item you click/tap on
                        #if DEBUG
                            System.Diagnostics.Debug.WriteLine("Response String for new place " + responseString.ToString());
                        #endif
                        NotifyUser("http response status code ." + wcfResponse.StatusCode, NotifyType.StatusMessage);
                        if (wcfResponse.IsSuccessStatusCode)
                        {
                            // Clean Fields
                            this.name.Text = String.Empty;
                            this.address.Text = String.Empty;
                            this.town.Text = String.Empty;
                            this.country.Text = String.Empty;
                            this.phone.Text = String.Empty;
                            this.geolocation.Text = String.Empty;
                            this.progress.IsActive = false;
                            //this.Frame.Navigate(typeof(MainPage), string.Empty);
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

        private void getRefresh(object sender, RoutedEventArgs e)
        {
            // Refresh

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
                ImageList.Add(new ImageItem(bitmapImage, base64Content));
                #if DEBUG
                    System.Diagnostics.Debug.WriteLine("ImageList count: " + ImageList.Count());
                #endif
                picture.Source = bitmapImage;
            }
            else
            {
                this.geolocation.Text = "Operation cancelled.";
            }

        }
        private async void getCamera(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            //captureUI.PhotoSettings.CroppedSizeInPixels = new Size(100, 100);
            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (photo != null)
            {
                // Application now has read/write access to the picked file
                var stream = await photo.OpenAsync(FileAccessMode.Read);
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                var decoder = await BitmapDecoder.CreateAsync(stream);
                base64Content = Convert.ToBase64String(await CreateScaledImage2(decoder, 760, 1024));
                ImageList.Add(new ImageItem(bitmapImage, base64Content));
                #if DEBUG
                    System.Diagnostics.Debug.WriteLine("ImageList count: " + ImageList.Count());
                #endif
                picture.Source = bitmapImage;

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
        /// <summary>
        /// Resizes and crops source file image so that resized image width/height are not larger than <param name="requestedMinSide"></param>
        /// </summary>
        /// <param name="sourceFile">Source StorageFile</param>
        /// <param name="requestedMinSide">Width/Height of the output image</param>
        /// <param name="resizedImageFile">Target StorageFile</param>
        /// <returns></returns>
        private async Task<IStorageFile> CreateThumbnaiImage(StorageFile sourceFile, int requestedMinSide, StorageFile resizedImageFile)
        {
            var imageStream = await sourceFile.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            var originalPixelWidth = decoder.PixelWidth;
            var originalPixelHeight = decoder.PixelHeight;
            using (imageStream)
            {
                //do resize only if needed
                if (originalPixelHeight > requestedMinSide && originalPixelWidth > requestedMinSide)
                {
                    using (var resizedStream = await resizedImageFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        //create encoder based on decoder of the source file
                        var encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                        double widthRatio = (double)requestedMinSide / originalPixelWidth;
                        double heightRatio = (double)requestedMinSide / originalPixelHeight;
                        uint aspectHeight = (uint)requestedMinSide;
                        uint aspectWidth = (uint)requestedMinSide;
                        uint cropX = 0, cropY = 0;
                        var scaledSize = (uint)requestedMinSide;
                        if (originalPixelWidth > originalPixelHeight)
                        {
                            aspectWidth = (uint)(heightRatio * originalPixelWidth);
                            cropX = (aspectWidth - aspectHeight) / 2;
                        }
                        else
                        {
                            aspectHeight = (uint)(widthRatio * originalPixelHeight);
                            cropY = (aspectHeight - aspectWidth) / 2;
                        }
                        //you can adjust interpolation and other options here, so far linear is fine for thumbnails
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                        encoder.BitmapTransform.ScaledHeight = aspectHeight;
                        encoder.BitmapTransform.ScaledWidth = aspectWidth;
                        encoder.BitmapTransform.Bounds = new BitmapBounds()
                        {
                            Width = scaledSize,
                            Height = scaledSize,
                            X = cropX,
                            Y = cropY,
                        };
                        await encoder.FlushAsync();
                    }
                }
                else
                {
                    //otherwise just use source file as thumbnail
                    await sourceFile.CopyAndReplaceAsync(resizedImageFile);
                }
            }
            return resizedImageFile;
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

    }
}
