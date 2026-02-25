using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Abstractions.Messaging;

public sealed class FrontendSettings
{
    public string BaseUrl { get; init; } = default!;
}
