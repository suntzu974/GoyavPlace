using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoyavPlace.EventModel
{
    public class Message
    {
        public Guid Id { get; set; }

        public string UserTo { get; set; }

        public string UserFrom { get; set; }

        public string TheMessage { get; set; }

        public DateTime SentDate { get; set; }
    }
}
