using FluentValidation;
using YekAbr.Services.DTOs.Cloud;

namespace YekAbr.Services.Validators.Cloud;

public sealed class CreateCloudTransferJobRequestValidator : AbstractValidator<CreateCloudTransferJobRequest>
{
    public CreateCloudTransferJobRequestValidator()
    {
        RuleFor(x => x.SourceConnectedAccountId)
            .NotEmpty().WithMessage("شناسه حساب مبدأ الزامی است.");

        RuleFor(x => x.DestinationConnectedAccountId)
            .NotEmpty().WithMessage("شناسه حساب مقصد الزامی است.");

        RuleFor(x => x.SourceItemId)
            .NotEmpty().WithMessage("شناسه آیتم مبدأ الزامی است.");
    }
}
