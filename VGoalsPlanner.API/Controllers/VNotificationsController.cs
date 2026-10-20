using MediatR;
using Microsoft.AspNetCore.Mvc;
using VGoalsPlanner.API.DTOs;
using VGoalsPlanner.Application.VNotifications;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VNotificationsController : ControllerBase
    {
        private readonly ISender _sender;

        public VNotificationsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Заметки конкретного дня.</summary>
        [HttpGet]
        public async Task<IActionResult> GetByDay([FromQuery] DateOnly date, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetDayNotificationsQuery(date), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Все непрочитанные заметки пользователя.</summary>
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUnreadNotificationsQuery(), cancellationToken);

            return ToResponse(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateNotificationCommand(
                request.Date,
                request.Title,
                request.Description,
                request.Deadline);

            var result = await _sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return Failure(result.Error);
            }

            return CreatedAtAction(nameof(GetByDay), new { date = request.Date }, new { id = result.Value });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNotificationRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateNotificationCommand(id, request.Title, request.Description, request.Deadline);
            var result = await _sender.Send(command, cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DeleteNotificationCommand(id), cancellationToken);

            return MapFailure(result) ?? NoContent();
        }

        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        [HttpPatch("{id:guid}/completed")]
        public async Task<IActionResult> MarkAsCompleted(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new MarkNotificationAsCompletedCommand(id), cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        private IActionResult ToResponse<T>(Result<T> result)
            => result.IsSuccess ? Ok(result.Value) : Failure(result.Error);

        private IActionResult? MapFailure(Result result)
            => result.IsSuccess ? null : Failure(result.Error);

        private IActionResult Failure(Error error)
            => error.Code.Contains("NotFound") ? NotFound(error) : BadRequest(error);
    }
}
