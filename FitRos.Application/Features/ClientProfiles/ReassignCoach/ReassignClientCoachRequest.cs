using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.ClientProfiles.ReassignCoach
{
    public sealed class ReassignClientCoachRequest
    {
        public Guid NewCoachId { get; set; }
    }
}
