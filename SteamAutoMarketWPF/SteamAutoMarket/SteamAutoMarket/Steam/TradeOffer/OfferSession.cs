namespace SteamAutoMarket.Steam.TradeOffer
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Net;

    using Newtonsoft.Json;

    using SteamAutoMarket.Core;
    using SteamAutoMarket.Steam.Auth;
    using SteamAutoMarket.Steam.Market.Exceptions;
    using SteamAutoMarket.Steam.TradeOffer.Enums;
    using SteamAutoMarket.Steam.TradeOffer.Models;

    using SteamKit2;

    public class OfferSession
    {
        private const string SendUrl = "https://steamcommunity.com/tradeoffer/new/send";

        private readonly CookieContainer _cookies;

        private readonly WebProxy _proxy;

        private readonly string _sessionId;

        private readonly TradeOfferWebApi _webApi;

        public OfferSession(TradeOfferWebApi webApi, CookieContainer cookies, string sessionId, WebProxy proxy = null)
        {
            this._webApi = webApi;
            this._cookies = cookies;
            this._sessionId = sessionId;
            this._proxy = proxy;

            this.JsonSerializerSettings = new JsonSerializerSettings
                                              {
                                                  PreserveReferencesHandling = PreserveReferencesHandling.None,
                                                  Formatting = Formatting.None
                                              };
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string Error { get; private set; }

        internal JsonSerializerSettings JsonSerializerSettings { get; set; }

        public TradeOfferAcceptResponse Accept(string tradeOfferId)
        {
            var data = new NameValueCollection
                           {
                               { "sessionid", this._sessionId }, { "serverid", "1" }, { "tradeofferid", tradeOfferId }
                           };

            var url = $"https://steamcommunity.com/tradeoffer/{tradeOfferId}/accept";
            var referer = $"https://steamcommunity.com/tradeoffer/{tradeOfferId}/";

            var resp = SteamWeb.Request(url, "POST", data, this._cookies, referer: referer, proxy: this._proxy);

            if (!string.IsNullOrEmpty(resp))
                try
                {
                    var res = JsonConvert.DeserializeObject<TradeOfferAcceptResponse>(resp);

                    // steam can return 'null' response
                    if (res != null)
                    {
                        res.Accepted = string.IsNullOrEmpty(res.TradeError);
                        return res;
                    }
                }
                catch (JsonException)
                {
                    return new TradeOfferAcceptResponse { TradeError = "Error parsing server response: " + resp };
                }

            // if it didn't work as expected, check the state, maybe it was accepted after all
            var state = this._webApi.GetOfferState(tradeOfferId);
            return new TradeOfferAcceptResponse { Accepted = state == TradeOfferState.TradeOfferStateAccepted };
        }

        public bool Cancel(string tradeOfferId)
        {
            var data = new NameValueCollection
                           {
                               { "sessionid", this._sessionId }, { "tradeofferid", tradeOfferId }, { "serverid", "1" }
                           };
            var url = string.Format("https://steamcommunity.com/tradeoffer/{0}/cancel", tradeOfferId);

            // should be http://steamcommunity.com/{0}/{1}/tradeoffers/sent/ - id/profile persona/id64 ideally
            var referer = string.Format("https://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);

            var resp = SteamWeb.Request(url, "POST", data, this._cookies, referer: referer, proxy: this._proxy);

            if (!string.IsNullOrEmpty(resp))
            {
                try
                {
                    var json = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                    if (json.TradeOfferId != null && json.TradeOfferId == tradeOfferId) return true;
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine("Error on cancel trade offer" + ex.Message);
                }
            }
            else
            {
                var state = this._webApi.GetOfferState(tradeOfferId);
                if (state == TradeOfferState.TradeOfferStateCanceled) return true;
            }

            return false;
        }

        public bool Decline(string tradeOfferId)
        {
            var data = new NameValueCollection
                           {
                               { "sessionid", this._sessionId }, { "serverid", "1" }, { "tradeofferid", tradeOfferId }
                           };

            var url = string.Format("https://steamcommunity.com/tradeoffer/{0}/decline", tradeOfferId);

            // should be http://steamcommunity.com/{0}/{1}/tradeoffers - id/profile persona/id64 ideally
            var referer = string.Format("https://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);

            var resp = SteamWeb.Request(url, "POST", data, this._cookies, referer: referer, proxy: this._proxy);

            if (!string.IsNullOrEmpty(resp))
            {
                try
                {
                    var json = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                    if (json.TradeOfferId != null && json.TradeOfferId == tradeOfferId) return true;
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine("Error on decline trade offer" + ex.Message);
                }
            }
            else
            {
                var state = this._webApi.GetOfferState(tradeOfferId);
                if (state == TradeOfferState.TradeOfferStateDeclined) return true;
            }

            return false;
        }

        /// <summary>
        ///     Creates a new trade offer
        /// </summary>
        /// <param name="message">A message to include with the trade offer</param>
        /// <param name="otherSteamId">The SteamID of the partner we are trading with</param>
        /// <param name="status">The list of items we and they are going to trade</param>
        /// <returns>True if successfully returns a newTradeOfferId, else false</returns>
        public string SendTradeOffer(string message, SteamID otherSteamId, TradeStatus status)
        {
            var data = new NameValueCollection
                           {
                               { "sessionid", this._sessionId },
                               { "serverid", "1" },
                               { "partner", otherSteamId.ConvertToUInt64().ToString() },
                               { "tradeoffermessage", message },
                               { "json_tradeoffer", JsonConvert.SerializeObject(status, this.JsonSerializerSettings) },
                               { "trade_offer_create_params", "{}" }
                           };

            var referer = string.Format(
                "https://steamcommunity.com/tradeoffer/new/?partner={0}",
                otherSteamId.AccountID);

            return this.Request(SendUrl, data, referer);
        }

        /// <summary>
        ///     Creates a new trade offer with a token
        /// </summary>
        /// <param name="message">A message to include with the trade offer</param>
        /// <param name="otherSteamId">The SteamID of the partner we are trading with</param>
        /// <param name="status">The list of items we and they are going to trade</param>
        /// <param name="token">The token of the partner we are trading with</param>
        /// <returns>True if successfully returns a newTradeOfferId, else false</returns>
        public string SendTradeOfferWithToken(string message, SteamID otherSteamId, TradeStatus status, string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token), @"Partner trade offer token is missing");

            var offerToken = new OfferAccessToken { TradeOfferAccessToken = token };

            var data = new NameValueCollection
                           {
                               { "sessionid", this._sessionId },
                               { "captcha", string.Empty },
                               { "serverid", "1" },
                               { "partner", otherSteamId.ConvertToUInt64().ToString() },
                               { "tradeoffermessage", message },
                               { "json_tradeoffer", JsonConvert.SerializeObject(status, this.JsonSerializerSettings) },
                               {
                                   "trade_offer_create_params",
                                   JsonConvert.SerializeObject(offerToken, this.JsonSerializerSettings)
                               }
                           };

            var referer = $"https://steamcommunity.com/tradeoffer/new/?partner={otherSteamId.AccountID}&token={token}";

            return this.Request(SendUrl, data, referer);
        }

        internal string Request(string url, NameValueCollection data, string referer)
        {
            string resp;

            try
            {
                resp = SteamWeb.RequestWithException(
                    url,
                    "POST",
                    data,
                    this._cookies,
                    referer: referer,
                    proxy: this._proxy);
            }
            catch (Exception e)
            {
                throw new SteamException($"Steam error: {e.Message}", e);
            }

            try
            {
                var offerResponse = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                if (!string.IsNullOrEmpty(offerResponse.TradeOfferId))
                {
                    return offerResponse.TradeOfferId;
                }

                throw new SteamException(offerResponse.TradeError);
            }
            catch (JsonException ex)
            {
                Logger.Log.Debug(resp);
                throw new SteamException($"Unknown steam response - {ex.Message}");
            }
        }
    }
}