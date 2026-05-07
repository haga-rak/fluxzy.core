# 528 transport error and `x-fluxzy-network-error`

When Fluxzy fails to relay a request because of an upstream network, DNS, or TLS
problem it returns a synthesized `528 Fluxzy transport error` response (or `502
Bad Gateway` when `FluxzySharedSetting.Use528 = false`).

To let programmatic consumers react without parsing the body, every synthesized
528 carries an extra response header:

```
x-fluxzy-network-error: <token>
```

The same token is persisted on `ClientError.NetworkErrorCode` in archives
(`.fxzy` / HAR) so post-mortem readers can recover it.

The header value is one of the stable identifiers below, defined as
`public const string` on `Fluxzy.Core.NetworkErrorCodes`. The strings are
considered a public contract and will not change without a major version bump.

## Token reference

### Connection layer

| Token                  | Description                                                                                       |
| ---------------------- | ------------------------------------------------------------------------------------------------- |
| `connection_refused`   | The remote peer responded but actively refused the TCP connection (RST on SYN).                   |
| `connection_reset`     | The remote peer reset an established connection.                                                  |
| `connection_aborted`   | The connection was aborted, often because the remote half closed without a clean FIN.             |
| `connection_timeout`   | The remote peer could not be contacted within the configured TCP connect timeout.                 |
| `host_unreachable`     | The OS reports the remote host as unreachable (no route, ICMP host-unreachable).                  |
| `network_unreachable`  | The OS reports the remote network as unreachable (no route at the network level).                 |
| `connection_closed`    | The remote peer closed the TCP connection while Fluxzy was reading the response header.           |

### DNS layer

| Token            | Description                                                                                                |
| ---------------- | ---------------------------------------------------------------------------------------------------------- |
| `dns_notfound`   | The DNS server returned NXDOMAIN: the requested host does not exist.                                       |
| `dns_no_data`    | The DNS server resolved the name but returned no usable A/AAAA record.                                     |
| `dns_try_again`  | The DNS server returned a transient failure (SERVFAIL or equivalent); a retry might succeed.               |
| `dns_failure`    | Generic DNS resolution failure: malformed response, unreachable resolver, DoH endpoint returning non-2xx.  |

### TLS layer

| Token                          | Description                                                                                                |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------- |
| `tls_cert_expired`             | The server certificate is past its validity period (or not yet valid).                                     |
| `tls_cert_hostname_mismatch`   | The server certificate is valid but does not cover the requested hostname (Subject / SAN mismatch).        |
| `tls_cert_untrusted`           | The server certificate chain does not chain to a trusted root (untrusted root, partial chain, no policy).  |
| `tls_cert_invalid`             | Other certificate-policy failure: revoked, malformed, unknown to the validator, or required but missing.   |
| `tls_handshake_failure`        | TLS handshake failed for a reason that is not specific to the certificate (alert, version, cipher, ...).   |

### Other

| Token            | Description                                                                                                                 |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `protocol_error` | An HTTP/2 stream protocol error happened before the response header was received.                                           |
| `rule_failure`   | A user-supplied Fluxzy rule (filter or action) threw during exchange processing. See the body for the rule diagnostic.      |
| `unknown`        | Fallback when none of the categories above match. Typically a wrapped exception that did not carry enough information.      |

## Example

```http
HTTP/1.1 528 Fluxzy error
x-fluxzy: Fluxzy error
x-fluxzy-network-error: tls_cert_expired
Content-length: 7520
Content-type: text/html; charset=utf-8
Connection: close

<html>...</html>
```

## Reading the token from C#

```csharp
using var response = await httpClient.GetAsync(url);

if ((int)response.StatusCode == 528 &&
    response.Headers.TryGetValues("x-fluxzy-network-error", out var values))
{
    var token = values.Single();
    // e.g. switch on Fluxzy.Core.NetworkErrorCodes.* constants
}
```

## Reading the token from an archive

```csharp
foreach (var clientError in exchange.ClientErrors)
{
    var token = clientError.NetworkErrorCode; // null on success
}
```

## See also

- `Fluxzy.Core.NetworkErrorCodes` (source: `src/Fluxzy.Core/Core/NetworkErrorCodes.cs`)
- `ClientError.NetworkErrorCode` (source: `src/Fluxzy.Core/ClientError.cs`)
- `NetworkErrorFilter` selects exchanges with `StatusCode == 528`
  (source: `src/Fluxzy.Core/Rules/Filters/ResponseFilters/NetworkErrorFilter.cs`)
