using MediatR;
using Microsoft.AspNetCore.Mvc;
using VGoalsPlanner.Application.Statistics;

namespace VGoalsPlanner.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ISender _sender;

        public StatisticsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Количество целей и задач: всего и по статусам.</summary>
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetStatisticsQuery(), cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }
    }
}
