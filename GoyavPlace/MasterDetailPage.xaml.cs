using GoyavPlace.Data;
using GoyavPlace.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GoyavPlace
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MasterDetailPage : Page
    {
        private PlaceData _lastSelectedItem;
        List<PlaceData> listOfPlaces = new List<PlaceData>();

        public MasterDetailPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

        }
   
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var items = e.Parameter as List<PlaceData>;
            listOfPlaces = e.Parameter as List<PlaceData>;
            base.OnNavigatedTo(e);
            if (items != null)
            {
                MasterListView.ItemsSource = items;// items;
            }

            /*if (e.Parameter != null)
            {
                // Parameter is item ID
                var id = e.Parameter as PlaceData;
                _lastSelectedItem =
                    items.Where((PlaceData) => PlaceData == id).FirstOrDefault();
            }*/

            UpdateForVisualState(AdaptiveStates.CurrentState);

            // Don't play a content transition for first item load.
            // Sometimes, this content will be animated as part of the page transition.
            DisableContentTransitions();

            if (Frame.CanGoBack)
            {
                // Show UI in title bar if opted-in and in-app backstack is not empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if in-app back stack is empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Collapsed;
            }
        }

        private void AdaptiveStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            UpdateForVisualState(e.NewState, e.OldState);
        }

        private void UpdateForVisualState(VisualState newState, VisualState oldState = null)
        {
            var isNarrow = newState == NarrowState;

            if (isNarrow && oldState == DefaultState && _lastSelectedItem != null)
            {
                // Resize down to the detail item. Don't play a transition.
                Frame.Navigate(typeof(DetailPage), _lastSelectedItem, new SuppressNavigationTransitionInfo());
            }

            EntranceNavigationTransitionInfo.SetIsTargetElement(MasterListView, isNarrow);
            if (DetailContentPresenter != null)
            {
                EntranceNavigationTransitionInfo.SetIsTargetElement(DetailContentPresenter, !isNarrow);
            }
        }

        private void MasterListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = (PlaceData)e.ClickedItem;
            _lastSelectedItem = clickedItem;

            if (AdaptiveStates.CurrentState == NarrowState)
            {
                // Use "drill in" transition for navigating from master list to detail view
                Frame.Navigate(typeof(DetailPage), clickedItem, new DrillInNavigationTransitionInfo());
            }
            else
            {
                // Play a refresh animation when the user switches detail items.
                EnableContentTransitions();
            }
        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            // Assure we are displaying the correct item. This is necessary in certain adaptive cases.
            MasterListView.SelectedItem = _lastSelectedItem;
        }

        private void EnableContentTransitions()
        {
            DetailContentPresenter.ContentTransitions.Clear();
            DetailContentPresenter.ContentTransitions.Add(new EntranceThemeTransition());
        }

        private void DisableContentTransitions()
        {
            if (DetailContentPresenter != null)
            {
                DetailContentPresenter.ContentTransitions.Clear();
            }
        }

        private void getIndex(object sender, SelectionChangedEventArgs e)
        {
            var pivot = this.mapsPivot.SelectedIndex;
            if (pivot == 1)
            {
                placeMap.MapServiceToken = "hhsGCt9NnTPzFSDLqtQE~-lHPBfyxJXelWFsjueAo-w~AuWF2cq1VKSnPzf9w6DtpnSKE20eL8AcuDHVRTJglKs671qn6bU9QVUao4QT_-Js";

                // Specify a known location.
                //   | 

                // Places
                foreach (PlaceData place in listOfPlaces)
                {
                    BasicGeoposition snPosition = new BasicGeoposition() { Latitude = place.Location.latitude, Longitude = place.Location.longitude };
                    Geopoint snPoint = new Geopoint(snPosition);
                    MapIcon myPOI = new MapIcon { Location = snPoint, NormalizedAnchorPoint = new Point(0.5, 1.0), Title = place.name.ToString() , ZIndex = 0 };
                    // add to map and center it
                    placeMap.MapElements.Add(myPOI);
                    placeMap.Center = snPoint;

#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Latitude : " + snPosition.Latitude.ToString()+ " Name :" + place.name.ToString());
#endif
                }
                // Create a MapIcon.

                // Center the map over the POI.
                placeMap.ZoomLevel = 10;
            }
        }
    }
}
