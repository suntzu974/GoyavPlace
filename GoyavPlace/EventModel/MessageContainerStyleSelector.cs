using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GoyavPlace.EventModel
{
    public class MessageContainerStyleSelector : StyleSelector
    {
        public Style SentStyle { get; set; }

        public Style ReceivedStyle { get; set; }

        public string Sender { get; set; }

        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            var message = item as Message;
            if (message != null)
            {
                return message.UserFrom.Equals(this.Sender, StringComparison.CurrentCultureIgnoreCase)
                           ? this.SentStyle
                           : this.ReceivedStyle;
            }

            return base.SelectStyleCore(item, container);
        }
    }
}
