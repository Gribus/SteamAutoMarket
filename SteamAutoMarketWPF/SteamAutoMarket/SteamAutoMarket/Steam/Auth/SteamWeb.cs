﻿namespace SteamAutoMarket.Steam.Auth
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;

    using log4net.Core;

    using SteamAutoMarket.Core;

    public class SteamWeb
    {
        /// <summary>
        /// Perform a mobile login request
        /// </summary>
        /// <param name="url">API url</param>
        /// <param name="method">GET or POST</param>
        /// <param name="data">Name-data pairs</param>
        /// <param name="cookies">current cookie container</param>
        /// <param name="headers">Headers</param>
        /// <param name="proxy">Proxy</param>
        /// <returns>response body</returns>
        public static string MobileLoginRequest(
            string url,
            string method,
            NameValueCollection data = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            WebProxy proxy = null)
        {
            return Request(
                url,
                method,
                data,
                cookies,
                headers,
                ApiEndpoints.CommunityBase
                + "/mobilelogin?oauth_client_id=DE45CD61&oauth_scope=read_profile%20write_profile%20read_client%20write_client",
                proxy);
        }

        public static string Request(
            string url,
            string method,
            NameValueCollection data = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = ApiEndpoints.CommunityBase,
            WebProxy proxy = null)
        {
            string query;

            if (data == null)
            {
                query = string.Empty;
            }
            else
            {
                query = string.Join(
                    "&",
                    Array.ConvertAll(
                        data.AllKeys,
                        key => $"{WebUtility.UrlEncode(key)}={WebUtility.UrlEncode(data[key])}"));
            }

            if (method == "GET")
            {
                url += (url.Contains("?") ? "&" : "?") + query;
            }

            return Request(url, method, query, cookies, headers, referer, proxy);
        }

        public static string Request(
            string url,
            string method,
            string dataString = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = ApiEndpoints.CommunityBase,
            WebProxy proxy = null)
        {
            if (Logger.CurrentLogLevel == Level.Debug)
            {
                Logger.Log.Debug(
                    $"Steam {method} request to {url}.\nData - {dataString}.\nCookies - {StringUtils.CookieContainerToString(cookies)}.\nHeaders {StringUtils.NameValueCollectionToString(headers)}.\nReferer {referer}.\nProxy {proxy?.Address}");
            }

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            request.UserAgent =
                "Mozilla/5.0 (Linux; U; Android 4.1.1; en-us; Google Nexus 4 - 4.1.1 - API 16 - 768x1280 Build/JRO03S) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.Referer = referer;

            if (proxy != null)
            {
                request.Proxy = proxy;
            }

            if (headers != null)
            {
                request.Headers.Add(headers);
            }

            if (cookies != null)
            {
                request.CookieContainer = cookies;
            }

            if (method == "POST")
            {
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.ContentLength = dataString?.Length ?? 0;

                var requestStream = new StreamWriter(request.GetRequestStream());
                requestStream.Write(dataString);
                requestStream.Close();
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        HandleFailedWebRequestResponse(response, url);
                        return null;
                    }

                    var responseStream = response.GetResponseStream();
                    if (responseStream == null)
                    {
                        HandleFailedWebRequestResponse(response, url);
                        return null;
                    }

                    using (var responseStreamReader = new StreamReader(responseStream))
                    {
                        var responseData = responseStreamReader.ReadToEnd();

                        if (Logger.CurrentLogLevel == Level.Debug)
                        {
                            Logger.Log.Debug(StringUtils.ClearString(responseData));
                        }

                        return responseData;
                    }
                }
            }
            catch (WebException e)
            {
                HandleFailedWebRequestResponse(e.Response as HttpWebResponse, url);
                return null;
            }
        }

        public static string RequestWithException(
            string url,
            string method,
            NameValueCollection data = null,
            CookieContainer cookies = null,
            NameValueCollection headers = null,
            string referer = ApiEndpoints.CommunityBase,
            WebProxy proxy = null)
        {
            string dataString;

            if (data == null)
            {
                dataString = string.Empty;
            }
            else
            {
                dataString = string.Join(
                    "&",
                    Array.ConvertAll(
                        data.AllKeys,
                        key => $"{WebUtility.UrlEncode(key)}={WebUtility.UrlEncode(data[key])}"));
            }

            if (method == "GET")
            {
                url += (url.Contains("?") ? "&" : "?") + dataString;
            }

            if (Logger.CurrentLogLevel == Level.Debug)
            {
                Logger.Log.Debug(
                    $"Steam {method} request to {url}.\nData - {dataString}.\nCookies - {StringUtils.CookieContainerToString(cookies)}.\nHeaders {StringUtils.NameValueCollectionToString(headers)}.\nReferer {referer}.\nProxy {proxy?.Address}");
            }

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            request.UserAgent =
                "Mozilla/5.0 (Linux; U; Android 4.1.1; en-us; Google Nexus 4 - 4.1.1 - API 16 - 768x1280 Build/JRO03S) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.Referer = referer;

            if (proxy != null)
            {
                request.Proxy = proxy;
            }

            if (headers != null)
            {
                request.Headers.Add(headers);
            }

            if (cookies != null)
            {
                request.CookieContainer = cookies;
            }

            if (method == "POST")
            {
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.ContentLength = dataString.Length;

                var requestStream = new StreamWriter(request.GetRequestStream());
                requestStream.Write(dataString);
                requestStream.Close();
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    HandleFailedWebRequestResponse(response, url);
                    return null;
                }
                var responseStream = response.GetResponseStream();
                if (responseStream == null)
                {
                    HandleFailedWebRequestResponse(response, url);
                    return null;
                }

                using (var responseStreamReader = new StreamReader(responseStream))
                {
                    var responseData = responseStreamReader.ReadToEnd();

                    if (Logger.CurrentLogLevel == Level.Debug)
                    {
                        Logger.Log.Debug(StringUtils.ClearString(responseData));
                    }

                    return responseData;
                }
            }
        }

        /// <summary>
        /// Raise exceptions relevant to this HttpWebResponse -- EG, to signal that our oauth token has expired.
        /// </summary>
        private static void HandleFailedWebRequestResponse(HttpWebResponse response, string requestURL)
        {
            if (Logger.CurrentLogLevel == Level.Debug)
            {
                Logger.Log.Debug($"Response failed with error - {response.StatusDescription}");
            }

            if (response == null) return;

            // Redirecting -- likely to a steammobile:// URI
            if (response.StatusCode == HttpStatusCode.Found)
            {
                var location = response.Headers.Get("Location");
                if (!string.IsNullOrEmpty(location))
                {
                    // Our OAuth token has expired. This is given both when we must refresh our session, or the entire OAuth Token cannot be refreshed anymore.
                    // Thus, we should only throw this exception when we're attempting to refresh our session.
                    if (location == "steammobile://lostauth" && requestURL == ApiEndpoints.MobileAuthGetWgToken)
                    {
                        throw new SteamGuardAccount.WGTokenExpiredException();
                    }
                }
            }
        }
    }
}