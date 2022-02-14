using System.IO;
using System.Threading.Tasks;

namespace Echoes
{
    public static class HttpMessageArchiveExtensions
    {
        //public static Stream ReadBodyAsStream(this HttpMessage message)
        //{
        //    if (message.Body != null)
        //        return new MemoryStream(message.Body);

        //    if (message.NoBody)
        //        return new MemoryStream(new byte[0]);

        //    if (message.ArchiveReference == null )
        //        return new MemoryStream(message.Body ?? new byte[0]);

        //    return message.ArchiveReference.InternalReadContentBody(message);
        //}

        //public static async Task<byte[]> ReadBodyAsByteArray(this HttpMessage message)
        //{
        //    using (var stream = ReadBodyAsStream(message))
        //    {
        //        if (stream is MemoryStream memoryStream)
        //            return memoryStream.ToArray();

        //        using (var copyStream = new MemoryStream())
        //        {
        //            await stream.CopyToAsync(copyStream).ConfigureAwait(false);
        //            copyStream.Seek(0, SeekOrigin.Begin);

        //            return copyStream.ToArray();
        //        }
        //    }
        //}
    }
}