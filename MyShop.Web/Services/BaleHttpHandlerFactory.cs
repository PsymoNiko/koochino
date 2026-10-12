using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MyShop.Web.Services
{
    /// <summary>
    /// کلاینت HTTPS برای بله روی هاست اشتراکی ویندوز.
    /// خطای WinHTTP 12175 اغلب به‌خاطر CRL/OCSP (revocation) بسته است — نه cipher.
    /// </summary>
    public static class BaleHttpHandlerFactory
    {
        public static HttpMessageHandler Create()
        {
            // اول Sockets با NoCheck — روی shared hosting معمولاً از WinHTTP 12175 بهتر است
            try
            {
                return CreateSocketsHandler();
            }
            catch
            {
                // fall through
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    return new WinHttpHandler
                    {
                        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        WindowsProxyUsePolicy = WindowsProxyUsePolicy.DoNotUseProxy,
                        SendTimeout = TimeSpan.FromSeconds(30),
                        ReceiveDataTimeout = TimeSpan.FromSeconds(30),
                        // نادیده گرفتن خطای امنیتی/revocation گواهی روی هاست محدود
                        ServerCertificateValidationCallback = static (_, _, _, _) => true
                    };
                }
                catch
                {
                    // fall through
                }
            }

            return CreateSocketsHandler();
        }

        private static SocketsHttpHandler CreateSocketsHandler()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                ConnectTimeout = TimeSpan.FromSeconds(20),
                SslOptions = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    // هاست اشتراکی اغلب به CRL دسترسی ندارد → Error 12175 / chain errors
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = static (sender, cert, chain, errors) =>
                    {
                        if (errors == SslPolicyErrors.None) return true;
                        // فقط برای زنجیره/نام — همچنان اتصال به API بله را مجاز کن
                        return true;
                    }
                }
            };

            handler.ConnectCallback = async (context, ct) =>
            {
                var host = context.DnsEndPoint.Host;
                var port = context.DnsEndPoint.Port;
                IPAddress[] addresses;
                try
                {
                    addresses = await Dns.GetHostAddressesAsync(host);
                }
                catch
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    await socket.ConnectAsync(context.DnsEndPoint, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }

                Exception last = null;
                foreach (var ip in addresses)
                {
                    if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                    var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    try
                    {
                        await socket.ConnectAsync(new IPEndPoint(ip, port), ct).ConfigureAwait(false);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception ex)
                    {
                        last = ex;
                        socket.Dispose();
                    }
                }

                var fallback = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await fallback.ConnectAsync(context.DnsEndPoint, ct);
                    return new NetworkStream(fallback, ownsSocket: true);
                }
                catch
                {
                    fallback.Dispose();
                    if (last != null) throw last;
                    throw;
                }
            };

            return handler;
        }
    }
}
