// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Extensions;
using Fluxzy.Misc;
using Fluxzy.Utils;

namespace Fluxzy.Core.Breakpoints
{
    public class ResponseSetupStepModel : IBreakPointAlterationModel
    {
        public string? FlatHeader { get; set; } = string.Empty;

        public bool FromFile { get; set; }

        public string? FileName { get; set; }

        public string? ContentBody { get; set; }

        public string? ContentType { get; set; }

        public long BodyLength { get; set; }

        public bool Done { get; private set; }

        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FlatHeader)) {
                yield break;
            }

            if (true) {
                if (FromFile) {
                    if (string.IsNullOrWhiteSpace(FileName)) {
                        yield return new System.ComponentModel.DataAnnotations.ValidationResult("File name is required",
                            new[] { nameof(FileName) });
                    }
                    else if (!File.Exists(FileName)) {
                        yield return new System.ComponentModel.DataAnnotations.ValidationResult("File does not exist",
                            new[] { nameof(FileName) });
                    }
                }
            }

            var tryParseResult = EditableResponseHeaderSet.TryParse(FlatHeader,
                1, out var headerSet);

            if (!tryParseResult.Success) {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult(tryParseResult.Message,
                    new[] { nameof(ContentBody) });
            }
        }

        public async ValueTask Init(Exchange exchange)
        {
            FlatHeader = exchange.Response.ToString();

            // rewind body 

            if (exchange.Response.Body != null) {
                var tempFileName = ExtendedPathHelper.GetTempFileName();

                await using (var fileStream = File.Create(tempFileName)) {
                    // TODO : we need to decode here 

                    await new ExchangeInfo(exchange).GetDecodedResponseBodyStream(exchange.Response.Body, out _)
                                                    .CopyToAsync(fileStream).ConfigureAwait(false);
                }

                var tempFileInfo = new FileInfo(tempFileName);

                var fileLength = tempFileInfo.Length;

                FromFile = true;
                FileName = tempFileInfo.FullName;

                if (fileLength < 0x10000) {
                    var isText = ArrayTextUtilities.IsText(tempFileName, 0x10000);

                    if (isText) {
                        FromFile = false;
                        ContentBody = await File.ReadAllTextAsync(tempFileName).ConfigureAwait(false);
                    }
                }

                BodyLength = fileLength;

                ContentType = exchange.GetResponseHeaderValue("Content-type") ?? "application/octet-stream";
            }
        }

        public ValueTask Alter(Exchange exchange)
        {
            Stream? body;

            if (FromFile) {
                body = FileName != null && File.Exists(FileName) ? File.OpenRead(FileName) : Stream.Null;
            }
            else {
                body = string.IsNullOrEmpty(ContentBody)
                    ? Stream.Null
                    : new MemoryStream(Encoding.UTF8.GetBytes(ContentBody));
            }

            var tryParseResult =
                EditableResponseHeaderSet.TryParse(FlatHeader!, (int) (body?.Length ?? 0), out var headerSet);

            if (!tryParseResult.Success) {
                throw new ClientErrorException(0, "User provided header was invalid");
            }


            if (ContentType != null) {
                headerSet!.Headers.Add(new EditableHeader("Content-Type", ContentType));
            }

            var response = headerSet!.ToResponse(body);

            exchange.Response.Header = response.Header;
            exchange.Response.Body = response.Body; // Header must be changed when we alter body 


            Done = true;

            return default;
        }
    }
}
