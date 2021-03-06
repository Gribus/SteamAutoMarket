﻿using System;

namespace SteamAutoMarket.Steam.Market.Exceptions
{
    public class RequestException : SteamException
    {
        public RequestException(string message) : base(message)
        {
        }

        public RequestException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}