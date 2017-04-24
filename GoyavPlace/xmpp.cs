using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.Streams;

namespace GoyavPlace
{
        public class LoginParams
        {
            public string user { set; get; }
            public string pwd { set; get; }
            public string host { set; get; }
            public int port { set; get; }
            public string resource { set; get; }
            public string domain { set; get; }

            public LoginParams()
            {
                port = 5222;
                resource = "win10";
                domain = "domain";
            }
        }
        public class XmppClient
        {
            public LoginParams params_;
            public string jid_;

            private int id_;
            private bool is_logined_;
            private bool is_loginning_;
            private _Connection conn_;
            private _LoginTask login_task_;
            private _XmlParser parser_;

            public delegate void cbLoginOk(object sender);
            public delegate void cbLoginFail(object sender, int code, string s);
            public delegate void cbConnectionClose(object sender, int code, string s);
            public delegate void cbXmppOutput(object sender, string s);
            public delegate void cbXmppInput(object sender, string s);
            public delegate void cbRecvIQ(object sender, string s);
            public delegate void cbRecvPresence(object sender, string s);
            public delegate void cbRecvMessage(object sender, string s);
            public delegate void cbSendXmlResult(object sender, int id, bool rs, string s);

            public event cbLoginOk OnLoginOk;
            public event cbLoginFail OnLoginFail;
            public event cbConnectionClose OnConnectionClose;
            public event cbXmppOutput OnXmppOutput;
            public event cbXmppInput OnXmppInput;
            public event cbRecvIQ OnRecvIQ;
            public event cbRecvPresence OnRecvPresence;
            public event cbRecvMessage OnRecvMessage;

            enum CloseReason
            {
                SERVER_CLOSE = -1,
                XML_INVALID = -2,
                QUIT = -3
            }

            public XmppClient()
            {
                id_ = 0;
                is_logined_ = false;
                is_loginning_ = false;
                params_ = new LoginParams();
                login_task_ = null;
                parser_ = new _XmlParser();
                conn_ = new _Connection();
                conn_.OnSocketRead += OnSocketRead;
                conn_.OnSocketState += OnSocketState;
            }

            ~XmppClient()
            {

            }

            public bool Login()
            {
                if (is_logined_ || is_loginning_)
                    return true;
                is_loginning_ = true;
                jid_ = params_.user + "@" + params_.domain + "/" + params_.resource;
                login_task_ = new _LoginTask();
                login_task_.client_ = this;
                login_task_.parser_ = parser_;
                login_task_.conn_ = conn_;
                return conn_.Connect(params_.host, params_.port);
            }

            public void Quit()
            {
                SignalConnectionClose((int)CloseReason.QUIT, "");
            }
            public bool SendXml(string s)
            {
                if (OnXmppOutput != null)
                    OnXmppOutput(this, s);
                return conn_.SendData(s);
            }

            public bool IsLogined()
            {
                return is_logined_;
            }
            public string Jid()
            {
                return jid_;
            }

            public int NextId()
            {
                return id_++;
            }
            private async void OnSocketState(int state, int code, string s)
            {
                _Connection.SocketState _state = (_Connection.SocketState)state;
                switch (_state)
                {
                    case _Connection.SocketState.CONNECTED:
                    case _Connection.SocketState.TLS_CONNECTED:
                        {
                            login_task_.Start();
                            break;
                        }
                    case _Connection.SocketState.CONNECT_FAILED:
                    case _Connection.SocketState.TLS_CONNECT_FAILED:
                        {
                            SignalLoginFailed(code, s);
                            break;
                        }
                    case _Connection.SocketState.CLOSE:
                        {
                            if (is_loginning_)
                                SignalLoginFailed(code, s);
                            else
                                SignalConnectionClose(code, s);
                            break;
                        }
                }
            }

