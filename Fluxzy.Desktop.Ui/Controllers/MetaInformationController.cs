﻿using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Formatters;
using Fluxzy.Misc;
using Fluxzy.Writers;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/meta-info")]
    [ApiController]
    public class MetaInformationController : ControllerBase
    {
        private readonly IArchiveReaderProvider _archiveReaderProvider;
        private readonly IObservable<RealtimeArchiveWriter?> _archiveWriterObservable;
        private readonly FileContentUpdateManager _fileContentUpdateManager;

        public record TagUpdateModel(string Name);

        public record TagGlobalApplyModel(int[] ExchangeIds, Guid[] TagIdentifiers);


        public record CommentUpdateModel(string Comment, int[] ExchangeIds);

        public MetaInformationController(IArchiveReaderProvider archiveReaderProvider,
            IObservable<RealtimeArchiveWriter?> archiveWriterObservable,
            FileContentUpdateManager fileContentUpdateManager)
        {
            _archiveReaderProvider = archiveReaderProvider;
            _archiveWriterObservable = archiveWriterObservable;
            _fileContentUpdateManager = fileContentUpdateManager;
        }

        [HttpGet]
        public async Task<ActionResult<ArchiveMetaInformation>> Get()
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            var metaInfo = archiveReader.ReadMetaInformation();

            return metaInfo;
        }

        [HttpPost("tag")]
        public async Task<ActionResult<Tag>> CreateTag(TagUpdateModel model,
            [FromServices] IObservable<FileState> filestate)
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            var archiveWriter = (await _archiveWriterObservable.FirstAsync())!;

            var metaInformation = archiveReader.ReadMetaInformation();
            var tag = new Tag(Guid.NewGuid(), model.Name);

            metaInformation.Tags = metaInformation.Tags;
            metaInformation.Tags.Add(tag);

            archiveWriter.UpdateTags(metaInformation.Tags);

            (await filestate.FirstAsync()).Owner.SetUnsaved(true);

            return tag;
        }

        [HttpPatch("tag/{tagIdentifier}")]
        public async Task<ActionResult<bool>> UpdateTag(Guid tagIdentifier, TagUpdateModel model,
            [FromServices] IObservable<FileState> filestate)
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            var archiveWriter = (await _archiveWriterObservable.FirstAsync())!;

            var metaInformation = archiveReader.ReadMetaInformation();

            var tag = metaInformation.Tags.First(t => t.Identifier == tagIdentifier);
            metaInformation.Tags.Remove(tag);

            metaInformation.Tags.Add(new Tag(tag.Identifier, model.Name));

            archiveWriter.UpdateTags(metaInformation.Tags);

            (await filestate.FirstAsync()).Owner.SetUnsaved(true);

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
                archiveWriter.Update(exchange, CancellationToken.None);

                _fileContentUpdateManager.AddOrUpdate(exchange, archiveReader);
            }

            return true;
        }

        [HttpPost("tag/apply")]
        public async Task<ActionResult<bool>> GlobalApplyTag(TagGlobalApplyModel model)
        {
            var archiveReader = (await _archiveReaderProvider.Get())!;
            var archiveWriter = (await _archiveWriterObservable.FirstAsync())!;
            var metaInformation = archiveReader.ReadMetaInformation();

            var tags = metaInformation.Tags
                                      .Where(t => model.TagIdentifiers.Any(r => r == t.Identifier))
                                      .ToList();

            foreach (var exchange in model.ExchangeIds.Distinct()
                                          .Select(i => archiveReader.ReadExchange(i))
                                          .Where(t => t != null)) {
                exchange!.Tags.Clear();
                exchange.Tags.AddRange(tags);

                archiveWriter.Update(exchange, CancellationToken.None);

                _fileContentUpdateManager.AddOrUpdate(exchange, archiveReader);
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
                archiveWriter.Update(exchange, CancellationToken.None);

                _fileContentUpdateManager.AddOrUpdate(exchange, archiveReader);
            }

            return true;
        }

    }
}