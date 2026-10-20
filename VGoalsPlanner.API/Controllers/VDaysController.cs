using MediatR;
using Microsoft.AspNetCore.Mvc;
using VGoalsPlanner.API.DTOs;
using VGoalsPlanner.Application.VDays;
using VGoalsPlanner.Domain.Abstractions;

namespace VGoalsPlanner.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VDaysController : ControllerBase
    {
        private readonly ISender _sender;

        public VDaysController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Гарантирует существование дня и возвращает его.</summary>
        [HttpPost]
        public async Task<IActionResult> GetOrCreate([FromBody] CreateDayRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetOrCreateDayCommand(request.Date), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Расписание на 7 дней подряд (по умолчанию — начиная с сегодня).</summary>
        [HttpGet("week")]
        public async Task<IActionResult> GetWeek([FromQuery] DateOnly? startDate, CancellationToken cancellationToken)
        {
            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _sender.Send(new GetWeekScheduleQuery(start), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Расписание на все дни календарного месяца (по умолчанию — текущий).</summary>
        [HttpGet("month")]
        public async Task<IActionResult> GetMonth(
            [FromQuery] int? year,
            [FromQuery] int? month,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var result = await _sender.Send(
                new GetMonthScheduleQuery(year ?? now.Year, month ?? now.Month),
                cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Задачи, не назначенные ни на один день.</summary>
        [HttpGet("unscheduled")]
        public async Task<IActionResult> GetUnscheduled(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUnscheduledTasksQuery(), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>День и его задачи (отсортированные по порядку в дне).</summary>
        [HttpGet("{date}")]
        public async Task<IActionResult> GetDay(DateOnly date, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetDayScheduleQuery(date), cancellationToken);

            return ToResponse(result);
        }

        [HttpPut("{date}/note")]
        public async Task<IActionResult> UpdateNote(DateOnly date, [FromBody] UpdateDayNoteRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new UpdateDayNoteCommand(date, request.SpecialNote), cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        [HttpPost("{date}/tasks")]
        public async Task<IActionResult> ScheduleTask(DateOnly date, [FromBody] ScheduleTaskRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new ScheduleTaskCommand(date, request.TaskId, request.OrderIndex),
                cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        [HttpDelete("{date}/tasks/{taskId:guid}")]
        public async Task<IActionResult> UnscheduleTask(DateOnly date, Guid taskId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new UnscheduleTaskCommand(date, taskId), cancellationToken);

            return MapFailure(result) ?? NoContent();
        }

        /// <summary>Переключить флаг "активна" у задачи именно в этот день.</summary>
        [HttpPatch("{date}/tasks/{taskId:guid}/active")]
        public async Task<IActionResult> ToggleTaskActive(DateOnly date, Guid taskId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ToggleTaskActiveCommand(date, taskId), cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        /// <summary>Переупорядочивание задач в рамках одного дня (Drag-n-Drop).</summary>
        [HttpPatch("{date}/tasks/reorder")]
        public async Task<IActionResult> ReorderTasks(DateOnly date, [FromBody] ReorderDayTasksRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new ReorderDayTasksCommand(date, request.OrderedTaskIds),
                cancellationToken);

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
