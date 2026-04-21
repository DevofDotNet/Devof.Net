using Blog.Domain.Common;
using Microsoft.Extensions.Options;

namespace Blog.Infrastructure.Validation;

public class EmailOptionsValidation : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("EmailOptions is null.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail("Brevo API key is required.");
        }

        return ValidateOptionsResult.Success;
    }
}