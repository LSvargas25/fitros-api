using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Common
{
    public interface ITenantEntity
    {
        Guid? GymId { get; }
    }
    
}
