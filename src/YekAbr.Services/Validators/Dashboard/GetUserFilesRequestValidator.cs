using FluentValidation;
using YekAbr.Services.DTOs.Dashboard;

namespace YekAbr.Services.Validators.Dashboard;

public sealed class GetUserFilesRequestValidator : AbstractValidator<GetUserFilesRequest>
{
    private static readonly HashSet<string> AllowedSortBy = new(StringComparer.OrdinalIgnoreCase)
    {
        "uploadedAt",
        "fileName",
        "size",
        "providerType"
    };

    private static readonly HashSet<string> AllowedSortDirection = new(StringComparer.OrdinalIgnoreCase)
    {
        "asc",
        "desc"
    };

    public GetUserFilesRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل ۱ باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("اندازه صفحه باید بین ۱ تا ۱۰۰ باشد.");

        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value) || AllowedSortBy.Contains(value.Trim()))
            .WithMessage("مقدار مرتب‌سازی معتبر نیست. مقادیر مجاز: uploadedAt، fileName، size، providerType.");

        RuleFor(x => x.SortDirection)
            .Must(value => string.IsNullOrWhiteSpace(value) || AllowedSortDirection.Contains(value.Trim()))
            .WithMessage("جهت مرتب‌سازی معتبر نیست. مقادیر مجاز: asc، desc.");
    }
}
