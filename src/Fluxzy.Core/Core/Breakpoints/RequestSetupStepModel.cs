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
    public class RequestSetupStepModel : IBreakPointAlterationModel
    {
        public bool Done { get; private set; }

        public string? FlatHeader { get; set; } = string.Empty;
        
        public bool FromFile { get; set; }

        public string? FileName { get; set; }

        public string? ContentBody { get; set; }

        public string? ContentType { get; set; }

        public long BodyLength { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FlatHeader))
                yield break;

            if (true) {
                if (FromFile) {
                    if (string.IsNullOrWhiteSpace(FileName))
                        yield return new ValidationResult("File name is required", new[] {nameof(FileName)});
                    else if (!File.Exists(FileName))
                        yield return new ValidationResult("File does not exist", new[] {nameof(FileName)});
                }
            }

            var tryParseResult = EditableRequestHeaderSet.TryParse(FlatHeader,
                1, out var headerSet);

            if (!tryParseResult.Success)
                yield return new ValidationResult(tryParseResult.Message, new[] {nameof(FlatHeader) });
        }

        public async ValueTask Init(Exchange exchange)
        {
            FlatHeader = exchange.Request.ToString();

            // rewind body 

            // Drinking request body to temp file or memory stream

            if (exchange.Request.Body != null) {
                var tempFileName = ExtendedPathHelper.GetTempFileName();

                await using (var fileStream = File.Create(tempFileName)) {
                    await exchange.Request.Body.CopyToAsync(fileStream).ConfigureAwait(false);
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

                ContentType = exchange.GetRequestHeaderValue("Content-type") ?? "application/octet-stream";
            }
        }

        public ValueTask Alter(Exchange exchange)
        {
            // Gather the request body 

            // Request body stream is dead, already drinked by Init 
            // must retrieved back from this model 


            Stream? body;

            if (FromFile)
                body = FileName != null && File.Exists(FileName) ? File.OpenRead(FileName) : Stream.Null;
            else {
                body = string.IsNullOrEmpty(ContentBody)
                    ? Stream.Null
                    : new MemoryStream(Encoding.UTF8.GetBytes(ContentBody));
            }

            var tryParseResult =
                EditableRequestHeaderSet.TryParse(FlatHeader!, (int) (body?.Length ?? 0), out var headerSet);

            if (!tryParseResult.Success)
                throw new ClientErrorException(0, "User provided header was invalid");

            if (ContentType != null)
                headerSet!.Headers.Add(new EditableHeader("Content-Type", ContentType));

            var request = headerSet!.ToRequest(body);

            exchange.Request.Header = request.Header;
            exchange.Request.Body = request.Body; // Header must be changed when we alter body 


            Done = true;

            return default;
        }
    }
}
