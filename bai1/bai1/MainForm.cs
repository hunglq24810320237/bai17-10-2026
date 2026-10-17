using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace Bai1;

/// <summary>
/// Bài 1: Clinic Appointment Manager - quản lý lịch hẹn phòng khám.
/// Bố cục dùng TableLayoutPanel, danh sách dùng DataGridView + BindingSource + BindingList.
/// </summary>
public class MainForm : Form
{
    private static readonly string[] Doctors =
    {
        "BS. Nguyễn Văn Hải", "BS. Trần Thị Mai", "BS. Lê Quốc Bảo", "BS. Phạm Thu Hà"
    };
    private static readonly string[] Statuses = { "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy" };
    private const string All = "Tất cả";
    private static readonly Regex PhoneRegex = new(@"^(0|\+84)\d{9}$", RegexOptions.Compiled);

    // Dữ liệu: danh sách gốc và danh sách đang hiển thị (sau khi tìm kiếm/lọc).
    private readonly BindingList<Appointment> all = new();
    private BindingList<Appointment> view = new();
    private readonly BindingSource bindingSource = new();
    private readonly ErrorProvider errorProvider = new();
    private Appointment? editing;

    // Điều khiển
    private readonly TextBox txtId = new(), txtName = new(), txtPhone = new(), txtReason = new(), txtSearch = new();
    private readonly DateTimePicker dtpTime = new(), dtpFilterDate = new();
    private readonly ComboBox cboDoctor = new(), cboStatus = new(), cboFilterStatus = new();
    private readonly CheckBox chkFilterDate = new();
    private readonly Button btnAdd = new(), btnUpdate = new(), btnDelete = new(), btnClear = new();
    private readonly DataGridView dgv = new();
    private readonly ToolStripStatusLabel lblCount = new(), lblMessage = new();

    public MainForm()
    {
        Text = "Clinic Appointment Manager - Quản lý lịch hẹn phòng khám";
        Font = new Font("Segoe UI", 9.5F);
        ClientSize = new Size(1040, 660);
        MinimumSize = new Size(900, 600);
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
        lblMessage.Spring = true;
        lblMessage.TextAlign = ContentAlignment.MiddleLeft;
        lblMessage.Text = "Sẵn sàng";
        status.Items.AddRange(new ToolStripItem[] { lblCount, lblMessage });

        // Khung chia vùng: nhập liệu / bộ lọc / danh sách.
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

        root.Controls.Add(BuildInputGroup(), 0, 0);
        root.Controls.Add(BuildFilterBar(), 0, 1);
        root.Controls.Add(BuildGrid(), 0, 2);

        Controls.Add(root);
        Controls.Add(status);
        Controls.Add(menu);
        MainMenuStrip = menu;
    }

    private GroupBox BuildInputGroup()
    {
        var grp = new GroupBox
        {
            Text = "Thông tin lịch hẹn",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            Padding = new Padding(6)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        cboDoctor.DropDownStyle = ComboBoxStyle.DropDownList;
        cboDoctor.Items.AddRange(Doctors);
        cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboStatus.Items.AddRange(Statuses);
        dtpTime.Format = DateTimePickerFormat.Custom;
        dtpTime.CustomFormat = "dd/MM/yyyy  HH:mm";
        txtReason.Multiline = true;
        txtReason.Height = 52;
        txtReason.ScrollBars = ScrollBars.Vertical;

        AddRow(tbl, 0, "Mã lịch hẹn:", txtId, "Họ tên bệnh nhân:", txtName);
        AddRow(tbl, 1, "Số điện thoại:", txtPhone, "Ngày giờ hẹn:", dtpTime);
        AddRow(tbl, 2, "Bác sĩ:", cboDoctor, "Trạng thái:", cboStatus);

        tbl.Controls.Add(Lbl("Lý do khám:"), 0, 3);
        tbl.Controls.Add(txtReason, 1, 3);
        tbl.SetColumnSpan(txtReason, 3);
        txtReason.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtReason.Margin = new Padding(3, 4, 26, 4);

        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        StyleButton(btnAdd, "Thêm", BtnAdd_Click);
        StyleButton(btnUpdate, "Cập nhật", BtnUpdate_Click);
        StyleButton(btnDelete, "Xóa", BtnDelete_Click);
        StyleButton(btnClear, "Làm mới", (_, _) => ClearForm());
        buttons.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });
        tbl.Controls.Add(buttons, 1, 4);
        tbl.SetColumnSpan(buttons, 3);

