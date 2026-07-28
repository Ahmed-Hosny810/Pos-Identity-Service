using Pos.Identity.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Identity.Infrastructure.Persistence.Context.DbConfigurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FullName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.TenantId)
               .IsRequired(false);

            builder.Property(x => x.UserType)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.HasIndex(x => x.TenantId);

            builder.HasIndex(x => x.UserType);

            builder.HasIndex(x => new { x.TenantId, x.Email });
        }
    }
}
