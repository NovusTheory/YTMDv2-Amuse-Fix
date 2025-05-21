using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NotificationIcon.NET;

namespace YTMDAmuseFix
{
    internal class Program
    {
        private static string LISTEN_PREFIX = "http://localhost:9963/";
        private static string SIXKLABS_URL = "https://6klabs.com";
        private static string SIXKLABS_CANVAS_URL = "https://canvaz.6klabs.com";

        private static Regex WIDGET_URL_REGEX = new("/widget/youtube/[A-Za-z0-9_]+/?[A-Za-z0-9_]+?/?$", RegexOptions.Compiled | RegexOptions.Singleline);

#if DEBUG
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
#endif

        static async Task Main(string[] args)
        {
#if DEBUG
            AllocConsole();
#endif

            var processClosed = false;

            var trayThread = new Thread(CreateNotifyIcon);
            trayThread.Start();

            var listener = new HttpListener();
            listener.Prefixes.Add(LISTEN_PREFIX);
            listener.Start();
            Console.WriteLine($"Listening on {LISTEN_PREFIX}");

            var proxyClient = new HttpClient();

            while (!processClosed)
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                Console.WriteLine($"{request.HttpMethod} {request.Url!.PathAndQuery}");

                HttpRequestMessage proxyRequest;
                if (request.Url!.AbsolutePath.StartsWith("/canvaz-proxy"))
                {
                    var pathAndQuery = request.Url!.AbsolutePath.Remove(0, 13);
                    proxyRequest = new HttpRequestMessage(HttpMethod.Parse(request.HttpMethod), $"{SIXKLABS_CANVAS_URL}{pathAndQuery}");
                    proxyRequest.Content = new StreamContent(request.InputStream);
                }
                else
                {
                    proxyRequest = new HttpRequestMessage(HttpMethod.Parse(request.HttpMethod), $"{SIXKLABS_URL}{request.Url!.PathAndQuery}");
                }

                var proxyResponse = await proxyClient.SendAsync(proxyRequest);
                var proxyResponseStream = await proxyResponse.Content.ReadAsStreamAsync();

                if (WIDGET_URL_REGEX.IsMatch(request.Url!.AbsolutePath))
                {
                    Console.WriteLine("Injecting Amuse fix into widget request...");

                    var doc = new HtmlDocument();
                    doc.Load(proxyResponseStream);

                    var interceptor = doc.CreateElement("script");
                    interceptor.InnerHtml = @"
                        function getHighestResThumbnail(thumbnails) {
                            return thumbnails.reduce((accu, curr) => (curr.height * curr.width <= accu.height * accu.width ? accu : curr), thumbnails[0]).url;
                        }

                        // XMLHttpRequest Interceptor
                        const originalXHR = window.XMLHttpRequest;

                        function XHRInterceptor() {
                            const xhr = new originalXHR();
                            const originalOpen = xhr.open;
                            const originalSend = xhr.send

                            xhr.open = function(method, urlString, async, user, password) {
                                const args = arguments;

                                const url = new URL(urlString);
                                if (url.host === ""canvaz.6klabs.com"") {
                                    url.protocol = ""http:""
                                    url.host = window.location.host;
                                    url.pathname = `/canvaz-proxy${url.pathname}`;
                                    args[1] = url.toString();
                                }
                  
                                this._url = urlString;
                                return originalOpen.apply(xhr, args);
                            };

                            xhr.send = function(body) {
                                if (this._url === ""http://localhost:9863/query"") {
                                    // If the URL matches, provide an artificial response
                                    setTimeout(() => {
                                        let hasMetadata = window.ytmdState.video && window.ytmdState.video.metadataFilled;
                                        const ytmdV1Response = JSON.stringify({
                                            player: {
                                                hasSong: hasMetadata,
                                                isPaused: window.ytmdState.player.trackState != 1,
                                                seekbarCurrentPosition: window.ytmdState.player.videoProgress
                                            },
                                            track: {
                                                author: window.ytmdState.video?.author,
                                                title: hasMetadata ? window.ytmdState.video?.title : """",
                                                album: window.ytmdState.video?.album ?? """",
                                                cover: getHighestResThumbnail(window.ytmdState.video?.thumbnails),
                                                duration: window.ytmdState.video?.durationSeconds,
                                                isAdvertisement: window.ytmdState.player.adPlaying
                                            }
                                        });

                                        Object.defineProperty(this, ""readyState"", { value: 4, configurable: true });
                                        Object.defineProperty(this, ""status"", { value: 200, configurable: true });
                                        Object.defineProperty(this, ""response"", { value: ytmdV1Response, configurable: true });
                                        Object.defineProperty(this, ""responseText"", { value: ytmdV1Response, configurable: true });

                                        if (typeof this.onreadystatechange === ""function"") {
                                            this.onreadystatechange();
                                        }
                                        this.dispatchEvent(new Event(""readystatechange""));

                                        if (typeof this.onload === ""function"") {
                                            this.onload();
                                        }
                                        this.dispatchEvent(new Event(""load""));

                                        if (typeof this.onloadend === ""function"") {
                                            this.onloadend();
                                        }
                                        this.dispatchEvent(new Event(""loadend""));
                                    }, 0);
                                } else {
                                    return originalSend.apply(xhr, arguments);
                                }
                            };

                            return xhr;
                        }

                        window.XMLHttpRequest = XHRInterceptor;
                    ";
                    doc.DocumentNode.SelectSingleNode("html").SelectSingleNode("head").AppendChild(interceptor);

                    var ytmdv2Api = doc.CreateElement("script");
                    ytmdv2Api.SetAttributeValue("type", "module");
                    ytmdv2Api.InnerHtml = @"
                        import { CompanionConnector } from 'https://esm.run/ytmdesktop-ts-companion';
              
                        const settings = {
                            host: ""127.0.0.1"",
                            port: 9863,
                            appId: ""ytmd-fix-6klabs-amuse"",
                            appName: ""YTMD 6k Labs Amuse Fixer"",
                            appVersion: ""1.0.0""
                        }

                        const savedToken = localStorage.getItem(""ytmd-token"");
                        if (savedToken) {
                            settings.token = savedToken;
                        }

                        let connector;
                        try {
                            connector = new CompanionConnector(settings);
                        } catch(error) {
                            console.log(error);
                            alert(""Failed to create connector to YTMDesktop. Refresh the widget and try again"");
                        }

                        const restClient = connector.restClient;
                        const socketClient = connector.socketClient;

                        socketClient.addErrorListener(error => {
                            if (error.message === ""Authentication not provided or invalid"") {
                                localStorage.removeItem(""ytmd-token"");
                            }
                        });
                        socketClient.addStateListener(state => {
                            window.ytmdState = state;
                        });

                        if (!savedToken) {
                            try {
                                const codeResponse = await restClient.getAuthCode();
                                setTimeout(() => alert(""Please compare this code with: "" + codeResponse.code), 0);

                                const tokenResponse = await restClient.getAuthToken(codeResponse.code);
                                const token = tokenResponse.token;

                                connector.setAuthToken(token);
                                localStorage.setItem(""ytmd-token"", token);
                            } catch (error) {
                                console.log(error);
                                alert(""Failed to connect to YTMDesktop. Refresh the widget and try again"");
                            }
                        }

                        socketClient.connect();
                    ";
                    doc.DocumentNode.SelectSingleNode("html").SelectSingleNode("head").AppendChild(ytmdv2Api);

                    response.ContentType = "text/html";
                    doc.Save(response.OutputStream);

                    Console.WriteLine("Injected Amuse fix into widget request");
                }
                else
                {

                    response.ContentType = proxyResponse.Content.Headers.ContentType?.MediaType;
                    response.ContentLength64 = proxyResponse.Content.Headers.ContentLength ?? 0;

                    await proxyResponseStream.CopyToAsync(response.OutputStream);
                }
                response.Close();
            }
        }

        private static void CreateNotifyIcon(object? obj)
        {
            var tray = NotifyIcon.Create("tray.ico",
            [
                new("YTMDv2 6k Labs Amuse Proxy") {
                    IsDisabled = true
                },
                new SeparatorItem(),
                new("Exit")
                {
                    Click = (s, e) =>
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                }
            ]);
            tray.Show();
        }
    }
}
