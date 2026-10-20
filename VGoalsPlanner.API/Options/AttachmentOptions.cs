namespace VGoalsPlanner.API.Options
{
    public sealed class AttachmentOptions
    {
        public const string SectionName = "Attachments";

        /// <summary>Максимальный размер одного вложения (по умолчанию 50 МБ).</summary>
        public long MaxFileSizeBytes { get; set; } = 50L * 1024 * 1024;
    }
}
