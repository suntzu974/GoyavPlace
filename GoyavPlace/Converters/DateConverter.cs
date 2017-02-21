using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace GoyavPlace.Converters
{
    class ConvertDate : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime? dateVal = value as DateTime?;
            if (dateVal == null || !dateVal.HasValue)
                return value;

            var myDate = dateVal.Value.ToLocalTime();
            string retVal = string.Empty;
            if (myDate.Date == DateTime.Today)
            {
                retVal = myDate.ToString("HH:mm:ss");
            }
            else if (myDate.Date > DateTime.Today.AddDays(-6))
            {
                retVal = myDate.ToString("ddd HH:mm:ss");
            }
            else if (myDate.Year == DateTime.Today.Year)
            {
                retVal = myDate.Date.ToString("dd MMM yyyy HH:mm:ss");
            }
            else
            {
                retVal = myDate.Date.ToString("dd MMM yyyy HH:mm:ss");
            }

            return retVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

    }
}
