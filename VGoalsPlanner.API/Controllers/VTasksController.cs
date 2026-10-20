using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VGoalsPlanner.API.DTOs;
using VGoalsPlanner.API.Options;
using VGoalsPlanner.Application.VTasks;
using VGoalsPlanner.Application.VTasks.Common;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;
using VGoalsPlanner.Domain.Errors;

namespace VGoalsPlanner.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VTasksController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly IOptions<AttachmentOptions> _attachmentOptions;

        public VTasksController(ISender sender, IOptions<AttachmentOptions> attachmentOptions)
        {
            _sender = sender;
            _attachmentOptions = attachmentOptions;
        }

        /// <summary>Задачи конкретной цели (постранично, с фильтром по статусу).</summary>
        [HttpGet]
        public async Task<IActionResult> GetByGoal(
            [FromQuery] Guid goalId,
            [FromQuery] VTaskStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetTasksByGoalQuery(goalId, status, page, pageSize),
                cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Глобальный поиск по названию, описанию и тегам.</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new SearchTasksQuery(query, page, pageSize), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Полный список задач пользователя с независимыми фильтрами, без пагинации.</summary>
        [HttpGet("filter")]
        public async Task<IActionResult> Filter(
            [FromQuery] string? title,
            [FromQuery] string? tag,
            [FromQuery] VTaskStatus? status,
            [FromQuery] Priority? priority,
            [FromQuery] bool regular = false,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new FilterTasksQuery(title, tag, status, priority, regular), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Задачи, отмеченные активными сегодня.</summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetActiveTasksQuery(), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>Полная информация о задаче: поля, логи выполнения и расписание.</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTaskByIdQuery(id), cancellationToken);

            return ToResponse(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateTaskCommand(
                request.GoalId,
                request.Title,
                request.Description,
                request.Deadline,
                request.Priority,
                request.Type,
                request.EstimatedMinutes,
                request.Tags ?? Array.Empty<string>(),
                request.RecurrenceDays,
                request.Links ?? Array.Empty<TaskLinkDto>());

            var result = await _sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return Failure(result.Error);
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateTaskCommand(
                id,
                request.Title,
                request.Description,
                request.Deadline,
                request.Priority,
                request.Type,
                request.EstimatedMinutes,
                request.Tags ?? Array.Empty<string>(),
                request.RecurrenceDays,
                request.Links);

            var result = await _sender.Send(command, cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DeleteTaskCommand(id), cancellationToken);

            return MapFailure(result) ?? NoContent();
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ChangeTaskStatusCommand(id, request.NewStatus), cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        [HttpPatch("{id:guid}/move")]
        public async Task<IActionResult> Move(Guid id, [FromBody] MoveTaskRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new MoveTaskCommand(id, request.NewGoalId), cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        /// <summary>Переупорядочивание задач внутри одной цели (Drag-n-Drop).</summary>
        [HttpPatch("reorder")]
        public async Task<IActionResult> Reorder([FromBody] ReorderTasksRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new ReorderTasksInGoalCommand(request.GoalId, request.OrderedTaskIds),
                cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        /// <summary>
        /// Загрузка вложения: тело запроса — сам файл (Content-Type = тип файла), имя в query.
        /// Без multipart: поток уходит в хранилище как есть, ничего не буферизуется.
        /// </summary>
        [HttpPost("{id:guid}/attachments")]
        public async Task<IActionResult> UploadAttachment(Guid id, [FromQuery] string fileName, CancellationToken cancellationToken)
        {
            var maxBytes = _attachmentOptions.Value.MaxFileSizeBytes;

            if (Request.ContentLength is not { } length)
            {
                return Failure(VTaskErrors.AttachmentLengthRequired);
            }

            if (length > maxBytes)
            {
                return Failure(VTaskErrors.AttachmentTooLarge(maxBytes));
            }

            // Лимит Kestrel по умолчанию ~30 МБ — поднимаем его для этого запроса до разрешённого размера файла.
            var bodySizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodySizeFeature is { IsReadOnly: false })
            {
                bodySizeFeature.MaxRequestBodySize = maxBytes;
            }

            var contentType = string.IsNullOrWhiteSpace(Request.ContentType) ? "application/octet-stream" : Request.ContentType;
            var result = await _sender.Send(
                new AddTaskAttachmentCommand(id, fileName ?? string.Empty, contentType, Request.Body, length),
                cancellationToken);

            return ToResponse(result);
        }

        [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
        public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DownloadTaskAttachmentQuery(id, attachmentId), cancellationToken);
            if (result.IsFailure)
            {
                return Failure(result.Error);
            }

            // Только скачивание (attachment) и без угадывания типа — загруженный HTML/SVG не должен выполняться в браузере.
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
        }

        [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
        public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DeleteTaskAttachmentCommand(id, attachmentId), cancellationToken);

            return MapFailure(result) ?? NoContent();
        }

        private IActionResult ToResponse<T>(Result<T> result)
            => result.IsSuccess ? Ok(result.Value) : Failure(result.Error);

        private IActionResult? MapFailure(Result result)
            => result.IsSuccess ? null : Failure(result.Error);

        private IActionResult Failure(Error error) => error.Code switch
        {
            var code when code.Contains("NotFound") => NotFound(error),
            "VTask.AttachmentTooLarge" => StatusCode(StatusCodes.Status413PayloadTooLarge, error),
            "VTask.AttachmentLengthRequired" => StatusCode(StatusCodes.Status411LengthRequired, error),
            // 503, а не 403: UI показывает на 403 экран "нет доступа к приложению".
            "VTask.StorageUnavailable" or "VTask.StorageForbidden" => StatusCode(StatusCodes.Status503ServiceUnavailable, error),
            _ => BadRequest(error)
        };
    }
}
