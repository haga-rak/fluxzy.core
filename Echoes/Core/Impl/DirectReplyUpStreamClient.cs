using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Echoes.Core.Utils;
using Echoes.Rules;

namespace Echoes.Core
{
    //internal class DirectReplyUpStreamClient : IUpstreamClient
    //{
    //    private readonly ReplyContent _replyContent;

    //    public DirectReplyUpStreamClient(ReplyContent replyContent)
    //    {
    //        _replyContent = replyContent;
    //    }

    //    public Task Init()
    //    {
    //        return Task.CompletedTask;
    //    }

    //    public EndPointInformation EndPointInformation { get; } = new EndPointInformation("127.0.0.1", 0, "127.0.0.1", 0);

    //    public async Task<Hpm> ProduceResponse(Exchange exchange, params Stream [] outStreams)
    //    {
    //        var responseMessage =
    //            HttpMessageHeaderParser.BuildResponseMessage(requestMessage.Id, Encoding.UTF8.GetBytes(_replyContent.Header), false);
            
    //        await outStreams.WriteAsync(responseMessage.ForwardableHeader, 0, responseMessage.ForwardableHeader.Length).ConfigureAwait(false);

    //        using (var responseBody = new MemoryStream())
    //        {
    //            using (var contentStream = _replyContent.GetBody())
    //            {
    //                await contentStream.CopyTo(new List<Stream>(outStreams) { responseBody }.ToArray()).ConfigureAwait(false);
    //                var body = responseBody.ToArray();
    //                responseMessage.Body = body;
    //                responseMessage.OnWireContentLength = body.Length;
    //            }
    //        }

    //        return responseMessage;
    //    }

    //    public Task Release(bool shouldClose)
    //    {
    //        return Task.CompletedTask;
    //    }

    //    public IUpstreamConnection Detach()
    //    {
    //        throw new InvalidOperationException("ReplyContent cannot be detached");
    //    }
    //}
}