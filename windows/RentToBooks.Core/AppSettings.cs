namespace RentToBooks.Core;

public sealed record AppSettings(
    string LastInputDirectory,
    string LastOutputDirectory,
    ProcessType ProcessType,
    string ReceivableAccount,
    string DepositAccount,
    string IncomeAccount);
