using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace GoyavPlace
{
    public class ImageItem  
    {
        public BitmapImage Source { get; set; }
        public string base64Content { get; set; }

        public ImageItem()
        {

        }

        public ImageItem(BitmapImage bitmap, string base64)
        {
//            BitmapImage bmp = new BitmapImage();
//            bmp.SetSource(stream);
            Source = bitmap;
            base64Content = base64;
        }
    }
}
