/****************************************************************************
*                                                                           *
* vCatchClientSocket.cs                                                     *
*                                                                           *
* made by Willy.Lee                                                         *
*                                                                           *
*    Kee-Wan Lee, 2022-          e-mail : wiljwilj@hotmail.com              *
*                                                                           *
****************************************************************************/
// 2022-10-23 - IP by unicode PC name make exception - localhost -> IPAddress.Loopback

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace vCatchStation
{
    public class vCatchClientSocket
    {
        Socket _sock = null;
        bool _connecting = false;
        string _detectionTypes = "";

        public bool Connected { get { return _sock != null && _sock.Connected; } }
        public bool Closed { get { return _sock == null || !_connecting; } }

        public interface Event {
            void vCatchClientSocket_OnClose();
            void vCatchClientSocket_OnPacket(JObject json);
        }

        Event _event = null;

        public bool Connect(string detectionTypes, Event evt)
        {
            if (_sock != null)
            {
                _event = evt;
                return true;
            }

            _event = evt;

            _detectionTypes = detectionTypes;
            if (_detectionTypes == null)
                return false;
            if (_detectionTypes.Length > 0 && _detectionTypes[0] != '[')
            {
                if (_detectionTypes[0] != '{')
                    _detectionTypes = '{' + _detectionTypes + '}';
                _detectionTypes = '[' + _detectionTypes + ']';
            }

            try
            {
                _sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _connecting = true;

                var sockEventArg = new SocketAsyncEventArgs();
                var ip = IPAddress.Loopback;
                sockEventArg.RemoteEndPoint = new IPEndPoint(ip, vCatchStation.App_TCP_PortNumber);
                sockEventArg.UserToken = _sock;
                sockEventArg.Completed += new EventHandler<SocketAsyncEventArgs>((s, e) =>
                {
                    Socket sock = (Socket)s;
                    if (e.SocketError == SocketError.Success && _sock.Connected)
                    {
                        _connecting = false;

                        try
                        {
                            string strSnd = "{\"init\":" + _detectionTypes + "}\n";
                            byte[] bytesSnd = Encoding.ASCII.GetBytes(strSnd);
                            _sock.Send(bytesSnd);

                            _sock.BeginReceive(_bufTcpReceive, 0, _bufTcpReceive.Length,
                                SocketFlags.None, new AsyncCallback(OnReceived), _sock);
                        }
                        catch
                        {
                            _sock = null;
                        }
                    }
                    else
                    {
                        _sock = null;
                    }
                });
                _sock.ConnectAsync(sockEventArg);
            }
            catch
            {
                _sock = null;
                return false;
            }
            return true;
        }

        private byte[] _bufTcpReceive = new byte[1024];
        private string _fromStation = "";

        private void OnReceived(IAsyncResult ar)
        {
            Socket sock = (Socket)ar.AsyncState;
            try
            {
                int recieved = sock.EndReceive(ar);
                if (recieved <= 0)
                {
                    Close();
                    return;
                }

                _fromStation += ASCIIEncoding.ASCII.GetString(_bufTcpReceive, 0, recieved);
                while (_fromStation.Length > 1)
                {
                    int nEnd = _fromStation.IndexOf('\n');
                    if (nEnd < 0)
                        break;

                    string packet = _fromStation.Substring(0, nEnd);
                    JObject json = JObject.Parse(packet);
                    if (_event != null)
                        _event.vCatchClientSocket_OnPacket(json);

                    _fromStation = _fromStation.Substring(nEnd + 1);
                }

                sock.BeginReceive(_bufTcpReceive, 0, _bufTcpReceive.Length,
                    SocketFlags.None, new AsyncCallback(OnReceived), sock);
            }
            catch
            {
                Close();
            }
        }

        public void Close()
        {
            if (_sock != null)
            {
                string strSnd = "{\"deinit\":{}}\n";
                byte[] bytesSnd = Encoding.ASCII.GetBytes(strSnd);
                try
                {
                    _sock.Send(bytesSnd);
                    _sock.Close();
                }
                catch
                {
                }
                _sock = null;
            }
            _fromStation = "";

            if (_event != null)
            {
                _event.vCatchClientSocket_OnClose();
                _event = null;
            }
        }

        public bool Do_vCatch(string protocol_face, string options = null)
        {
            string strSnd = "{\"do\":\"" + protocol_face + "\"";
            if (options != null && options.Length > 0)
                strSnd += "," + options;
            strSnd += "}\n";
            byte[] bytesSnd = Encoding.ASCII.GetBytes(strSnd);
            _sock.Send(bytesSnd);
            return true;
        }

        public void AddLog(long time, int level,
            string tag, string msg, string filename, int lineno)
        {
            int s_skipped = 0;

            JObject log = new JObject();
            log.Add("time", time);
            log.Add("level", level);
            log.Add("tag", tag);
            log.Add("skipped", s_skipped);
            log.Add("msg", msg);
            if (filename != null)
                log.Add("file", filename);
            log.Add("line", lineno);

            JObject json = new JObject();
            json.Add("log", log);
            string str = json.ToString(Formatting.None) + "\n";
            byte[] bytesSnd = Encoding.ASCII.GetBytes(str);
            _sock.Send(bytesSnd);
        }
    }
}

