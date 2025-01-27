using ServiceStack.FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Models
{
    public class EmployeValidations : AbstractValidator<Employee>
    {
        public EmployeValidations()
        {
            RuleFor(x => x.Name).NotEmpty().MinimumLength(5).MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Contact).NotEmpty().MinimumLength(10).MaximumLength(15); 
        }
    }
}