            private void OnSocketRead(string s)
            {
                Debug.WriteLine("\n[ proc_xml ]\n" + s);

                if (OnXmppInput != null)
                    OnXmppInput(this, s);

                try
                {
                    if (login_task_ != null)
                    {
                        if (!login_task_.ProcData(s))
                        {
                            SignalLoginFailed(login_task_.GetErrorCode(), login_task_.GetErrorInfo());
                            login_task_ = null;
                        }
                        else if (login_task_.IsDone())
                        {
                            is_logined_ = true;
                            is_loginning_ = true;
                            login_task_ = null;
                            if (OnLoginOk != null)
                            {
                                OnLoginOk(this);
                            }
                        }
                        return;
                    }

                    XmlReader _reader = parser_.Create(s);
                    while (_reader.Read())
                    {
                        if (_reader.Depth == 0
                            && (_reader.IsEmptyElement || _reader.NodeType == XmlNodeType.Element))
                        {
                            string sname = _reader.Name;
                            string elem = _reader.ReadOuterXml();
                            Debug.WriteLine(elem);

                            try
                            {
                                if (sname == "iq" && OnRecvIQ != null)
                                    OnRecvIQ(this, elem);
                                if (sname == "message" && OnRecvMessage != null)
                                    OnRecvMessage(this, elem);
                                if (sname == "presence" && OnRecvPresence != null)
                                    OnRecvPresence(this, elem);
                            }
                            catch (System.Exception ex)
                            {

                            }

                            _reader.Skip();
                        }
                    }
                }
                catch (Exception e)
                {
                    if (s == "</stream:stream>")
                    {
                        SignalConnectionClose((int)CloseReason.SERVER_CLOSE, "server close stream");
                    }
                    else
                    {
                        SignalConnectionClose((int)CloseReason.XML_INVALID, "XML invalid");
                    }
                }
            }

            private void SignalLoginFailed(int code, string s)
            {
                conn_.Close();
                is_logined_ = false;
                is_loginning_ = false;
                if (OnLoginFail != null)
                    OnLoginFail(this, code, s);
            }

            private void SignalConnectionClose(int code, string s)
            {
                conn_.Close();
                is_logined_ = false;
                is_loginning_ = false;
                if (OnConnectionClose != null)
                    OnConnectionClose(this, code, s);
            }
        }

        public static class XmppNS
        {
            public const string Xmlns = "http://www.w3.org/2000/xmlns";
            public const string Xml = "http://www.w3.org/XML/1998/namespace";
            public const string Client = "jabber:client";
            public const string Stream = "http://etherx.jabber.org/streams";
            public const string XmppStreams = "urn:ietf:params:xml:ns:xmpp-streams";
            public const string StartTls = "urn:ietf:params:xml:ns:xmpp-tls";
            public const string Sasl = "urn:ietf:params:xml:ns:xmpp-sasl";
            public const string Auth = "http://jabber.org/features/iq-auth";
            public const string Register = "http://jabber.org/features/iq-register";
            public const string Bind = "urn:ietf:params:xml:ns:xmpp-bind";
            public const string Session = "urn:ietf:params:xml:ns:xmpp-session";
            public const string Compression = "http://jabber.org/features/compress";
            public const string CompressionProtocol = "http://jabber.org/protocol/compress";
            public const string Stanzas = "urn:ietf:params:xml:ns:xmpp-stanzas";
            public const string Rostver = "urn:xmpp:features:rosterver";
            public const string Entity = "http://jabber.org/protocol/caps";
            public const string DiscoInfo = "http://jabber.org/protocol/disco#info";
            public const string Ping = "urn:xmpp:ping";
            public const string Roster = "jabber:iq:roster";
        }

        internal class _XmlParser
        {
            public _XmlParser()
            {
                doc_ = new XmlDocument();
                nsmgr_ = new XmlNamespaceManager(doc_.NameTable);
                nsmgr_.AddNamespace("", XmppNS.Client);
                nsmgr_.AddNamespace("stream", XmppNS.Stream);
                sets_ = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
                ctx_ = new XmlParserContext(null, nsmgr_, null, XmlSpace.None);
                reader_ = null;
            }

            public XmlReader Create(string s)
            {
                reader_ = XmlReader.Create(new StringReader(s), sets_, ctx_);
                return reader_;
            }

            public XmlDocument doc_;
            public XmlNamespaceManager nsmgr_;
            public XmlParserContext ctx_;
            public XmlReaderSettings sets_;
            public XmlReader reader_;
        }
        internal class _Connection
        {
            private StreamSocket socket_;
            private DataReader reader_;
            private DataWriter writer_;
            private string host_;
            private int port_;

            public enum SocketState
            {
                NONE = 0,
                CONNECTED = 1,
                CONNECT_FAILED = 2,
                CLOSE = 3,
                TLS_CONNECTED = 4,
                TLS_CONNECT_FAILED = 5
            }

