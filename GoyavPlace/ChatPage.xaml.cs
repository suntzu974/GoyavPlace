using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;
using static GoyavPlace.MainPage;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace GoyavPlace
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class ChatPage : Page
    {
        private const string socketId = "SampleSocket";
        private const string port = "2345";
        private MainPage rootPage;
        MessageWebSocket webSock;
        private DataWriter messageWriter;

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        public ChatPage()
        {
            this.InitializeComponent();
            InitChatWithServer();
        }

        private async void InitChatWithServer()
        {
            await InitChatAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            rootPage = MainPage.Current;

        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CloseSocket();
        }

        private async Task InitChatAsync()
        {
            webSock = new MessageWebSocket();

            //In this case we will be sending/receiving a string so we need to set the MessageType to Utf8.
            webSock.Control.MessageType = SocketMessageType.Utf8;
            //Add the MessageReceived event handler.
            webSock.MessageReceived += WebSock_MessageReceivedAsync;
            //Add the Closed event handler.
            webSock.Closed += OnClosed;

            Uri serverUri = new Uri("ws://www.goyav.com:2345");
            AppendOutputLine($"Connecting to {serverUri}...");
            try
            {
                //Connect to the server.
                await webSock.ConnectAsync(serverUri);

            }
            catch (Exception ex)
            {
                //Add code here to handle any exceptions
                webSock.Dispose();
                webSock = null;

                AppendOutputLine(BuildWebSocketError(ex));
                AppendOutputLine(ex.Message);
            }
            messageWriter = new DataWriter(webSock.OutputStream);
        }
        async void OnSend()
        {
            await SendAsync();
        }

        async Task SendAsync()
        {
            string message = InputField.Text;
            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            AppendOutputLine("Sending Message: " + message);

            // Buffer any data we want to send.
            messageWriter.WriteString(message);

            try
            {
                // Send the data as one complete message.
                await messageWriter.StoreAsync();
            }
            catch (Exception ex)
            {
                AppendOutputLine(BuildWebSocketError(ex));
                AppendOutputLine(ex.Message);
                return;
            }

        }
        //The MessageReceived event handler.
        private void WebSock_MessageReceivedAsync(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                AppendOutputLine("Message Received; Type: " + args.MessageType);
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = UnicodeEncoding.Utf8;

                    try
                    {
                        string read = reader.ReadString(reader.UnconsumedBufferLength);
                        AppendOutputLine(read);
                    }
                    catch (Exception ex)
                    {
                        AppendOutputLine(ex.Message);
                    }
                }
            });

        }



        private void OnDisconnect()
        {
            CloseSocket();
        }
        private async void OnClosed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            // Dispatch the event to the UI thread so we do not need to synchronize access to messageWebSocket.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                AppendOutputLine("Closed; Code: " + args.Code + ", Reason: " + args.Reason);

                if (webSock == sender)
                {
                    CloseSocket();
                }
            });
        }
        private void CloseSocket()
        {
            if (messageWriter != null)
            {
                // In order to reuse the socket with another DataWriter, the socket's output stream needs to be detached.
                // Otherwise, the DataWriter's destructor will automatically close the stream and all subsequent I/O operations
                // invoked on the socket's output stream will fail with ObjectDisposedException.
                //
                // This is only added for completeness, as this sample closes the socket in the very next code block.
                messageWriter.DetachStream();
                messageWriter.Dispose();
                messageWriter = null;
            }

            if (webSock != null)
            {
                try
                {
                    webSock.Close(1000, "Closed due to user request.");
                }
                catch (Exception ex)
                {
                    AppendOutputLine(BuildWebSocketError(ex));
                    AppendOutputLine(ex.Message);
                }
                webSock = null;
            }
        }
        public static string BuildWebSocketError(Exception ex)
        {
            ex = ex.GetBaseException();

            if ((uint)ex.HResult == 0x800C000EU)
            {
                // INET_E_SECURITY_PROBLEM - our custom certificate validator rejected the request.
                return "Error: Rejected by custom certificate validation.";
            }

            WebErrorStatus status = WebSocketError.GetStatus(ex.HResult);

            // Normally we'd use the HResult and status to test for specific conditions we want to handle.
            // In this sample, we'll just output them for demonstration purposes.
            switch (status)
            {
                case WebErrorStatus.CannotConnect:
                case WebErrorStatus.NotFound:
                case WebErrorStatus.RequestTimeout:
                    return "Cannot connect to the server. Please make sure " +
                        "to run the server setup script before running the sample.";

                case WebErrorStatus.Unknown:
                    return "COM error: " + ex.HResult;

                default:
                    return "Error: " + status;
            }
        }
        private void AppendOutputLine(string value)
        {
            OutputField.Text += value + "\r\n";
        }
    }
}

