using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Common
{
     

    public interface IAuditableEntity
    {
        Guid CreatedBy { get; }
        DateTime CreatedAt { get; }

        Guid? ModifiedBy { get; }
        DateTime? ModifiedAt { get; }

        void SetModified(Guid userId);
    }
}
