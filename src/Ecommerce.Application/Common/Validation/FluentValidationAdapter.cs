using System.Threading.Tasks;

namespace Ecommerce.Application.Common.Validation
{
    // Adapter to allow FluentValidation validators to be used via the project's IValidator<T> abstraction
    public class FluentValidationAdapter<T> : IValidator<T>
    {
        private readonly FluentValidation.IValidator<T> _inner;

        public FluentValidationAdapter(FluentValidation.IValidator<T> inner)
        {
            _inner = inner;
        }

        public async Task<ValidationResult> ValidateAsync(T instance)
        {
            var res = await _inner.ValidateAsync(instance);
            var vr = new ValidationResult();
            vr.IsValid = res.IsValid;
            foreach (var e in res.Errors)
            {
                vr.Errors.Add(e.ErrorMessage);
            }
            return vr;
        }
    }
}
