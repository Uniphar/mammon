using FluentValidation;

namespace Mammon.Models.Actors
{
    public sealed class CostReportRequest
    {
        public string SubscriptionName { get; set; } = string.Empty;
        public DateTime costFrom { get; set; }
        public DateTime costTo { get; set; }
    }

    public class CostReportRequestValidator : AbstractValidator<CostReportRequest>
    {
        public CostReportRequestValidator()
        {
            RuleFor(x => x.SubscriptionName).NotEmpty();
            RuleFor(x => x.costFrom).NotEmpty();
            RuleFor(x => x.costFrom).NotEmpty();
            RuleFor(x => x.costTo).GreaterThan(x => x.costFrom);
        }
    }
}
