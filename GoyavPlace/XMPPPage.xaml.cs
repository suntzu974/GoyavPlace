using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace GoyavPlace
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class XMPPPage : Page
    {
        public XMPPPage()
        {
            this.InitializeComponent();
            XmppClient xmpp = new XmppClient();
            xmpp.params_.host = "213.167.243.103";
            xmpp.params_.port = 5222;
            xmpp.params_.domain = "www.goyav.com";
//            xmpp.params_.resource = "win10";
            xmpp.params_.user = "suntzu974";
            xmpp.params_.pwd = "HpNKUsNN@27031968";

            xmpp.OnLoginOk += OnLoginOk;
            xmpp.OnLoginFail += OnLoginFail;
            xmpp.OnConnectionClose += OnConnectionClose;
            xmpp.OnXmppInput += OnXmppInput;
            xmpp.OnXmppOutput += OnXmppOutput;
            xmpp.OnRecvIQ += OnRecvIQ;
            xmpp.OnRecvMessage += OnRecvMessage;
            xmpp.OnRecvPresence += OnRecvPresence;

            xmpp.Login();
        }

        private void OnRecvPresence(object sender, string s)
        {
            throw new NotImplementedException();
        }

        private void OnRecvMessage(object sender, string s)
        {
            throw new NotImplementedException();
        }

        private void OnRecvIQ(object sender, string s)
        {
            throw new NotImplementedException();
        }

        public void OnXmppInput(object sender, string s)
        {
            textBlock.Text += string.Format("[   recv   ] {0} {1} \n", sender.GetHashCode(),
                DateTime.Now.ToString());

            textBlock.Text += s;
            textBlock.Text += "\n\n";
        }

        public void OnXmppOutput(object sender, string s)
        {
            textBlock.Text += string.Format("[   send   ] {0} {1} \n", sender.GetHashCode(),
                DateTime.Now.ToString());

            textBlock.Text += s;
            textBlock.Text += "\n\n";
        }

        private void OnConnectionClose(object sender, int code, string s)
        {
            throw new NotImplementedException();
        }

        private void OnLoginFail(object sender, int code, string s)
        {
            throw new NotImplementedException();
        }

        private void OnLoginOk(object sender)
        {
            throw new NotImplementedException();
        }
    }
}
