using FluentValidation;
using YekAbr.Services.DTOs.Transfers;

namespace YekAbr.Services.Validators.Transfers;

public sealed class StartProviderSyncRequestValidator : AbstractValidator<StartProviderSyncRequest>
{
    public StartProviderSyncRequestValidator()
    {
        RuleFor(x => x.SourceProvider)
            .IsInEnum().WithMessage("ارائه‌دهنده مبدأ نامعتبر است.");

        RuleFor(x => x.DestinationProvider)
            .IsInEnum().WithMessage("ارائه‌دهنده مقصد نامعتبر است.")
            .Must((request, destination) => request.SourceProvider != destination)
            .WithMessage("ارائه‌دهنده مبدأ و مقصد نباید یکسان باشند.");
    }
}
