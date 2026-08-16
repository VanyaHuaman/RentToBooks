using RentToBooks.App.Resources;
using RentToBooks.Core;

namespace RentToBooks.App;

/// <summary>
/// Pairs a <see cref="ProcessType"/> with its localized display text, so the combo box
/// can show translated labels without needing to parse the enum back out of UI text.
/// </summary>
public sealed record ProcessTypeOption(ProcessType Value, string DisplayText)
{
    public static IReadOnlyList<ProcessTypeOption> All { get; } =
    [
        new(ProcessType.Payment, AppStrings.ProcessTypePayment),
        new(ProcessType.Invoice, AppStrings.ProcessTypeInvoice),
        new(ProcessType.Both, AppStrings.ProcessTypeBoth),
    ];

    public override string ToString() => DisplayText;
}
