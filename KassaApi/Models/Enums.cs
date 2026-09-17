namespace KassaApi.Models;

public enum Currency
{
    USD,
    RUB
}

public enum TransactionType
{
    In,
    Out
}

public enum LoanStatus
{
    Open,
    PartiallyPaid,
    Closed
}

public enum CashSource
{
    Loan,
    LoanPayment,
    MyDebt,
    MyDebtPayment,
    Exchange,
    Expense
}
