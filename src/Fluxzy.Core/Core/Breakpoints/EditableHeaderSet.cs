// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fluxzy.Clients;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Core.Breakpoints
{
    public class EditableRequestHeaderSet :
        EditableHeaderSet
    {
        private EditableRequestHeaderSet(List<EditableHeader> headers)
            : base(headers)
        {
        }
        

        public static EditableHeaderParsingResult TryParse(
            string rawHttp11, int payloadLength, out EditableRequestHeaderSet? result)
        {
            result = null;

            rawHttp11 = rawHttp11.Trim('\r', '\n');

            List<HeaderField> headers;

            try {
                headers = Http11Parser.Read(rawHttp11.AsMemory()).ToList();
            }
            catch (Exception) {
                return new EditableHeaderParsingResult("Some header lines are invalid");
            }

            if (!headers.Any(h => h.Name.Equals(Http11Constants.MethodVerb)))
                return new EditableHeaderParsingResult("Request header does not contain method");

            if (!headers.Any(h => h.Name.Equals(Http11Constants.PathVerb)))
                return new EditableHeaderParsingResult("Request header does not contain path");

            if (!headers.Any(h => h.Name.Equals(Http11Constants.SchemeVerb)))
                return new EditableHeaderParsingResult("Request header does not contain scheme");

            // We are removing any content length header 

            headers.RemoveAll(t => t.Name.Span.Equals(Http11Constants.ContentLength.Span, StringComparison.OrdinalIgnoreCase));
            headers.RemoveAll(t => Http11Constants.UnEditableHeaders.Contains(t.Name));

            result = new EditableRequestHeaderSet(
                headers.Select(h => new EditableHeader(
                h.Name.ToString(), h.Value.ToString())).ToList());

            result.Headers.Add(
                new EditableHeader(Http11Constants.ContentLength.ToString(),
                    payloadLength.ToString()));

            return new EditableHeaderParsingResult(true);
        }

        public Request ToRequest(Stream ? body)
        {
            var request = new Request(new RequestHeader(Headers.Select(s => new HeaderField(s.Name, s.Value))));

            request.Body = body;

            return request;
        }
    }

    public class EditableResponseHeaderSet :
        EditableHeaderSet
    {
        private EditableResponseHeaderSet(List<EditableHeader> headers)
            : base(headers)
        {
        }
        
        public Response ToResponse(Stream ? body)
        {
            var response = new Response {
                Header = new ResponseHeader(Headers.Select(s => new HeaderField(s.Name, s.Value))),
                Body = body
            };
            return response;
        }

        public static EditableHeaderParsingResult TryParse(
            string rawHttp11, int payloadLength, out EditableResponseHeaderSet? result)
        {
            result = null;

            rawHttp11 = rawHttp11.Trim('\r', '\n');

            List<HeaderField> headers;

            try {
                headers = Http11Parser.Read(rawHttp11.AsMemory()).ToList();
            }
            catch (Exception) {
                return new EditableHeaderParsingResult("Some header lines are invalid");
            }

            if (!headers.Any(h => h.Name.Equals(Http11Constants.StatusVerb)))
                return new EditableHeaderParsingResult("Request header does not contain method");

            // We are removing any content length header 

            headers.RemoveAll(t =>
                t.Name.Span.Equals(Http11Constants.ContentLength.Span, StringComparison.OrdinalIgnoreCase));

            headers.RemoveAll(t =>
                t.Name.Span.Equals(Http11Constants.ContentType.Span, StringComparison.OrdinalIgnoreCase));

            headers.RemoveAll(t => Http11Constants.UnEditableHeaders.Contains(t.Name));

            result = new EditableResponseHeaderSet(headers.Select(h => new EditableHeader(
                h.Name.ToString(), h.Value.ToString())).ToList());

            result.Headers.Add(
                new EditableHeader(Http11Constants.ContentLength.ToString(),
                    payloadLength.ToString()));

            return new EditableHeaderParsingResult(true);
        }
    }

    public class EditableHeaderSet
    {
        public EditableHeaderSet(List<EditableHeader> headers)
        {
            Headers = headers;
        }

        public List<EditableHeader> Headers { get; }
    }

    public class EditableHeader
    {
        public EditableHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }

        public bool ComputeOnly { get; set; }
    }

    public class EditableHeaderParsingResult
    {
        public EditableHeaderParsingResult(bool success)
            : this(success, null)
        {
        }

        public EditableHeaderParsingResult(string message)
            : this(false, message)
        {
        }

        public EditableHeaderParsingResult(bool success, string? message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }

        public string? Message { get; }
    }
}
