// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Misc;

namespace Fluxzy.Core.Breakpoints
{
    public class RequestSetupStepModel : IBreakPointAlterationModel
    {
        public bool Done { get; private set; }

        public string? FlatHeader { get; set; } = string.Empty;

        public bool EditBody { get; set; }

        public bool FromFile { get; set; }

        public string? FileName { get; set; }

        public string? ContentBody { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FlatHeader))
                yield break;

            if (EditBody) {
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
                yield return new ValidationResult(tryParseResult.Message, new[] {nameof(EditBody)});
        }

        public async ValueTask Init(Exchange exchange)
        {
            FlatHeader = exchange.Request.ToString();

            // rewind body 

            // Drinking request body to temp file or memory stream

            if (exchange.Request.Body != null) {
                var tempFileName = Path.GetTempFileName();

                await using (var fileStream = File.Create(tempFileName)) {
                    await exchange.Request.Body.CopyToAsync(fileStream);
                }

                var tempFileInfo = new FileInfo(tempFileName);

                var fileLength = tempFileInfo.Length;

                FromFile = true;
                FileName = tempFileInfo.FullName;

                if (fileLength < 0x10000) {
                    var isText = ArrayTextUtilities.IsText(tempFileName, 0x10000);

                    if (isText) {
                        FromFile = false;
                        ContentBody = await File.ReadAllTextAsync(tempFileName);
                    }
                }
            }
        }

        public void Alter(Exchange exchange)
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

            var request = headerSet!.ToRequest(body);

            exchange.Request.Header = request.Header;
            exchange.Request.Body = request.Body; // Header must be changed when we alter body 


            Done = true;
        }
    }
}
