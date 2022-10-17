using Fluxzy.Formatters;
using Fluxzy.Writers;
using Microsoft.AspNetCore.Mvc;
using System.Reactive.Linq;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/meta-info")]
    [ApiController]
    public class MetaInformationController : ControllerBase
    {
        private readonly IArchiveReaderProvider _archiveReaderProvider;
        private readonly IObservable<RealtimeArchiveWriter?> _archiveWriterObservable;

        public record TagUpdateModel(string Name); 

        public record CommentUpdateModel(string Comment, int[] ExchangeIds); 

        public MetaInformationController(IArchiveReaderProvider archiveReaderProvider, 
            IObservable<RealtimeArchiveWriter?> archiveWriterObservable)
        {
            _archiveReaderProvider = archiveReaderProvider;
            _archiveWriterObservable = archiveWriterObservable;
        }

        [HttpGet()]
        public async Task<ActionResult<ArchiveMetaInformation>> Get()
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            return archiveReader.ReadMetaInformation(); 
        }

        [HttpPost("tag")]
        public async Task<ActionResult<bool>> CreateTag(TagUpdateModel model)
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            var archiveWriter = (await _archiveWriterObservable.FirstAsync())!; 

            var metaInformation =  archiveReader.ReadMetaInformation();
            var tag = new Tag(Guid.NewGuid(), model.Name);

            metaInformation.Tags = metaInformation.Tags;

            metaInformation.Tags.Add(tag);

            archiveWriter.UpdateTags(metaInformation.Tags);

            return Created("", tag.Identifier); 
        }

        [HttpPatch("tag/{tagIdentifier}")]
        public async Task<ActionResult<bool>> UpdateTag(Guid tagIdentifier, TagUpdateModel model)
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            var archiveWriter = (await _archiveWriterObservable.FirstAsync())!; 

            var metaInformation =  archiveReader.ReadMetaInformation();
            
            var tag = metaInformation.Tags.First(t => t.Identifier == tagIdentifier);
            metaInformation.Tags.Remove(tag);

            metaInformation.Tags.Add(new Tag(tag.Identifier, model.Name));

            archiveWriter.UpdateTags(metaInformation.Tags);

            return true; 
        }


        [HttpPost("tag/{tagIdentifier}")]
        public async Task<ActionResult<bool>> ApplyTag(Guid tagIdentifier, int[] exchangeIds)
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            var archiveWriter = (await _archiveWriterObservable.FirstAsync())!;
            var metaInformation = archiveReader.ReadMetaInformation();

            var tag = metaInformation.Tags.FirstOrDefault(t => t.Identifier == tagIdentifier);

            if (tag == null)
                return false; 

            foreach (var exchange in exchangeIds
                         .Select(i => archiveReader.ReadExchange(i)).Where(t => t != null)) {

                exchange!.Tags.Add(tag);
                await archiveWriter.Update(exchange, CancellationToken.None);
            }

            return true; 
        }

        [HttpPost("comment")]
        public async Task<ActionResult<bool>> Comment(CommentUpdateModel comment)
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            var archiveWriter = (await _archiveWriterObservable.FirstAsync())!;

            foreach (var exchange in comment.ExchangeIds.Select(s => archiveReader.ReadExchange(s))
                                            .Where(s => s != null)) {

                exchange!.Comment = comment.Comment; 
                await archiveWriter.Update(exchange, CancellationToken.None);
            }

            return true; 
        }
    }
}