using Fluxzy.Formatters.Producers.Responses;

namespace Fluxzy
{
    public class CookieTrackingEvent
    {
        public CookieTrackingEvent(CookieUpdateType updateType, string value, ExchangeInfo exchangeInfo)
        {
            UpdateType = updateType;
            Value = value;
            ExchangeInfo = exchangeInfo;
        }

        public CookieUpdateType UpdateType { get; }

        public string Value { get; }

        public ExchangeInfo ExchangeInfo { get; }

        public SetCookieItem? SetCookieItem { get; set; }

        public override string ToString()
        {
            return $"{UpdateType} {Value} {ExchangeInfo.Method} {ExchangeInfo.FullUrl}";
        }
    }
}