using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Bai3;

/// <summary>
/// Bài 3: Repair Service Tracker - quản lý đơn đăng ký dịch vụ sửa chữa.
/// </summary>
public class MainForm : Form
{
    private static readonly string[] DeviceTypes = { "Điện thoại", "Laptop", "Máy tính bảng", "Máy in", "Tivi", "Khác" };
    private static readonly string[] Statuses = { "Mới tiếp nhận", "Đang sửa", "Chờ linh kiện", "Hoàn tất", "Đã hủy" };
    private const string All = "Tất cả";

    private readonly BindingList<RepairOrder> all = new();
    private BindingList<RepairOrder> view = new();
    private readonly BindingSource bindingSource = new();
    private readonly ErrorProvider errorProvider = new();
    private RepairOrder? editing;
    private string imagePath = "";

    private readonly TextBox txtId = new(), txtCustomer = new(), txtPhone = new(), txtDescription = new(), txtCost = new(), txtSearch = new();
    private readonly ComboBox cboDevice = new(), cboStatus = new(), cboFilter = new();
    private readonly DateTimePicker dtpReceived = new();
    private readonly PictureBox picDevice = new();
    private readonly Button btnChooseImage = new(), btnAdd = new(), btnUpdate = new(), btnDelete = new(), btnClear = new();
    private readonly DataGridView dgv = new();
    private readonly ToolStripStatusLabel lblCount = new(), lblTotal = new(), lblMessage = new();

    public MainForm()
    {
        Text = "Repair Service Tracker - Quản lý đơn sửa chữa";
        Font = new Font("Segoe UI", 9.5F);
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(960, 640);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        SeedData();

        bindingSource.DataSource = view;
        dgv.DataSource = bindingSource;
        ApplyFilter();
        Shown += (_, _) => ClearForm();
    }

    // ============================ GIAO DIỆN ============================
    private void BuildUi()
    {
        errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

        var menu = new MenuStrip();
        var mnuFile = new ToolStripMenuItem("Tệp");
        var mnuExport = new ToolStripMenuItem("Xuất danh sách đang hiển thị ra CSV...");
        var mnuExit = new ToolStripMenuItem("Thoát");
        mnuExport.ShortcutKeys = Keys.Control | Keys.E;
        mnuExport.Click += (_, _) => ExportCsv();
        mnuExit.Click += (_, _) => Close();
        mnuFile.DropDownItems.AddRange(new ToolStripItem[] { mnuExport, new ToolStripSeparator(), mnuExit });
        menu.Items.Add(mnuFile);

        var status = new StatusStrip { Dock = DockStyle.Bottom, SizingGrip = false, CanOverflow = false };
        lblCount.BorderSides = ToolStripStatusLabelBorderSides.Right;
        lblTotal.BorderSides = ToolStripStatusLabelBorderSides.Right;
        lblMessage.Spring = true;
        lblMessage.TextAlign = ContentAlignment.MiddleLeft;
        lblMessage.Text = "Sẵn sàng";
        status.Items.AddRange(new ToolStripItem[] { lblCount, lblTotal, lblMessage });

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildTopArea(), 0, 0);
        root.Controls.Add(BuildFilterBar(), 0, 1);
        root.Controls.Add(BuildGrid(), 0, 2);