            public delegate void cbSocketState(int state, int code, string s);
            public delegate void cbSocketRead(string s);
            public event cbSocketState OnSocketState;
            public event cbSocketRead OnSocketRead;
            public _Connection()
            {
                reader_ = null;
                writer_ = null;
                socket_ = null;
            }

            ~_Connection()
            {
                DetachStream();
                if (socket_ != null)
                {
                    socket_.Dispose();
                    socket_ = null;
                }
            }

            public bool Connect(string host, int port)
            {
                DetachStream();
                host_ = host;
                port_ = port;

                var act = new Action(async () =>
                {
                    if (socket_ == null)
                        socket_ = new StreamSocket();
                    try
                    {
                        await socket_.ConnectAsync(new HostName(host), port.ToString(), SocketProtectionLevel.PlainSocket);
                        AttachStream();
                        SignalSocketState(SocketState.CONNECTED, 0, "");
                        ReadMoreData();
                    }
                    catch (System.Exception ex)
                    {
                        var subcode = Windows.Networking.Sockets.SocketError.GetStatus(ex.HResult);
                        SignalSocketState(SocketState.CONNECT_FAILED, (int)subcode, ex.Message);
                    }

                });
                act.Invoke();

                return true;
            }

            public void Close()
            {
                DetachStream();
                if (socket_ != null)
                {
                    socket_.Dispose();
                    socket_ = null;
                }
            }

            public async void UpgradeToTls()
            {
                try
                {
                    DetachStream();
                    socket_.Control.ClientCertificate = null;
                    socket_.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                    socket_.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
                    await socket_.UpgradeToSslAsync(SocketProtectionLevel.Tls12, new HostName(host_));
                    AttachStream();
                    SignalSocketState(SocketState.TLS_CONNECTED, 0, "");
                    ReadMoreData();
                }
                catch (System.Exception ex)
                {
                    var subcode = Windows.Networking.Sockets.SocketError.GetStatus(ex.HResult);
                    SignalSocketState(SocketState.TLS_CONNECT_FAILED, (int)subcode, ex.Message);
                }

            }

            public bool SendData(string s)
            {
                if (writer_ == null)
                    return false;
                writer_.WriteString(s);
                writer_.StoreAsync();
                return true;
            }

            private async void ReadMoreData(uint max_len = 8096)
            {
                uint read = 0;
                try
                {
                    while (true)
                    {
                        read = await reader_.LoadAsync(max_len);
                        string str = reader_.ReadString(read);
                        while (read >= max_len)
                        {
                            read = await reader_.LoadAsync(max_len);
                            str += reader_.ReadString(read);
                        }
                        if (OnSocketRead != null)
                        {
                            OnSocketRead(str);
                        }
                        //NOTE: reader_ may be reset before upgrade to ssl.
                        // c# no need to lock and set reader_, it's atomic, very good.
                        if (read <= 0 || reader_ == null)
                            break;
                    }
                }
                catch (System.Exception ex)
                {
                    var subcode = Windows.Networking.Sockets.SocketError.GetStatus(ex.HResult);
                    SignalSocketState(SocketState.CLOSE, (int)subcode, ex.Message);
                }

            }

            private void SignalSocketState(SocketState state, int code, string s)
            {
                if (OnSocketState != null)
                {
                    OnSocketState((int)state, code, s);
                }
            }

            private void DetachStream()
            {
                if (reader_ != null)
                {
                    reader_.DetachStream();
                    reader_.Dispose();
                    reader_ = null;
                }
                if (writer_ != null)
                {
                    writer_.DetachStream();
                    writer_.Dispose();
                    writer_ = null;
                }
            }

            private void AttachStream()
            {
                reader_ = new DataReader(socket_.InputStream);
                reader_.InputStreamOptions = InputStreamOptions.Partial;
                writer_ = new DataWriter(socket_.OutputStream);
            }
        }
        internal class _LoginTask : object
        {
            // LoginState value different from SocketError (0-N)
            enum LoginState
            {
                START = -1,
                TLS = -2,
                COMPRESS = -3,
                AUTH = -4,
                BIND = -5,
                DONE = -6
            }

            public _LoginTask()
            {
                state_ = LoginState.START;
                client_ = null;
                openstream_ = false;
            }

