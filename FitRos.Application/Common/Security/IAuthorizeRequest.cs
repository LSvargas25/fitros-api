using FitRos.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Common.Security
{
    public interface  IAuthorizeRequest
    {
        UserRole[] AllowedRoles { get; }
    }
}
