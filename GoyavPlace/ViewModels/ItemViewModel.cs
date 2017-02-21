using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GoyavPlace.Data;
using Windows.Globalization.DateTimeFormatting;
using System.Runtime.Serialization;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Data;

namespace GoyavPlace.ViewModels
{
    [DataContract]
    public class AuthToken
    {
        [DataMember(Name = "status")]
        public string status { get; set; }
        [DataMember(Name = "success")]
        public bool success { get; set; }
        [DataMember(Name = "info")]
        public string info { get; set; }
        [DataMember(Name = "data")]
        public DataAuthToken token { get; set; }
    }
    [DataContract]
    public class DataAuthToken
    {
        [DataMember(Name = "auth_token_id")]
        public int id { get; set; }
        [DataMember(Name = "auth_token")]
        public string auth_token { get; set; }
    }
    [DataContract]
    public class ApiClient
    {
        [DataMember(Name = "api_client")]
        public string api_client { get; set; }
        [DataMember(Name = "api_model")]
        public string api_model { get; set; }
    }

    [DataContract]
    public class ItemViewModel
    {
        private int _itemId;

        public int ItemId
        {
            get
            {
                return _itemId;
            }
        }

        public string DateCreatedHourMinute
        {
            get
            {
                var formatter = new DateTimeFormatter("hour minute");
                return formatter.Format(DateCreated);
            }
        }

        public string Title { get; set; }
        public string Text { get; set; }
        public string Picture { get; set; }
        public DateTime DateCreated { get; set; }

        public ItemViewModel()
        {
        }

        public static ItemViewModel FromItem(Item item)
        {
            var viewModel = new ItemViewModel();

            viewModel._itemId = item.Id;
            viewModel.DateCreated = item.DateCreated;
            viewModel.Title = item.Title;
            viewModel.Text = item.Text;
            viewModel.Picture = item.Picture;
            return viewModel;
        }
    }
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
    [DataContract]
    public class Author
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
        public DateTime updated_at { get; set; }
        [DataMember(Name = "author")]
        public Author author { get; set; }
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

    [DataContract]
    public class Evaluation : EvaluationData
    {
        [DataMember(Name = "place_id")]
        public int place_id { get; set; }
        [DataMember(Name = "api_key_id")]
        public int api_key_id { get; set; }

    }

    [DataContract]
    public class NewPlaceData : PlaceData
    {
        [DataMember(Name = "location_attributes")]
        public new LocationData Location { get; set; }
        [DataMember(Name = "picture_attributes")]
        public new PictureData Picture { get; set; }
        [DataMember(Name = "category_attributes")]
        public new CategoryData Category { get; set; }
        [DataMember(Name = "evaluation_attributes")]
        public new Evaluation Evaluation { get; set; }
    }
    [DataContract]
    public class AddResponse : ResponseData
    {
        [DataMember(Name = "place")]
        public NewPlaceData Place { get; set; }
    }

    [DataContract]
    public class PhotoData : PictureData
    {
        [DataMember(Name = "photo")]
        public string photo { get; set; }
    }
    [DataContract]
    public class Photo : PhotoData
    {
        [DataMember(Name = "id")]
        public string id { get; set; }
        [DataMember(Name = "encoded")]
        public string encoded { get; set; }
        [DataMember(Name = "place_id")]
        public int place_id { get; set; }
        [DataMember(Name = "api_key_id")]
        public int api_key_id { get; set; }
        [DataMember(Name = "updated_at")]
        public DateTime updated_at { get; set; }
        [DataMember(Name = "author")]
        public Author author { get; set; }

        public BitmapImage base64Decoded { get; set; }

    }
    [DataContract]
    public class ResponsePhoto
    {
        [DataMember(Name = "success")]
        public string success { get; set; }
        [DataMember(Name = "status")]
        public string status { get; set; }
        [DataMember(Name = "photos")]
        public Photo[] photos { get; set; }
    }



}