        Controls.Add(root);
        Controls.Add(status);
        Controls.Add(menu);
        MainMenuStrip = menu;
    }

    /// <summary>Khu vực trên: thông tin đơn (trái) và ảnh thiết bị (phải).</summary>
    private TableLayoutPanel BuildTopArea()
    {
        var top = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        top.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        top.Controls.Add(BuildInfoGroup(), 0, 0);
        top.Controls.Add(BuildImageGroup(), 1, 0);
        return top;
    }

    private GroupBox BuildInfoGroup()
    {
        var grp = new GroupBox { Text = "Thông tin đơn sửa chữa", Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, Padding = new Padding(6) };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        cboDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        cboDevice.Items.AddRange(DeviceTypes);
        cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboStatus.Items.AddRange(Statuses);
        dtpReceived.Format = DateTimePickerFormat.Custom;
        dtpReceived.CustomFormat = "dd/MM/yyyy";
        txtCost.TextAlign = HorizontalAlignment.Right;
        txtDescription.Multiline = true;
        txtDescription.Height = 52;
        txtDescription.ScrollBars = ScrollBars.Vertical;

        AddRow(tbl, 0, "Mã đơn:", txtId, "Tên khách hàng:", txtCustomer);
        AddRow(tbl, 1, "Số điện thoại:", txtPhone, "Loại thiết bị:", cboDevice);
        AddRow(tbl, 2, "Ngày tiếp nhận:", dtpReceived, "Trạng thái:", cboStatus);
        AddRow(tbl, 3, "Chi phí dự kiến (đ):", txtCost, "", new Panel { Height = 1 });

        tbl.Controls.Add(Lbl("Mô tả lỗi:"), 0, 4);
        tbl.Controls.Add(txtDescription, 1, 4);
        tbl.SetColumnSpan(txtDescription, 3);
        txtDescription.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtDescription.Margin = new Padding(3, 4, 26, 4);

        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        StyleButton(btnAdd, "Thêm", BtnAdd_Click);
        StyleButton(btnUpdate, "Cập nhật", BtnUpdate_Click);
        StyleButton(btnDelete, "Xóa", BtnDelete_Click);
        StyleButton(btnClear, "Làm mới", (_, _) => ClearForm());
        buttons.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });
        tbl.Controls.Add(buttons, 1, 5);
        tbl.SetColumnSpan(buttons, 3);

        foreach (var t in new[] { txtId, txtCustomer, txtPhone, txtCost, txtDescription })
            t.TextChanged += (s, _) => errorProvider.SetError((Control)s!, "");

        grp.Controls.Add(tbl);
        return grp;
    }

    private GroupBox BuildImageGroup()
    {
        var grp = new GroupBox { Text = "Ảnh thiết bị", Dock = DockStyle.Fill };
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(6) };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        picDevice.Dock = DockStyle.Fill;
        picDevice.SizeMode = PictureBoxSizeMode.Zoom;      // hiển thị đúng tỷ lệ, không bị méo
        picDevice.BorderStyle = BorderStyle.FixedSingle;
        picDevice.BackColor = Color.FromArgb(245, 246, 248);

        btnChooseImage.Text = "Chọn ảnh...";
        btnChooseImage.Dock = DockStyle.Fill;
        btnChooseImage.Height = 32;
        btnChooseImage.Click += BtnChooseImage_Click;

        tbl.Controls.Add(picDevice, 0, 0);
        tbl.Controls.Add(btnChooseImage, 0, 1);
        grp.Controls.Add(tbl);
        return grp;
    }

    private FlowLayoutPanel BuildFilterBar()
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 6) };

        txtSearch.Width = 300;
        txtSearch.PlaceholderText = "Gõ mã đơn, tên khách hàng hoặc số điện thoại...";
        txtSearch.TextChanged += (_, _) => ApplyFilter();

        cboFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        cboFilter.Items.Add(All);
        cboFilter.Items.AddRange(Statuses);
        cboFilter.SelectedIndex = 0;
        cboFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var l1 = Lbl("Tìm kiếm:");
        var l2 = Lbl("Lọc trạng thái:");
        l1.Margin = new Padding(3, 7, 3, 3);
        l2.Margin = new Padding(18, 7, 3, 3);
        bar.Controls.AddRange(new Control[] { l1, txtSearch, l2, cboFilter });
        return bar;
    }

    private DataGridView BuildGrid()
    {
        dgv.Dock = DockStyle.Fill;
        dgv.AutoGenerateColumns = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.ReadOnly = true;
        dgv.MultiSelect = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.RowHeadersVisible = false;
        dgv.BackgroundColor = Color.White;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgv.Columns.Add(Col(nameof(RepairOrder.Id), "Mã đơn", 8));
        dgv.Columns.Add(Col(nameof(RepairOrder.Customer), "Khách hàng", 14));
        dgv.Columns.Add(Col(nameof(RepairOrder.Phone), "Số điện thoại", 12));
        dgv.Columns.Add(Col(nameof(RepairOrder.DeviceType), "Loại thiết bị", 12));
        dgv.Columns.Add(Col(nameof(RepairOrder.Description), "Mô tả lỗi", 20));
        var colDate = Col(nameof(RepairOrder.ReceivedDate), "Ngày tiếp nhận", 14);
        colDate.DefaultCellStyle.Format = "dd/MM/yyyy";
        dgv.Columns.Add(colDate);
        var colCost = Col(nameof(RepairOrder.Cost), "Chi phí dự kiến", 13);
        colCost.DefaultCellStyle.Format = "N0";                       // định dạng chi phí "N0"
        colCost.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        dgv.Columns.Add(colCost);
        dgv.Columns.Add(Col(nameof(RepairOrder.Status), "Trạng thái", 12));

        dgv.SelectionChanged += (_, _) => LoadSelected();
        return dgv;
    }

    private static DataGridViewTextBoxColumn Col(string prop, string header, float weight) => new()
    {
        DataPropertyName = prop,
        HeaderText = header,
        Name = "col" + prop,
        FillWeight = weight
    };

    private static Label Lbl(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 6, 3, 6)
    };

    private void AddRow(TableLayoutPanel tbl, int row, string l1, Control c1, string l2, Control c2)
    {
        tbl.Controls.Add(Lbl(l1), 0, row);
        tbl.Controls.Add(c1, 1, row);
        tbl.Controls.Add(Lbl(l2), 2, row);
        tbl.Controls.Add(c2, 3, row);
        foreach (var c in new[] { c1, c2 })
        {
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(3, 4, 26, 4);
        }
    }

    private static void StyleButton(Button b, string text, EventHandler onClick)
    {
        b.Text = text;
        b.Size = new Size(96, 32);
        b.Click += onClick;
    }

    // ============================ DỮ LIỆU ============================
    private static string Img(string file) => Path.Combine(AppContext.BaseDirectory, "Images", file);

    private void SeedData()
    {
        DateTime t = DateTime.Today;
        all.Add(new RepairOrder { Id = "DH001", Customer = "Nguyễn Văn An", Phone = "0912345678", DeviceType = "Laptop", Description = "Không lên nguồn, nghi hỏng mainboard", ReceivedDate = t.AddDays(-5), Cost = 1800000, Status = "Đang sửa", ImagePath = Img("laptop.png") });
        all.Add(new RepairOrder { Id = "DH002", Customer = "Trần Thị Bích", Phone = "0987654321", DeviceType = "Điện thoại", Description = "Vỡ màn hình, cần thay mới", ReceivedDate = t.AddDays(-3), Cost = 950000, Status = "Chờ linh kiện", ImagePath = Img("phone.png") });
        all.Add(new RepairOrder { Id = "DH003", Customer = "Lê Quốc Cường", Phone = "0903111222", DeviceType = "Máy in", Description = "Kẹt giấy, không in được", ReceivedDate = t.AddDays(-8), Cost = 350000, Status = "Hoàn tất", ImagePath = Img("printer.png") });
        all.Add(new RepairOrder { Id = "DH004", Customer = "Phạm Thu Dung", Phone = "0977888999", DeviceType = "Máy tính bảng", Description = "Pin chai, sạc không vào", ReceivedDate = t.AddDays(-1), Cost = 600000, Status = "Mới tiếp nhận", ImagePath = Img("tablet.png") });
        all.Add(new RepairOrder { Id = "DH005", Customer = "Đỗ Minh Đức", Phone = "0935123456", DeviceType = "Laptop", Description = "Cài lại Windows, vệ sinh máy", ReceivedDate = t.AddDays(-2), Cost = 250000, Status = "Mới tiếp nhận" });
        all.Add(new RepairOrder { Id = "DH006", Customer = "Vũ Thị Hạnh", Phone = "0966333444", DeviceType = "Điện thoại", Description = "Loa ngoài bị rè", ReceivedDate = t.AddDays(-9), Cost = 400000, Status = "Đã hủy" });
    }

    private static bool IsUnfinished(RepairOrder o) => o.Status != "Hoàn tất" && o.Status != "Đã hủy";

    private void ApplyFilter(RepairOrder? keep = null)
    {
        string q = txtSearch.Text.Trim();
        string status = cboFilter.SelectedItem as string ?? All;

        var items = all.Where(o =>
            (q.Length == 0
                || o.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
                || o.Customer.Contains(q, StringComparison.CurrentCultureIgnoreCase)
                || o.Phone.Contains(q, StringComparison.OrdinalIgnoreCase))
            && (status == All || o.Status == status)).ToList();

        view = new BindingList<RepairOrder>(items);
        bindingSource.DataSource = view;
        UpdateStatusBar();

        if (keep != null) SelectRow(keep); else dgv.ClearSelection();
    }

    /// <summary>Số đơn theo bộ lọc và tổng chi phí dự kiến của các đơn chưa hoàn tất (không tính đơn đã hủy).</summary>
    private void UpdateStatusBar()
    {
        decimal total = all.Where(IsUnfinished).Sum(o => o.Cost);
        lblCount.Text = $"Số đơn hiển thị: {view.Count} / {all.Count}";
        lblTotal.Text = $"Tổng chi phí dự kiến (đơn chưa hoàn tất): {total:N0} đ";
    }

    private void SelectRow(RepairOrder item)
    {
        foreach (DataGridViewRow row in dgv.Rows)
        {
            if (!ReferenceEquals(row.DataBoundItem, item)) continue;
            dgv.CurrentCell = row.Cells[0];
            row.Selected = true;
            return;
        }
    }

    private void LoadSelected()
    {
        if (dgv.SelectedRows.Count == 0) return;
        if (dgv.SelectedRows[0].DataBoundItem is not RepairOrder o) return;
        editing = o;
        txtId.Text = o.Id;
        txtCustomer.Text = o.Customer;
        txtPhone.Text = o.Phone;
        cboDevice.SelectedItem = o.DeviceType;
        txtDescription.Text = o.Description;
        dtpReceived.Value = o.ReceivedDate;
        txtCost.Text = o.Cost.ToString("N0");
        cboStatus.SelectedItem = o.Status;
        ShowImage(o.ImagePath);
        errorProvider.Clear();
    }

    private void ClearForm()
    {
        editing = null;
        dgv.ClearSelection();
        txtId.Clear();
        txtCustomer.Clear();
        txtPhone.Clear();
        txtDescription.Clear();
        txtCost.Clear();
        cboDevice.SelectedIndex = -1;
        cboStatus.SelectedIndex = 0;
        dtpReceived.Value = DateTime.Today;
        ShowImage("");
        errorProvider.Clear();
        txtId.Focus();
    }

    /// <summary>Nạp ảnh qua FileStream rồi sao chép sang Bitmap để không khóa file ảnh.</summary>
    private void ShowImage(string path)
    {
        imagePath = path;
        Image? old = picDevice.Image;
        picDevice.Image = null;
        old?.Dispose();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var img = Image.FromStream(fs);
            picDevice.Image = new Bitmap(img);
        }
        catch (Exception ex)
        {
            imagePath = "";
            MessageBox.Show("Không đọc được ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnChooseImage_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Chọn ảnh thiết bị",
            Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        ShowImage(dlg.FileName);
        lblMessage.Text = "Đã chọn ảnh thiết bị. Nhấn Thêm hoặc Cập nhật để lưu vào đơn.";
    }

    // ============================ NGHIỆP VỤ ============================
    private static bool TryParseCost(string text, out decimal cost) =>
        decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out cost);

    private bool ValidateInputs(RepairOrder? current)
    {
        errorProvider.Clear();
        bool ok = true;
        void Fail(Control c, string msg) { errorProvider.SetError(c, msg); ok = false; }

        string id = txtId.Text.Trim();
        if (id.Length == 0)
            Fail(txtId, "Mã đơn không được để trống.");
        else if (all.Any(x => !ReferenceEquals(x, current) && string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)))
            Fail(txtId, "Mã đơn đã tồn tại.");

        if (txtCustomer.Text.Trim().Length == 0)
            Fail(txtCustomer, "Tên khách hàng không được để trống.");
        if (txtPhone.Text.Trim().Length == 0)
            Fail(txtPhone, "Số điện thoại không được để trống.");
        if (cboDevice.SelectedIndex < 0)
            Fail(cboDevice, "Hãy chọn loại thiết bị.");
        if (txtDescription.Text.Trim().Length == 0)
            Fail(txtDescription, "Mô tả lỗi là bắt buộc.");

        if (txtCost.Text.Trim().Length == 0)
            Fail(txtCost, "Chi phí dự kiến không được để trống.");
        else if (!TryParseCost(txtCost.Text, out decimal cost))
            Fail(txtCost, "Chi phí phải là một số.");
        else if (cost < 0)
            Fail(txtCost, "Chi phí không được âm.");

        if (!ok) lblMessage.Text = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra các ô báo lỗi.";
        return ok;
    }

    private void FillFromForm(RepairOrder o)
    {
        TryParseCost(txtCost.Text, out decimal cost);
        o.Id = txtId.Text.Trim();
        o.Customer = txtCustomer.Text.Trim();
        o.Phone = txtPhone.Text.Trim();
        o.DeviceType = (string)cboDevice.SelectedItem!;
        o.Description = txtDescription.Text.Trim();
        o.ReceivedDate = dtpReceived.Value.Date;
        o.Cost = cost;
        o.Status = (string)cboStatus.SelectedItem!;
        o.ImagePath = imagePath;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        if (!ValidateInputs(null)) return;
        if (MessageBox.Show($"Thêm đơn {txtId.Text.Trim()} của khách hàng {txtCustomer.Text.Trim()}?",
                "Xác nhận thêm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var o = new RepairOrder();
        FillFromForm(o);
        all.Add(o);
        ApplyFilter(o);
        lblMessage.Text = $"Đã thêm đơn {o.Id}.";
    }

    private void BtnUpdate_Click(object? sender, EventArgs e)
    {
        if (editing == null)
        {
            MessageBox.Show("Hãy chọn một đơn trong danh sách để cập nhật.", "Chưa chọn đơn",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ValidateInputs(editing)) return;
        if (MessageBox.Show($"Cập nhật đơn {editing.Id}?", "Xác nhận cập nhật",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        FillFromForm(editing);
        ApplyFilter(editing);
        lblMessage.Text = $"Đã cập nhật đơn {editing.Id}.";
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (editing == null)
        {
            MessageBox.Show("Hãy chọn một đơn trong danh sách để xóa.", "Chưa chọn đơn",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show($"Xóa đơn {editing.Id} của {editing.Customer}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        string id = editing.Id;
        all.Remove(editing);
        ClearForm();
        ApplyFilter();
        lblMessage.Text = $"Đã xóa đơn {id}.";
    }

    // ============================ XUẤT CSV ============================
    private void ExportCsv()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Xuất danh sách đơn sửa chữa",
            Filter = "CSV (*.csv)|*.csv",
            FileName = "don_sua_chua.csv"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("Mã đơn,Tên khách hàng,Số điện thoại,Loại thiết bị,Mô tả lỗi,Ngày tiếp nhận,Chi phí dự kiến,Trạng thái,Ảnh thiết bị");
        foreach (var o in view)
            sb.AppendLine(string.Join(",", Csv(o.Id), Csv(o.Customer), Csv(o.Phone), Csv(o.DeviceType), Csv(o.Description),
                o.ReceivedDate.ToString("dd/MM/yyyy"), o.Cost.ToString("0", CultureInfo.InvariantCulture), Csv(o.Status), Csv(o.ImagePath)));

        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
        lblMessage.Text = $"Đã xuất {view.Count} đơn ra {dlg.FileName}";
        MessageBox.Show($"Đã xuất {view.Count} đơn ra:\n{dlg.FileName}", "Xuất CSV",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
