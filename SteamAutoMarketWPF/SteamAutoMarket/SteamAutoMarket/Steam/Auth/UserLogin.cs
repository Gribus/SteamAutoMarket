﻿namespace SteamAutoMarket.Steam.Auth
{
    using System;
    using System.Collections.Specialized;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    using Newtonsoft.Json;

    /// <summary>
    /// Handles logging the user into the mobile Steam website. Necessary to generate OAuth token and session cookies.
    /// </summary>
    public class UserLogin
    {
        public string CaptchaGID;

        public string CaptchaText = null;

        public string EmailCode = null;

        public string EmailDomain = null;

        public bool LoggedIn;

        public string Password;

        public bool Requires2FA;

        public bool RequiresCaptcha;

        public bool RequiresEmail;

        public SessionData Session;

        public ulong SteamID;

        public string TwoFactorCode = null;

        public string Username;

        private readonly CookieContainer _cookies = new CookieContainer();

        private readonly WebProxy proxy;

        public UserLogin(string username, string password, WebProxy proxy = null)
        {
            this.Username = username;
            this.Password = password;
            this.proxy = proxy;
        }

        public LoginResult DoLogin()
        {
            var postData = new NameValueCollection();
            var cookies = this._cookies;

            if (cookies.Count == 0)
            {
                // Generate a SessionID
                cookies.Add(new Cookie("mobileClientVersion", "0 (2.1.3)", "/", ".steamcommunity.com"));
                cookies.Add(new Cookie("mobileClient", "android", "/", ".steamcommunity.com"));
                cookies.Add(new Cookie("Steam_Language", "english", "/", ".steamcommunity.com"));

                var headers = new NameValueCollection
                                  {
                                      { "X-Requested-With", "com.valvesoftware.android.steam.community" }
                                  };

                SteamWeb.MobileLoginRequest(
                    "https://steamcommunity.com/login?oauth_client_id=DE45CD61&oauth_scope=read_profile%20write_profile%20read_client%20write_client",
                    "GET",
                    null,
                    cookies,
                    headers,
                    this.proxy);
            }

            postData.Add("donotcache", (TimeAligner.GetSteamTime() * 1000).ToString());
            postData.Add("username", this.Username);
            string response = SteamWeb.MobileLoginRequest(
                ApiEndpoints.CommunityBase + "/login/getrsakey",
                "POST",
                postData,
                cookies,
                proxy: this.proxy);
            if (response == null || response.Contains("<BODY>\nAn error occurred while processing your request."))
                return LoginResult.GeneralFailure;

            var rsaResponse = JsonConvert.DeserializeObject<RSAResponse>(response);

            if (!rsaResponse.Success)
            {
                return LoginResult.BadRSA;
            }

            Thread.Sleep(350); // Sleep for a bit to give Steam a chance to catch up??

            byte[] encryptedPasswordBytes;
            using (var rsaEncryptor = new RSACryptoServiceProvider())
            {
                var passwordBytes = Encoding.ASCII.GetBytes(this.Password);
                var rsaParameters = rsaEncryptor.ExportParameters(false);
                rsaParameters.Exponent = Util.HexStringToByteArray(rsaResponse.Exponent);
                rsaParameters.Modulus = Util.HexStringToByteArray(rsaResponse.Modulus);
                rsaEncryptor.ImportParameters(rsaParameters);
                encryptedPasswordBytes = rsaEncryptor.Encrypt(passwordBytes, false);
            }

            var encryptedPassword = Convert.ToBase64String(encryptedPasswordBytes);

            postData.Clear();
            postData.Add("donotcache", (TimeAligner.GetSteamTime() * 1000).ToString());

            postData.Add("password", encryptedPassword);
            postData.Add("username", this.Username);
            postData.Add("twofactorcode", this.TwoFactorCode ?? string.Empty);

            postData.Add("emailauth", this.RequiresEmail ? this.EmailCode : string.Empty);
            postData.Add("loginfriendlyname", string.Empty);
            postData.Add("captchagid", this.RequiresCaptcha ? this.CaptchaGID : "-1");
            postData.Add("captcha_text", this.RequiresCaptcha ? this.CaptchaText : string.Empty);
            postData.Add(
                "emailsteamid",
                (this.Requires2FA || this.RequiresEmail) ? this.SteamID.ToString() : string.Empty);

            postData.Add("rsatimestamp", rsaResponse.Timestamp);
            postData.Add("remember_login", "true");
            postData.Add("oauth_client_id", "DE45CD61");
            postData.Add("oauth_scope", "read_profile write_profile read_client write_client");

            response = SteamWeb.MobileLoginRequest(
                ApiEndpoints.CommunityBase + "/login/dologin",
                "POST",
                postData,
                cookies,
                proxy: this.proxy);
            if (response == null) return LoginResult.GeneralFailure;

            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(response);

            if (loginResponse.Message != null && loginResponse.Message.Contains("Incorrect login"))
            {
                return LoginResult.BadCredentials;
            }

            if (loginResponse.CaptchaNeeded)
            {
                this.RequiresCaptcha = true;
                this.CaptchaGID = loginResponse.CaptchaGID;
                return LoginResult.NeedCaptcha;
            }

            if (loginResponse.EmailAuthNeeded)
            {
                this.RequiresEmail = true;
                this.SteamID = loginResponse.EmailSteamID;
                return LoginResult.NeedEmail;
            }

            if (loginResponse.TwoFactorNeeded && !loginResponse.Success)
            {
                this.Requires2FA = true;
                return LoginResult.Need2FA;
            }

            if (loginResponse.Message != null && loginResponse.Message.Contains("too many login failures"))
            {
                return LoginResult.TooManyFailedLogins;
            }

            if (loginResponse.OAuthData == null || loginResponse.OAuthData.OAuthToken == null
                                                || loginResponse.OAuthData.OAuthToken.Length == 0)
            {
                return LoginResult.GeneralFailure;
            }

            if (!loginResponse.LoginComplete)
            {
                return LoginResult.BadCredentials;
            }

            var readableCookies = cookies.GetCookies(new Uri("https://steamcommunity.com"));
            var oAuthData = loginResponse.OAuthData;

            var session = new SessionData { OAuthToken = oAuthData.OAuthToken, SteamID = oAuthData.SteamID };
            session.SteamLogin = session.SteamID + "%7C%7C" + oAuthData.SteamLogin;
            session.SteamLoginSecure = session.SteamID + "%7C%7C" + oAuthData.SteamLoginSecure;
            session.WebCookie = oAuthData.Webcookie;
            session.SessionID = readableCookies["sessionid"]?.Value;
            this.Session = session;
            this.LoggedIn = true;
            return LoginResult.LoginOkay;
        }

        private class LoginResponse
        {
            [JsonProperty("captcha_gid")]
            public string CaptchaGID { get; set; }

            [JsonProperty("captcha_needed")]
            public bool CaptchaNeeded { get; set; }

            [JsonProperty("emailauth_needed")]
            public bool EmailAuthNeeded { get; set; }

            [JsonProperty("emailsteamid")]
            public ulong EmailSteamID { get; set; }

            [JsonProperty("login_complete")]
            public bool LoginComplete { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            public OAuth OAuthData
            {
                get
                {
                    return this.OAuthDataString != null
                               ? JsonConvert.DeserializeObject<OAuth>(this.OAuthDataString)
                               : null;
                }
            }

            [JsonProperty("oauth")]
            public string OAuthDataString { get; set; }

            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("requires_twofactor")]
            public bool TwoFactorNeeded { get; set; }

            internal class OAuth
            {
                [JsonProperty("oauth_token")]
                public string OAuthToken { get; set; }

                [JsonProperty("steamid")]
                public ulong SteamID { get; set; }

                [JsonProperty("wgtoken")]
                public string SteamLogin { get; set; }

                [JsonProperty("wgtoken_secure")]
                public string SteamLoginSecure { get; set; }

                [JsonProperty("webcookie")]
                public string Webcookie { get; set; }
            }
        }

        private class RSAResponse
        {
            [JsonProperty("publickey_exp")]
            public string Exponent { get; set; }

            [JsonProperty("publickey_mod")]
            public string Modulus { get; set; }

            [JsonProperty("steamid")]
            public ulong SteamID { get; set; }

            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("timestamp")]
            public string Timestamp { get; set; }
        }
    }
}