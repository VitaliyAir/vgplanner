using MediatR;
using Microsoft.AspNetCore.Mvc;
using VGoalsPlanner.API.DTOs;
using VGoalsPlanner.Application.VGoals;
using VGoalsPlanner.Domain.Abstractions;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VGoalsController : ControllerBase
    {
        private readonly ISender _sender;

        public VGoalsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] VGoalStatus? status,
            [FromQuery] Priority? priority,
            [FromQuery] Guid? parentGoalId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetGoalsListQuery(status, priority, parentGoalId, search, page, pageSize),
                cancellationToken);

            return ToResponse(result);
        }

        /// <summary>
        /// Верхний уровень дерева — цели без родителя. Стартовая точка для ленивой загрузки.
        /// </summary>
        [HttpGet("roots")]
        public async Task<IActionResult> GetRoots(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetRootGoalsQuery(), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>
        /// Возвращает цель и один уровень её дочерних элементов (цели + задачи).
        /// Дерево на UI разворачивается лениво: клик по узлу — ещё один запрос по его Id.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetGoalByIdQuery(id), cancellationToken);

            return ToResponse(result);
        }

        /// <summary>
        /// Хлебные крошки цели: заголовки от корня дерева до самой цели включительно.
        /// </summary>
        [HttpGet("{id:guid}/breadcrumb")]
        public async Task<IActionResult> GetBreadcrumb(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetGoalBreadcrumbQuery(id), cancellationToken);

            return ToResponse(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGoalRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateGoalCommand(request.Title, request.Description, request.Priority, request.ParentGoalId);
            var result = await _sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGoalRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateGoalCommand(id, request.Title, request.Description, request.Priority);
            var result = await _sender.Send(command, cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DeleteGoalCommand(id), cancellationToken);

            return MapFailure(result) ?? NoContent();
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeGoalStatusRequest request, CancellationToken cancellationToken)
        {
            var command = new ChangeGoalStatusCommand(id, request.NewStatus);
            var result = await _sender.Send(command, cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        [HttpPatch("{id:guid}/move")]
        public async Task<IActionResult> Move(Guid id, [FromBody] MoveGoalRequest request, CancellationToken cancellationToken)
        {
            var command = new MoveGoalCommand(id, request.NewParentGoalId);
            var result = await _sender.Send(command, cancellationToken);

            return MapFailure(result) ?? Ok();
        }

        /// <summary>Переупорядочивание целей внутри одного родителя (null — корневые цели).</summary>
        [HttpPatch("reorder")]
        public async Task<IActionResult> Reorder([FromBody] ReorderGoalsRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new ReorderGoalsCommand(request.ParentGoalId, request.OrderedGoalIds),
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
