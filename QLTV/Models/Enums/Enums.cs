namespace LibraryManagement.Models.Enums;

public enum CopyStatus { Available = 0, OnLoan = 1, Lost = 2, Damaged = 3, Disposed = 4 }

public enum CopyCondition { New = 0, Good = 1, Worn = 2 }

public enum DepositStatus { Held = 0, Refunded = 1, Forfeited = 2 }

public enum MembershipStatus { Active = 0, Expired = 1, Cancelled = 2 }

public enum LoanItemStatus { Borrowed = 0, Returned = 1, Lost = 2 }

public enum PaymentType
{
    MembershipFee = 0,
    Deposit = 1,
    DepositRefund = 2,
    RentalFee = 3,
    LateFine = 4,
    Compensation = 5
}

public enum PaymentMethod { Cash = 0, Transfer = 1 }

// Paid = đã thu, Unpaid = khoản còn nợ, Voided = đã hủy (không tính vào doanh thu)
public enum PaymentStatus { Paid = 0, Unpaid = 1, Voided = 2 }
