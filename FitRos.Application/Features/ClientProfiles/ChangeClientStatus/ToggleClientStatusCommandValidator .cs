using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.ClientProfiles.ChangeClientStatus
{
    public sealed class ToggleClientStatusCommandValidator
     : AbstractValidator<ToggleClientStatusCommand>
    {
        public ToggleClientStatusCommandValidator()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty();
        }
    }
}