            public bool ProcData(string s)
            {
                if (!openstream_)
                {
                    s += "</stream:stream>";
                    openstream_ = true;
                }

                switch (state_)
                {
                    case LoginState.START: return on_state_start(s);
                    case LoginState.TLS: return on_state_tls(s);
                    case LoginState.AUTH: return on_state_auth(s);
                    case LoginState.BIND: return on_state_bind(s);
                }

                return false;
            }

            public bool IsDone()
            {
                return (state_ == LoginState.DONE);
            }

            public int GetErrorCode()
            {
                return (int)state_;
            }
            public string GetErrorInfo()
            {
                switch (state_)
                {
                    case LoginState.START: return "Start failed";
                    case LoginState.TLS: return "TLS handshake failed";
                    case LoginState.AUTH: return "Auth failed";
                    case LoginState.BIND: return "Bind failed";
                }
                return "Login failed(unknown)";
            }

            public bool Start()
            {
                openstream_ = false;
                string str = string.Format("<stream:stream to=\"{0}\" xml:lang=\"*\" version=\"1.0\" xmlns:stream=\"http://etherx.jabber.org/streams\" xmlns=\"jabber:client\">"
                    , client_.params_.host);
                return client_.SendXml(str);
            }

            private bool on_state_start(string s)
            {
                bool has_features = false;
                XmlReader _reader = parser_.Create(s);
                while (_reader.Read())
                {
                    if (_reader.IsEmptyElement || _reader.NodeType == XmlNodeType.Element)
                    {
                        if (_reader.Name == "stream:features")
                            has_features = true;

                        if (_reader.Name == "starttls")
                        {
                            state_ = LoginState.TLS;
                            client_.SendXml("<starttls xmlns=\"urn:ietf:params:xml:ns:xmpp-tls\"/>");
                            return true;
                        }
                        else if (_reader.Name == "compression")
                        {

                        }
                        else if (_reader.Name == "auth")
                        {
                            state_ = LoginState.AUTH;
                            string str = "\0" + client_.params_.user + "\0" + client_.params_.pwd;
                            byte[] b = System.Text.Encoding.ASCII.GetBytes(str);
                            str = Convert.ToBase64String(b);
                            str = "<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"PLAIN\">" + str + "</auth>";
                            client_.SendXml(str);
                            return true;
                        }
                        else if (_reader.Name == "bind")
                        {
                            state_ = LoginState.BIND;
                            string str = "<iq type=\"set\" id=\"{0}\"><bind xmlns=\"urn:ietf:params:xml:ns:xmpp-bind\"><resource>{1}</resource></bind></iq>";
                            str = string.Format(str, client_.NextId(), client_.params_.resource);
                            client_.SendXml(str);
                            return true;
                        }
                    }
                }

                return !has_features;
            }

            private bool on_state_tls(string s)
            {
                XmlReader _reader = parser_.Create(s);
                while (_reader.Read())
                {
                    if (_reader.IsEmptyElement || _reader.NodeType == XmlNodeType.Element)
                    {
                        if (_reader.Name == "proceed")
                        {
                            state_ = LoginState.START;
                            conn_.UpgradeToTls();
                            return true;
                        }
                    }
                }

                return false;
            }

            private bool on_state_auth(string s)
            {
                XmlReader _reader = parser_.Create(s);
                while (_reader.Read())
                {
                    if (_reader.IsEmptyElement || _reader.NodeType == XmlNodeType.Element)
                    {
                        if (_reader.Name == "success")
                        {
                            state_ = LoginState.START;
                            return Start();
                        }
                    }
                }

                return false;
            }

            private bool on_state_bind(string s)
            {
                int succ = 0;
                XmlReader _reader = parser_.Create(s);
                while (_reader.Read())
                {
                    if (_reader.IsEmptyElement || _reader.NodeType == XmlNodeType.Element)
                    {
                        if (_reader.Name == "iq")
                        {
                            succ = (_reader.GetAttribute("type") == "result") ? 1 : 0;
                        }
                        else if (_reader.Name == "bind")
                        {
                            succ++;
                        }
                        else if (_reader.Name == "jid")
                        {
                            client_.jid_ = _reader.ReadInnerXml();
                        }
                    }
                }
                if (succ == 2)
                    state_ = LoginState.DONE;
                return (state_ == LoginState.DONE);
            }

            private bool openstream_;
            private LoginState state_;
            public XmppClient client_;
            public _XmlParser parser_;
            public _Connection conn_;
        }
}
