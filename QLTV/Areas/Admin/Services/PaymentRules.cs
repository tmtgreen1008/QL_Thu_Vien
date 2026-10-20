using LibraryManagement.Models.Enums;

namespace LibraryManagement.Areas.Admin.Services;

public static class PaymentRules
{
    // Doanh thu thật: không gồm tiền cọc (cọc là tiền giữ hộ, sẽ hoàn lại)
    public static readonly PaymentType[] RevenueTypes =
    {
        PaymentType.MembershipFee, PaymentType.RentalFee, PaymentType.LateFine, PaymentType.Compensation
    };

    public static readonly PaymentType[] DepositTypes = { PaymentType.Deposit, PaymentType.DepositRefund };

    // Phí thẻ, cọc, hoàn cọc gắn với thẻ nên không hủy trực tiếp (dùng "Hoàn cọc, hủy thẻ")
    public static bool CanVoid(PaymentType t) =>
        t is PaymentType.RentalFee or PaymentType.LateFine or PaymentType.Compensation;
}
