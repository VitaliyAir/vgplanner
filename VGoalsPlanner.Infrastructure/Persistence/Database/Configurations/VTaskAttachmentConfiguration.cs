using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VGoalsPlanner.Domain.Entities;

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Configurations
{
    internal class VTaskAttachmentConfiguration : IEntityTypeConfiguration<VTaskAttachment>
    {
        public void Configure(EntityTypeBuilder<VTaskAttachment> builder)
        {
            builder.ToTable("VTaskAttachments");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(e => e.ContentType)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.SizeBytes)
                .IsRequired();

            builder.Property(e => e.StorageKey)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.StorageObjectId)
                .IsRequired();

            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.UpdatedAt).IsRequired();

            builder.HasIndex(e => e.VTaskId)
                .HasDatabaseName("IX_VTaskAttachments_VTaskId");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_VTaskAttachments_UserId");

            // Удаление задачи из БД удаляет и строки вложений (байты в хранилище при этом не трогаются).
            builder.HasOne(e => e.VTask)
                .WithMany(t => t.Attachments)
                .HasForeignKey(e => e.VTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<VUser>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
