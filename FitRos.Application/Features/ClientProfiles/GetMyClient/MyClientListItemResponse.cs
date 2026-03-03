using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.ClientProfiles.GetMyClient
{
    public sealed record MyClientListItemResponse(
     Guid ClientProfileId,
     Guid ClientUserId,
     string Email,
     string FirstName,
     string LastName,
     DateTime ClientProfileCreatedAtUtc,
     DateTime? LastMeasureRecordedAtUtc,
     decimal? LastWeight
 );
}