        // Gỡ lỗi ngay khi người dùng sửa lại ô nhập.
        foreach (var t in new[] { txtId, txtName, txtPhone, txtReason })
            t.TextChanged += (s, _) => errorProvider.SetError((Control)s!, "");
        dtpTime.ValueChanged += (_, _) => errorProvider.SetError(dtpTime, "");
        cboDoctor.SelectedIndexChanged += (_, _) => errorProvider.SetError(cboDoctor, "");

        grp.Controls.Add(tbl);
        return grp;
    }

    private FlowLayoutPanel BuildFilterBar()
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 6) };

        txtSearch.Width = 260;
        txtSearch.PlaceholderText = "Gõ tên bệnh nhân hoặc số điện thoại...";
        txtSearch.TextChanged += (_, _) => ApplyFilter();   // tìm kiếm tức thời

        chkFilterDate.Text = "Lọc theo ngày hẹn:";
        chkFilterDate.AutoSize = true;
        chkFilterDate.CheckedChanged += (_, _) => { dtpFilterDate.Enabled = chkFilterDate.Checked; ApplyFilter(); };
        dtpFilterDate.Format = DateTimePickerFormat.Short;
        dtpFilterDate.Width = 110;
        dtpFilterDate.Enabled = false;
        dtpFilterDate.ValueChanged += (_, _) => ApplyFilter();

        cboFilterStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboFilterStatus.Items.Add(All);
        cboFilterStatus.Items.AddRange(Statuses);
        cboFilterStatus.SelectedIndex = 0;
        cboFilterStatus.SelectedIndexChanged += (_, _) => ApplyFilter();

        var lblSearch = Lbl("Tìm kiếm:");
        var lblStatus = Lbl("Trạng thái:");
        lblSearch.Margin = lblStatus.Margin = new Padding(3, 7, 3, 3);
        chkFilterDate.Margin = new Padding(16, 6, 3, 3);
        dtpFilterDate.Margin = new Padding(3, 3, 3, 3);
        lblStatus.Margin = new Padding(16, 7, 3, 3);

        bar.Controls.AddRange(new Control[] { lblSearch, txtSearch, chkFilterDate, dtpFilterDate, lblStatus, cboFilterStatus });
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

        dgv.Columns.Add(Col(nameof(Appointment.Id), "Mã lịch hẹn", 12));
        dgv.Columns.Add(Col(nameof(Appointment.PatientName), "Bệnh nhân", 20));
        dgv.Columns.Add(Col(nameof(Appointment.Phone), "Số điện thoại", 14));
        var colTime = Col(nameof(Appointment.Time), "Ngày giờ hẹn", 16);
        colTime.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        dgv.Columns.Add(colTime);
        dgv.Columns.Add(Col(nameof(Appointment.Doctor), "Bác sĩ", 18));
        dgv.Columns.Add(Col(nameof(Appointment.Reason), "Lý do khám", 24));
        dgv.Columns.Add(Col(nameof(Appointment.Status), "Trạng thái", 12));

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
            c.Margin = new Padding(3, 4, 26, 4);   // chừa chỗ cho biểu tượng ErrorProvider
        }
    }

    private static void StyleButton(Button b, string text, EventHandler onClick)
    {
        b.Text = text;
        b.Size = new Size(96, 32);
        b.Click += onClick;
    }

    // ============================ DỮ LIỆU ============================
    private void SeedData()
    {
        DateTime d = DateTime.Today;
        all.Add(new Appointment { Id = "LH001", PatientName = "Nguyễn Thị Lan", Phone = "0912345678", Time = d.AddHours(8), Doctor = Doctors[0], Reason = "Khám tổng quát định kỳ", Status = "Hoàn tất" });
        all.Add(new Appointment { Id = "LH002", PatientName = "Trần Văn Minh", Phone = "0987654321", Time = d.AddHours(9.5), Doctor = Doctors[1], Reason = "Đau đầu, chóng mặt", Status = "Hoàn tất" });
        all.Add(new Appointment { Id = "LH003", PatientName = "Lê Hoàng Nam", Phone = "0903111222", Time = d.AddHours(23), Doctor = Doctors[2], Reason = "Tái khám sau phẫu thuật", Status = "Chờ khám" });
        all.Add(new Appointment { Id = "LH004", PatientName = "Phạm Thu Trang", Phone = "0977888999", Time = d.AddDays(1).AddHours(8.5), Doctor = Doctors[3], Reason = "Khám da liễu", Status = "Chờ khám" });
        all.Add(new Appointment { Id = "LH005", PatientName = "Đỗ Quang Huy", Phone = "0935123456", Time = d.AddDays(1).AddHours(10), Doctor = Doctors[0], Reason = "Đau dạ dày kéo dài", Status = "Đã hủy" });
        all.Add(new Appointment { Id = "LH006", PatientName = "Vũ Thị Hạnh", Phone = "0966333444", Time = d.AddDays(2).AddHours(14), Doctor = Doctors[1], Reason = "Khám thai định kỳ", Status = "Chờ khám" });
    }

    /// <summary>Tìm kiếm + lọc, đưa kết quả vào BindingList đang hiển thị và cập nhật StatusStrip.</summary>
    private void ApplyFilter(Appointment? keep = null)
    {
        string q = txtSearch.Text.Trim();
        string status = cboFilterStatus.SelectedItem as string ?? All;

        var items = all.Where(a =>
            (q.Length == 0
                || a.PatientName.Contains(q, StringComparison.CurrentCultureIgnoreCase)
                || a.Phone.Contains(q, StringComparison.OrdinalIgnoreCase))
            && (!chkFilterDate.Checked || a.Time.Date == dtpFilterDate.Value.Date)
            && (status == All || a.Status == status)).ToList();

        view = new BindingList<Appointment>(items);
        bindingSource.DataSource = view;
        lblCount.Text = $"Số lịch hẹn hiển thị: {view.Count} / {all.Count}";

        if (keep != null) SelectRow(keep); else dgv.ClearSelection();
    }

    private void SelectRow(Appointment item)
    {
        foreach (DataGridViewRow row in dgv.Rows)
        {
            if (!ReferenceEquals(row.DataBoundItem, item)) continue;
            dgv.CurrentCell = row.Cells[0];
            row.Selected = true;
            return;
        }
    }

    // Chọn một dòng thì nạp dữ liệu lên khu vực nhập.
    private void LoadSelected()
    {
        if (dgv.SelectedRows.Count == 0) return;
        if (dgv.SelectedRows[0].DataBoundItem is not Appointment a) return;
        editing = a;
        errorProvider.Clear();
        txtId.Text = a.Id;
        txtName.Text = a.PatientName;
        txtPhone.Text = a.Phone;
        dtpTime.Value = a.Time;
        cboDoctor.SelectedItem = a.Doctor;
        txtReason.Text = a.Reason;
        cboStatus.SelectedItem = a.Status;
        errorProvider.Clear();
    }

    private void ClearForm()
    {
        editing = null;
        dgv.ClearSelection();
        txtId.Clear();
        txtName.Clear();
        txtPhone.Clear();
        txtReason.Clear();
        dtpTime.Value = DateTime.Now.AddHours(1);
        cboDoctor.SelectedIndex = -1;
        cboStatus.SelectedIndex = 0;
        errorProvider.Clear();
        txtId.Focus();
    }

    // ============================ NGHIỆP VỤ ============================
    private static DateTime Minute(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0);

    /// <summary>Kiểm tra dữ liệu, báo lỗi bằng ErrorProvider cạnh từng ô. Trả về true nếu hợp lệ.</summary>
    private bool ValidateInputs(Appointment? current)
    {
        errorProvider.Clear();
        bool ok = true;
        void Fail(Control c, string msg) { errorProvider.SetError(c, msg); ok = false; }

        string id = txtId.Text.Trim();
        if (id.Length == 0)
            Fail(txtId, "Mã lịch hẹn không được để trống.");
        else if (all.Any(x => !ReferenceEquals(x, current) && string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)))
            Fail(txtId, "Mã lịch hẹn đã tồn tại.");

        if (txtName.Text.Trim().Length == 0)
            Fail(txtName, "Tên bệnh nhân không được để trống.");

        string phone = txtPhone.Text.Trim();
        if (phone.Length == 0)
            Fail(txtPhone, "Số điện thoại không được để trống.");
        else if (!PhoneRegex.IsMatch(phone))
            Fail(txtPhone, "Số điện thoại không hợp lệ (10 chữ số, bắt đầu bằng 0).");

        if (cboDoctor.SelectedIndex < 0)
            Fail(cboDoctor, "Hãy chọn bác sĩ.");

        if (txtReason.Text.Trim().Length == 0)
            Fail(txtReason, "Lý do khám không được để trống.");

        // Ngày hẹn không nằm trong quá khứ (khi sửa mà không đổi giờ hẹn thì không kiểm tra lại).
        bool timeChanged = current == null || Minute(dtpTime.Value) != Minute(current.Time);
        if (timeChanged && Minute(dtpTime.Value) < Minute(DateTime.Now))
            Fail(dtpTime, "Ngày giờ hẹn không được nằm trong quá khứ.");

        if (!ok) lblMessage.Text = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra các ô báo lỗi.";
        return ok;
    }

    private void FillFromForm(Appointment a)
    {
        a.Id = txtId.Text.Trim();
        a.PatientName = txtName.Text.Trim();
        a.Phone = txtPhone.Text.Trim();
        a.Time = Minute(dtpTime.Value);
        a.Doctor = (string)cboDoctor.SelectedItem!;
        a.Reason = txtReason.Text.Trim();
        a.Status = (string)cboStatus.SelectedItem!;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        if (!ValidateInputs(null)) return;
        if (MessageBox.Show($"Thêm lịch hẹn {txtId.Text.Trim()} của bệnh nhân {txtName.Text.Trim()}?",
                "Xác nhận thêm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var a = new Appointment();
        FillFromForm(a);
        all.Add(a);
        ApplyFilter(a);
        lblMessage.Text = $"Đã thêm lịch hẹn {a.Id}.";
    }

    private void BtnUpdate_Click(object? sender, EventArgs e)
    {
        if (editing == null)
        {
            MessageBox.Show("Hãy chọn một lịch hẹn trong danh sách để cập nhật.", "Chưa chọn lịch hẹn",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ValidateInputs(editing)) return;
        if (MessageBox.Show($"Cập nhật lịch hẹn {editing.Id}?", "Xác nhận cập nhật",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        FillFromForm(editing);
        ApplyFilter(editing);
        lblMessage.Text = $"Đã cập nhật lịch hẹn {editing.Id}.";
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (editing == null)
        {
            MessageBox.Show("Hãy chọn một lịch hẹn trong danh sách để xóa.", "Chưa chọn lịch hẹn",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show($"Xóa lịch hẹn {editing.Id} của {editing.PatientName}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        string id = editing.Id;
        all.Remove(editing);
        ClearForm();
        ApplyFilter();
        lblMessage.Text = $"Đã xóa lịch hẹn {id}.";
    }

    // ============================ XUẤT CSV ============================
    private void ExportCsv()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Xuất danh sách lịch hẹn",
            Filter = "CSV (*.csv)|*.csv",
            FileName = "lich_hen.csv"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("Mã lịch hẹn,Họ tên bệnh nhân,Số điện thoại,Ngày giờ hẹn,Bác sĩ,Lý do khám,Trạng thái");
        foreach (var a in view)
            sb.AppendLine(string.Join(",", Csv(a.Id), Csv(a.PatientName), Csv(a.Phone),
                a.Time.ToString("dd/MM/yyyy HH:mm"), Csv(a.Doctor), Csv(a.Reason), Csv(a.Status)));

        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));   // UTF-8 BOM để Excel đọc đúng tiếng Việt
        lblMessage.Text = $"Đã xuất {view.Count} lịch hẹn ra {dlg.FileName}";
        MessageBox.Show($"Đã xuất {view.Count} lịch hẹn ra:\n{dlg.FileName}", "Xuất CSV",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
