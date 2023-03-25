// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Filters;
using Fluxzy.Rules.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilterController : ControllerBase
    {
        [HttpGet("description/{typeKind}")]
        public ActionResult<string> GetFilterDescription(
            string typeKind, [FromServices] FilterTemplateManager templateManager)
        {
            if (templateManager.TryGetDescription(typeKind, out var longDescription))
                return longDescription;

            return NotFound();
        }

        [HttpPost("validate")]
        public ActionResult<Filter> Validate(Filter filter)
        {
            return filter;
        }

        [HttpGet("templates")]
        public ActionResult<List<FilterTemplate>> GetTemplates([FromServices] FilterTemplateManager templateManager)
        {
            return templateManager.ReadAvailableTemplates();
        }

        [HttpGet("templates/any")]
        public ActionResult<AnyFilter> GetTemplates()
        {
            return AnyFilter.Default;
        }

        [HttpPost("apply/regular")]
        public ActionResult<bool> ApplyToView(
            Filter filter,
            [FromServices]
            ActiveViewFilterManager activeViewFilterManager,
            [FromServices]
            TemplateToolBarFilterProvider filterProvider)
        {
            activeViewFilterManager.UpdateViewFilter(filter);
            filterProvider.SetNewFilter(filter);

            return true;
        }


        /// <summary>
        /// Appending a view filter view and rule
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="activeViewFilterManager"></param>
        /// <param name="filterProvider"></param>
        /// <returns></returns>
        [HttpPost("apply/regular/and")]
        public ActionResult<bool> ApplyToViewAddAnd(
            Filter filter,
            [FromServices]
            ActiveViewFilterManager activeViewFilterManager,
            [FromServices]
            TemplateToolBarFilterProvider filterProvider)
        {
            var currentFilter = activeViewFilterManager.Current;

            if (currentFilter.Filter is AnyFilter) {

                activeViewFilterManager.UpdateViewFilter(filter);
                filterProvider.SetNewFilter(filter);

                return true; 
            }


            var filterCollection = new FilterCollection(currentFilter.Filter, filter) {
                Operation = SelectorCollectionOperation.And
            }; 

            activeViewFilterManager.UpdateViewFilter(filterCollection);
            filterProvider.SetNewFilter(filterCollection);

            return true;
        }
        /// <summary>
        /// Appending a view filter view and rule
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="activeViewFilterManager"></param>
        /// <param name="filterProvider"></param>
        /// <returns></returns>
        [HttpPost("apply/regular/or")]
        public ActionResult<bool> ApplyToViewAddOr(
            Filter filter,
            [FromServices]
            ActiveViewFilterManager activeViewFilterManager,
            [FromServices]
            TemplateToolBarFilterProvider filterProvider)
        {
            var currentFilter = activeViewFilterManager.Current;

            if (currentFilter.Filter is AnyFilter)
            {

                activeViewFilterManager.UpdateViewFilter(filter);
                filterProvider.SetNewFilter(filter);

                return true;
            }

            var filterCollection = new FilterCollection(currentFilter.Filter, filter) {
                Operation = SelectorCollectionOperation.Or
            }; 

            activeViewFilterManager.UpdateViewFilter(filterCollection);
            filterProvider.SetNewFilter(filterCollection);

            return true;
        }

        [HttpPost("apply/source")]
        public ActionResult<bool> ApplySourceFilterToView(
            Filter filter,
            [FromServices]
            ActiveViewFilterManager activeViewFilterManager,
            [FromServices]
            TemplateToolBarFilterProvider filterProvider)
        {
            activeViewFilterManager.UpdateSourceFilter(filter);

            // filterProvider.SetNewFilter(filter);

            return true;
        }

        [HttpDelete("apply/source")]
        public ActionResult<bool> ApplyResetSourceFilterToView(
            [FromServices] ActiveViewFilterManager activeViewFilterManager,
            [FromServices]
            TemplateToolBarFilterProvider filterProvider)
        {
            activeViewFilterManager.UpdateSourceFilter(AnyFilter.Default);

            return true;
        }
    }
}
