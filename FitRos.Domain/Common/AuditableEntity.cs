using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Common;

public abstract class AuditableEntity : IAuditableEntity
{
    public Guid CreatedBy { get; protected set; }
    public DateTime CreatedAt { get; protected set; }

    public Guid? ModifiedBy { get; protected set; }
    public DateTime? ModifiedAt { get; protected set; }

    public void SetModified(Guid userId)
    {
        ModifiedBy = userId;
        ModifiedAt = DateTime.UtcNow;
    }
}