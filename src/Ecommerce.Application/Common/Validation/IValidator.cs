using System.Threading.Tasks;

namespace Ecommerce.Application.Common.Validation
{
    public interface IValidator<T>
    {
        Task<ValidationResult> ValidateAsync(T instance);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public System.Collections.Generic.List<string> Errors { get; set; } = new System.Collections.Generic.List<string>();
    }
}
