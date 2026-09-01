using FitRos.Application.Features.Auth.ForgotPassword;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Auth.Login
{
    public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordValidator()
        {
            RuleFor(x => x.Email)
      .Cascade(CascadeMode.Stop)
      .NotEmpty()
      .EmailAddress()
      .MaximumLength(256);
        }
    }
    
}
