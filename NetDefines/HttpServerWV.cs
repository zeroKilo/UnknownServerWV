using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetDefines
{
    public class HttpServerWV
    {
        #region public members
        public ushort bindPort = 80;
        public string bindAddress = "localhost";
        public HttpListener httpListener;
        public Func<HttpListenerContext, int> DefaultHandlerGET = DefaultHandler;
        public Func<HttpListenerContext, int> DefaultHandlerPOST = DefaultHandler;
        #endregion

        #region private members
        private readonly object _syncStop = new object();
        private readonly object _syncRunning = new object();
        private bool _isRunning = false;
        private bool _shouldStop = false;
        private Dictionary<string, Func<HttpListenerContext, int>> _handlerGET = new Dictionary<string, Func<HttpListenerContext, int>>();
        private Dictionary<string, Func<HttpListenerContext, int>> _handlerPOST = new Dictionary<string, Func<HttpListenerContext, int>>();
        #endregion

        #region thread safe public getter/setters
        public bool IsRunning
        {
            get
            {
                bool result = false;
                lock (_syncRunning)
                {
                    result = _isRunning;
                }
                return result;
            }
            set
            {
                lock (_syncRunning)
                {
                    _isRunning = value;
                }
            }
        }
        public bool ShouldStop
        {
            get
            {
                bool result = false;
                lock (_syncStop)
                {
                    result = _shouldStop;
                }
                return result;
            }
            set
            {
                lock (_syncStop)
                {
                    _shouldStop = value;
                }
            }
        }
        #endregion

        public HttpServerWV(ushort port)
        {
            bindPort = port;
        }

        public HttpServerWV(string address, ushort port)
        {
            bindAddress = address;
            bindPort = port;
        }

        public void Start()
        {
            if (IsRunning)
                return;
            ShouldStop = false;
            new Thread(tMainLoop).Start();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;
            ShouldStop = true;
            if (httpListener != null)
                httpListener.Stop();
        }

        public static int DefaultHandler(HttpListenerContext ctx)
        {
            SendResponse(ctx, HttpStatusCode.NotFound, "text/plain", "");
            return 0;
        }

        public void AddHandlerGET(string url, Func<HttpListenerContext, int> handler)
        {
            if (_handlerGET.ContainsKey(url))
                _handlerGET[url] = handler;
            else
                _handlerGET.Add(url, handler);
        }

        public void AddHandlerPOST(string url, Func<HttpListenerContext, int> handler)
        {
            if (_handlerPOST.ContainsKey(url))
                _handlerPOST[url] = handler;
            else
                _handlerPOST.Add(url, handler);
        }

        public static void SendResponse(HttpListenerContext ctx, HttpStatusCode status, string type, string content)
        {
            HttpListenerResponse res = ctx.Response;
            res.StatusCode = (int)status;
            res.ContentType = type;
            res.Headers["Server"] = "";
            byte[] buff = Encoding.UTF8.GetBytes(content);
            res.OutputStream.Write(buff, 0, content.Length);
            res.OutputStream.Close();
        }

        public static void SendJsonResponse(HttpListenerContext ctx, string content)
        {
            SendResponse(ctx, HttpStatusCode.OK, "application/json", content);
        }

        public static void SendTextResponse(HttpListenerContext ctx, string content)
        {
            SendResponse(ctx, HttpStatusCode.OK, "text/plain", content);
        }

        public static void SendBadRequestResponse(HttpListenerContext ctx)
        {
            SendResponse(ctx, HttpStatusCode.BadRequest, "text/plain", "");
        }

        public static string GetRequestData(HttpListenerContext ctx)
        {
            HttpListenerRequest req = ctx.Request;
            if (!req.HasEntityBody)
                return "";
            using (Stream body = req.InputStream)
                using (var reader = new StreamReader(body, req.ContentEncoding))
                    return reader.ReadToEnd();
        }

        public static string GetResponseData(HttpResponseMessage msg)
        {
            Task<string> task = Task.Run(() => msg.Content.ReadAsStringAsync());
            task.Wait();
            return task.Result;
        }

        public static HttpResponseMessage SendRestRequest(HttpMethod type, string baseAddress, string path, string content, Dictionary<string,string> extraHeader = null, string contentType= "application/json")
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://" + baseAddress + "/");
            HttpRequestMessage request = new HttpRequestMessage(type, path);
            if (extraHeader != null)
                foreach (KeyValuePair<string, string> pair in extraHeader)
                    request.Headers.Add(pair.Key, pair.Value);
            if (type == HttpMethod.Post)
                request.Content = new StringContent(content, Encoding.UTF8, contentType);
            Task<HttpResponseMessage> task = Task.Run(() => client.SendAsync(request));
            task.Wait();
            return task.Result;
        }

        public static HttpResponseMessage SendSignedRestRequest(RSAParameters rsa, string pubKey, HttpMethod type, string baseAddress, string path, string content, Dictionary<string, string> extraHeader = null, string contentType = "application/json")
        {
            byte[] buff = Encoding.ASCII.GetBytes(content);
            byte[] signature = NetHelper.MakeSignature(buff, rsa);
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://" + baseAddress + "/");
            HttpRequestMessage request = new HttpRequestMessage(type, path);
            request.Headers.Add("Signature", NetHelper.MakeHexString(signature));
            request.Headers.Add("Public-Key", pubKey);
            if (extraHeader != null)
                foreach (KeyValuePair<string, string> pair in extraHeader)
                    request.Headers.Add(pair.Key, pair.Value);
            if (type == HttpMethod.Post)
                request.Content = new StringContent(content, Encoding.UTF8, contentType);
            Task<HttpResponseMessage> task = Task.Run(() => client.SendAsync(request));
            task.Wait();
            return task.Result;
        }

        private void tMainLoop(object obj)
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://" + bindAddress + ":" + bindPort + "/");
            httpListener.Start();
            IsRunning = true;
            try
            {
                while (true)
                {
                    HttpListenerContext ctx = httpListener.GetContext();
                    new Thread(tClientHandler).Start(ctx);
                    if (ShouldStop)
                        break;
                }
            }
            catch { }
            IsRunning = false;
        }

        private void tClientHandler(object obj)
        {
            HttpListenerContext ctx = (HttpListenerContext)obj;
            HttpListenerRequest req = ctx.Request;
            try
            {
                switch (req.HttpMethod)
                {
                    case "GET":
                        if (_handlerGET.ContainsKey(req.RawUrl))
                            _handlerGET[req.RawUrl](ctx);
                        else
                            DefaultHandlerGET(ctx);
                        break;
                    case "POST":
                        if (_handlerPOST.ContainsKey(req.RawUrl))
                            _handlerPOST[req.RawUrl](ctx);
                        else
                            DefaultHandlerPOST(ctx);
                        break;
                    default:
                        DefaultHandler(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                SendBadRequestResponse(ctx);
            }
        }
    }
}
