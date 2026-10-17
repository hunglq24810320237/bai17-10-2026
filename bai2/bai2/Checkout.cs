namespace Bai2;

/// <summary>Một phiếu mượn thiết bị.</summary>
public class Checkout
{
    public string Id { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Equipment { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }

    /// <summary>Tình trạng tự tính: đã trả / quá hạn (chưa trả và hạn trả trước hôm nay) / đang mượn.</summary>
    public string Status
    {
        get
        {
            if (ReturnDate != null) return "Đã trả";
            return DueDate.Date < DateTime.Today ? "Quá hạn" : "Đang mượn";
        }
    }

    // Cột hiển thị ngày trả: để trống khi chưa trả.
    public string ReturnDateText => ReturnDate?.ToString("dd/MM/yyyy") ?? "";
}
