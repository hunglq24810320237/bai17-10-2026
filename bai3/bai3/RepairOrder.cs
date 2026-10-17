namespace Bai3;

/// <summary>Một đơn đăng ký sửa chữa.</summary>
public class RepairOrder
{
    public string Id { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Phone { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime ReceivedDate { get; set; }
    public decimal Cost { get; set; }
    public string Status { get; set; } = "Mới tiếp nhận";
    public string ImagePath { get; set; } = "";
}
