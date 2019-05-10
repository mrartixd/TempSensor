using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace BMP208OwnApp
{
    public sealed class HttpServer : IDisposable
    {
        private const uint bufLen = 8192;
        private readonly int defaultPort = 80;
        private readonly StreamSocketListener sock;

        public object[] TimeStamp { get; private set; }

        public HttpServer()
        {
            sock = new StreamSocketListener();

            sock.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
        }

        public async void StartServer()
        {
            await sock.BindServiceNameAsync(defaultPort.ToString());
        }

        private async void ProcessRequestAsync(StreamSocket socket)
        {
            // Read in the HTTP request, we only care about type 'GET'
            StringBuilder request = new StringBuilder();
            using (IInputStream input = socket.InputStream)
            {
                byte[] data = new byte[bufLen];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = bufLen;
                while (dataRead == bufLen)
                {
                    await input.ReadAsync(buffer, bufLen, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                    dataRead = buffer.Length;
                }
            }

            using (IOutputStream output = socket.OutputStream)
            {
                string requestMethod = request.ToString().Split('\n')[0];
                string[] requestParts = requestMethod.Split(' ');
                await WriteResponseAsync(requestParts, output);
            }
        }

        private async Task WriteResponseAsync(string[] requestTokens, IOutputStream outstream)
        {
            
            string sText = @File.ReadAllText("mainpage.html");
            StringBuilder sb = new StringBuilder(sText);
            sb.Replace("{0}",DateTime.Now.ToString("HH:mm"));
            sb.Replace("{1}", String.Format("{0:0.00}", MainPage.temp));
            sb.Replace("{2}", String.Format("{0:0.00}", MainPage.currentLux));
            sb.Replace("{3}", String.Format("{0:0.00}", MainPage.temp));
            sb.Replace("{4}", String.Format("{0:0.00}", MainPage.temp));


            string respBody = sb.ToString();

            string htmlCode = "200 OK";

            using (Stream resp = outstream.AsStreamForWrite())
            {
                byte[] bodyArray = Encoding.UTF8.GetBytes(respBody);
                MemoryStream stream = new MemoryStream(bodyArray);

                // Response heeader
                string header = string.Format("HTTP/1.1 {0}\r\n" +
                                              "Content-Type: text/html\r\n" +
                                              "Content-Length: {1}\r\n" +
                                              "Connection: close\r\n\r\n",
                                              htmlCode, stream.Length);

                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }

        public void Dispose()
        {
            sock.Dispose();
        }
    }
}
