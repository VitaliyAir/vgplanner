using MediatR;
using Microsoft.AspNetCore.Mvc;
using VGoalsPlanner.API.DTOs;
using VGoalsPlanner.Application.VTaskLogs;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VTaskLogsController : ControllerBase
    {
        private readonly ISender _sender;

        public VTaskLogsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Полная хронология записей лога по задаче.</summary>
        [HttpGet]
        public async Task<IActionResult> GetTaskHistory([FromQuery] Guid taskId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTaskHistoryQuery(taskId), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Записи лога за день, сгруппированные по задачам.</summary>
        [HttpGet("daily-report")]
        public async Task<IActionResult> GetDailyReport([FromQuery] DateOnly date, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetDailyReportQuery(date), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Статистика по времени за период (day/week/month задаёт клиент через from/to).</summary>
        [HttpGet("time-report")]
        public async Task<IActionResult> GetTimeReport(
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTimeReportQuery(from, to), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Агрегированный прогресс по цели (средний % выполнения задач).</summary>
        [HttpGet("goal-progress")]
        public async Task<IActionResult> GetGoalProgress([FromQuery] Guid goalId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetGoalProgressQuery(goalId), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Лента записей лога за фиксированное окно в Days дней, заканчивая endDate.</summary>
        [HttpGet("timeline")]
        public async Task<IActionResult> GetTimeline([FromQuery] DateOnly endDate, [FromQuery] int days = 7, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetLogTimelineQuery(endDate, days), cancellationToken);

            return ToResponse(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddTaskLogRequest request, CancellationToken cancellationToken)
        {
            var command = new AddTaskLogCommand(
                request.TaskId,
                request.Date,
                request.Note,
                request.Attachments ?? Array.Empty<string>(),
                request.TimeSpentMinutes,
                request.OldStatus,
                request.NewStatus);

            var result = await _sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return Failure(result.Error);
            }

            return CreatedAtAction(nameof(GetTaskHistory), new { taskId = request.TaskId }, new { id = result.Value });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskLogRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateTaskLogCommand(id, request.Note, request.Attachments ?? Array.Empty<string>());
            var result = await _sender.Send(command, cancellationToken);

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
