using FluentValidation;
using VGoalsPlanner.Domain.Entities;
using VGoalsPlanner.Domain.Enums;

namespace VGoalsPlanner.Application.VTasks.Common
{
    internal sealed class TaskLinkDtoValidator : AbstractValidator<TaskLinkDto>
    {
        public TaskLinkDtoValidator()
        {
            RuleFor(x => x.Kind).IsInEnum();
            RuleFor(x => x.Target).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Alias).MaximumLength(100);
            RuleFor(x => x.Target)
                .Must(t => t.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                           || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                .When(x => x.Kind == TaskLinkKind.Url && !string.IsNullOrEmpty(x.Target))
                .WithMessage("Веб-адрес должен начинаться с http:// или https://");
        }
    }

    internal static class TaskLinks
    {
        public const int MaxCount = 30;

        /// <summary>Обрезает пробелы, пустой алиас делает null, пустые адреса убирает.</summary>
        public static List<TaskLink> Normalize(IEnumerable<TaskLinkDto>? links) =>
            (links ?? Enumerable.Empty<TaskLinkDto>())
                .Select(l => new TaskLink
                {
                    Kind = l.Kind,
                    Target = l.Target.Trim(),
                    Alias = string.IsNullOrWhiteSpace(l.Alias) ? null : l.Alias.Trim()
                })
                .Where(l => l.Target.Length > 0)
                .ToList();
    }
}
