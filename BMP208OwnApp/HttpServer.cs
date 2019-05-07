using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace BMP208OwnApp
{
    public sealed class HttpServer : IDisposable
    {
        private const uint bufLen = 8192;
        private int defaultPort = 80;
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
            // Content body
            string respBody = string.Format(@"<html>
                                                    <head>
                                                        <title>Weather Station</title>
                                                        <meta http-equiv='refresh' content='3' />
                                                    </head>
                                                    <body>
                                                        <p><font size='3'>Time:{0}</font></p>
                                                        <br/>
                                                        <p><font size='6'>Temperature: {1} deg C</font></p>
                                                        <p><font size='6'>Light: {2} lux</font></p>
                                                        <p><font size='6'>Pressure: {3} Pa</font></p>
                                                        <p><font size='6'>Altitude: {4} m</font></p>
                                                        <br />
                                                    </body>
                                                  </html>",

                                            DateTime.Now.ToString("h:mm:ss tt"),
                                            String.Format("{0:0.00}", MainPage.temp),
                                            String.Format("{0:0.00}", MainPage.currentLux),
                                            String.Format("{0:0.00}", MainPage.pressure),
                                            String.Format("{0:0.00}", MainPage.altitude));

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
