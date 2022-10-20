
export const StringOperationTypes : string [] = [
    "Exact",
    "Contains",
    "StartsWith",
    "EndsWith",
    "Regex",
];

export const CheckRegexValidity = (input : string) : boolean  => {
    try {
        new RegExp(input);
    } catch(e) {
        return false;
    }
    return true;
}


export const RequestHeaderNames : string[] = ["A-IM", "Accept", "Accept-Charset", "Accept-Datetime", "Accept-Encoding", "Accept-Language", "Access-Control-Request-Method,",
    "Access-Control-Request-Headers", "Authorization", "Cache-Control", "Connection", "Content-Encoding", "Content-Length", "Content-MD5", "Content-Type",
    "Cookie", "Date", "Expect", "Forwarded", "From", "Host", "HTTP2-Settings", "If-Match", "If-Modified-Since", "If-None-Match", "If-Range", "If-Unmodified-Since",
    "Max-Forwards", "Origin", "Pragma", "Prefer", "Proxy-Authorization", "Range", "Referer ", "TE", "Trailer", "Transfer-Encoding", "User-Agent", "Upgrade",
    "Via", "Warning", "Upgrade-Insecure-Requests", "X-Requested-With", "DNT", "X-Forwarded-For", "X-Forwarded-Host", "X-Forwarded-Proto", "Front-End-Https",
    "X-Http-Method-Override", "X-ATT-DeviceId", "X-Wap-Profile", "Proxy-Connection", "X-UIDH", "X-Csrf-Token", "X-Request-ID,", "X-Correlation-ID,", "Save-Data",
];


export const ResponseHeaderNames : string [] = ["Accept-CH", "Access-Control-Allow-Origin", "Access-Control-Allow-Credentials", "Access-Control-Expose-Headers",
    "Access-Control-Max-Age", "Access-Control-Allow-Methods", "Access-Control-Allow-Headers", "Accept-Patch", "Accept-Ranges", "Age", "Allow", "Alt-Svc",
    "Cache-Control", "Connection", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-MD5",
    "Content-Range", "Content-Type", "Date", "Delta-Base", "ETag", "Expires", "IM", "Last-Modified", "Link", "Location", "P3P", "Pragma", "Preference-Applied",
    "Proxy-Authenticate", "Public-Key-Pins", "Retry-After", "Server", "Set-Cookie", "Strict-Transport-Security", "Trailer", "Transfer-Encoding", "Tk",
    "Upgrade", "Vary", "Via", "Warning", "WWW-Authenticate", "X-Frame-Options", "Content-Security-Policy", "X-Content-Security-Policy", "X-WebKit-CSP",
    "Expect-CT", "NEL", "Permissions-Policy", "Refresh", "Report-To", "Status", "Timing-Allow-Origin", "X-Content-Duration", "X-Content-Type-Options",
    "X-Powered-By", "X-Redirect-By", "X-Request-ID", "X-Correlation-ID", "X-UA-Compatible", "X-XSS-Protection",];
