using System.ComponentModel;
using System.Text;

namespace Bai2;

/// <summary>
/// Bài 2: Lab Equipment Checkout - quản lý mượn trả thiết bị tại phòng thực hành.
/// </summary>
public class MainForm : Form
{
    private static readonly string[] Equipments =
    {
        "Kính hiển vi", "Bộ KIT Arduino", "Máy đo VOM", "Máy hiện sóng (Oscilloscope)",
        "Laptop thực hành", "Máy hàn thiếc"
    };
    private static readonly string[] Statuses = { "Đang mượn", "Quá hạn", "Đã trả" };
    private const string All = "Tất cả";

    private readonly BindingList<Checkout> all = new();
    private BindingList<Checkout> view = new();
    private readonly BindingSource bindingSource = new();
    private readonly ErrorProvider errorProvider = new();
    private Checkout? editing;

    private readonly TextBox txtId = new(), txtStudentId = new(), txtStudentName = new(), txtSearch = new();
    private readonly ComboBox cboEquipment = new(), cboFilter = new();
    private readonly NumericUpDown numQty = new();
    private readonly DateTimePicker dtpBorrow = new(), dtpDue = new(), dtpReturn = new();
    private readonly Label lblStatusValue = new();
    private readonly Button btnAdd = new(), btnUpdate = new(), btnDelete = new(), btnReturn = new(), btnClear = new();
    private readonly DataGridView dgv = new();
    private readonly ToolStripStatusLabel lblTotal = new(), lblOverdue = new(), lblShown = new(), lblMessage = new();

    public MainForm()
    {
        Text = "Lab Equipment Checkout - Quản lý mượn trả thiết bị phòng thực hành";
        Font = new Font("Segoe UI", 9.5F);
        ClientSize = new Size(1060, 660);
        MinimumSize = new Size(920, 600);
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
        var mnuExport = new ToolStripMenuItem("Xuất các phiếu đang lọc ra CSV...");
        var mnuExit = new ToolStripMenuItem("Thoát");
        mnuExport.ShortcutKeys = Keys.Control | Keys.E;
        mnuExport.Click += (_, _) => ExportCsv();
        mnuExit.Click += (_, _) => Close();
        mnuFile.DropDownItems.AddRange(new ToolStripItem[] { mnuExport, new ToolStripSeparator(), mnuExit });
        menu.Items.Add(mnuFile);

        var status = new StatusStrip { Dock = DockStyle.Bottom, SizingGrip = false, CanOverflow = false };
        foreach (var l in new[] { lblTotal, lblOverdue, lblShown })
            l.BorderSides = ToolStripStatusLabelBorderSides.Right;
        lblMessage.Spring = true;
        lblMessage.TextAlign = ContentAlignment.MiddleLeft;
        lblMessage.Text = "Sẵn sàng";
        status.Items.AddRange(new ToolStripItem[] { lblTotal, lblOverdue, lblShown, lblMessage });

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
            Text = "Phiếu mượn thiết bị",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, Padding = new Padding(6) };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        cboEquipment.DropDownStyle = ComboBoxStyle.DropDownList;
        cboEquipment.Items.AddRange(Equipments);
        numQty.Minimum = 0;
        numQty.Maximum = 100;
        foreach (var d in new[] { dtpBorrow, dtpDue, dtpReturn })
        {
            d.Format = DateTimePickerFormat.Custom;
            d.CustomFormat = "dd/MM/yyyy";
        }
        dtpReturn.ShowCheckBox = true;      // bỏ chọn = chưa trả
        dtpReturn.Checked = false;
        lblStatusValue.AutoSize = true;
        lblStatusValue.Font = new Font(Font, FontStyle.Bold);

        AddRow(tbl, 0, "Mã phiếu:", txtId, "Mã sinh viên:", txtStudentId);
        AddRow(tbl, 1, "Họ tên sinh viên:", txtStudentName, "Thiết bị:", cboEquipment);
        AddRow(tbl, 2, "Số lượng:", numQty, "Ngày mượn:", dtpBorrow);
        AddRow(tbl, 3, "Hạn trả:", dtpDue, "Ngày trả thực tế:", dtpReturn);

        tbl.Controls.Add(Lbl("Tình trạng:"), 0, 4);
        tbl.Controls.Add(lblStatusValue, 1, 4);
        lblStatusValue.Anchor = AnchorStyles.Left;

        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        StyleButton(btnAdd, "Thêm", 90, BtnAdd_Click);
        StyleButton(btnUpdate, "Cập nhật", 90, BtnUpdate_Click);
        StyleButton(btnDelete, "Xóa", 90, BtnDelete_Click);
        StyleButton(btnReturn, "Đánh dấu đã trả", 140, BtnReturn_Click);
        StyleButton(btnClear, "Làm mới", 90, (_, _) => ClearForm());
        buttons.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnReturn, btnClear });
        tbl.Controls.Add(buttons, 1, 5);
        tbl.SetColumnSpan(buttons, 3);

        foreach (var t in new[] { txtId, txtStudentId, txtStudentName })
            t.TextChanged += (s, _) => errorProvider.SetError((Control)s!, "");
        cboEquipment.SelectedIndexChanged += (_, _) => errorProvider.SetError(cboEquipment, "");
        numQty.ValueChanged += (_, _) => errorProvider.SetError(numQty, "");
        dtpBorrow.ValueChanged += (_, _) => { errorProvider.SetError(dtpBorrow, ""); errorProvider.SetError(dtpDue, ""); errorProvider.SetError(dtpReturn, ""); };
        dtpDue.ValueChanged += (_, _) => errorProvider.SetError(dtpDue, "");
        dtpReturn.ValueChanged += (_, _) => errorProvider.SetError(dtpReturn, "");

        grp.Controls.Add(tbl);
        return grp;
    }

    private FlowLayoutPanel BuildFilterBar()
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 6) };

        txtSearch.Width = 280;
        txtSearch.PlaceholderText = "Gõ mã sinh viên hoặc tên thiết bị...";
        txtSearch.TextChanged += (_, _) => ApplyFilter();

        cboFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        cboFilter.Items.Add(All);
        cboFilter.Items.AddRange(Statuses);
        cboFilter.SelectedIndex = 0;
        cboFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var l1 = Lbl("Tìm kiếm:");
        var l2 = Lbl("Lọc tình trạng:");
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

        dgv.Columns.Add(Col(nameof(Checkout.Id), "Mã phiếu", 9));
        dgv.Columns.Add(Col(nameof(Checkout.StudentId), "Mã SV", 13));
        dgv.Columns.Add(Col(nameof(Checkout.StudentName), "Họ tên sinh viên", 16));
        dgv.Columns.Add(Col(nameof(Checkout.Equipment), "Thiết bị", 19));
        var colQty = Col(nameof(Checkout.Quantity), "SL", 5);
        colQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgv.Columns.Add(colQty);
        dgv.Columns.Add(DateCol(nameof(Checkout.BorrowDate), "Ngày mượn"));
        dgv.Columns.Add(DateCol(nameof(Checkout.DueDate), "Hạn trả"));
        dgv.Columns.Add(Col(nameof(Checkout.ReturnDateText), "Ngày trả thực tế", 14));
        dgv.Columns.Add(Col(nameof(Checkout.Status), "Tình trạng", 10));

        dgv.SelectionChanged += (_, _) => LoadSelected();
        dgv.CellFormatting += (_, e) =>
        {
            // Tô nền các phiếu quá hạn để dễ nhận diện.
            if (e.RowIndex < 0 || e.RowIndex >= dgv.Rows.Count) return;
            if (dgv.Rows[e.RowIndex].DataBoundItem is Checkout c && c.Status == "Quá hạn")
            {
                e.CellStyle!.BackColor = Color.FromArgb(255, 226, 226);
                e.CellStyle.ForeColor = Color.FromArgb(150, 20, 20);
            }
        };
        return dgv;
    }

    private static DataGridViewTextBoxColumn Col(string prop, string header, float weight) => new()
    {
        DataPropertyName = prop,
        HeaderText = header,
        Name = "col" + prop,
        FillWeight = weight
    };

    private static DataGridViewTextBoxColumn DateCol(string prop, string header)
    {
        var c = Col(prop, header, 11);
        c.DefaultCellStyle.Format = "dd/MM/yyyy";
        return c;
    }

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

    private static void StyleButton(Button b, string text, int width, EventHandler onClick)
    {
        b.Text = text;
        b.Size = new Size(width, 32);
        b.Click += onClick;
    }

    // ============================ DỮ LIỆU ============================
    private void SeedData()
    {
        DateTime t = DateTime.Today;
        all.Add(new Checkout { Id = "PM001", StudentId = "24810320237", StudentName = "Lê Quang Hưng", Equipment = Equipments[1], Quantity = 1, BorrowDate = t.AddDays(-3), DueDate = t.AddDays(4) });
        all.Add(new Checkout { Id = "PM002", StudentId = "24810320101", StudentName = "Nguyễn Thị Hoa", Equipment = Equipments[0], Quantity = 2, BorrowDate = t.AddDays(-10), DueDate = t.AddDays(-3) });
        all.Add(new Checkout { Id = "PM003", StudentId = "24810320155", StudentName = "Trần Minh Khôi", Equipment = Equipments[2], Quantity = 1, BorrowDate = t.AddDays(-8), DueDate = t.AddDays(-1), ReturnDate = t.AddDays(-2) });
        all.Add(new Checkout { Id = "PM004", StudentId = "24810320188", StudentName = "Phạm Gia Bảo", Equipment = Equipments[3], Quantity = 1, BorrowDate = t.AddDays(-14), DueDate = t.AddDays(-7) });
        all.Add(new Checkout { Id = "PM005", StudentId = "24810320209", StudentName = "Đỗ Thu Phương", Equipment = Equipments[4], Quantity = 3, BorrowDate = t.AddDays(-1), DueDate = t.AddDays(6) });
        all.Add(new Checkout { Id = "PM006", StudentId = "24810320132", StudentName = "Vũ Đức Anh", Equipment = Equipments[5], Quantity = 1, BorrowDate = t.AddDays(-6), DueDate = t.AddDays(1), ReturnDate = t.AddDays(-1) });
    }

    private void ApplyFilter(Checkout? keep = null)
    {
        string q = txtSearch.Text.Trim();
        string status = cboFilter.SelectedItem as string ?? All;

        var items = all.Where(c =>
            (q.Length == 0
                || c.StudentId.Contains(q, StringComparison.OrdinalIgnoreCase)
                || c.Equipment.Contains(q, StringComparison.CurrentCultureIgnoreCase))
            && (status == All || c.Status == status)).ToList();

        view = new BindingList<Checkout>(items);
        bindingSource.DataSource = view;
        UpdateStatusBar();

        if (keep != null) SelectRow(keep); else dgv.ClearSelection();
    }

    private void UpdateStatusBar()
    {
        lblTotal.Text = $"Tổng số phiếu: {all.Count}";
        lblOverdue.Text = $"Phiếu quá hạn: {all.Count(c => c.Status == "Quá hạn")}";
        lblShown.Text = $"Đang hiển thị: {view.Count}";
    }

    private void SelectRow(Checkout item)
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
        if (dgv.SelectedRows[0].DataBoundItem is not Checkout c) return;
        editing = c;
        txtId.Text = c.Id;
        txtStudentId.Text = c.StudentId;
        txtStudentName.Text = c.StudentName;
        cboEquipment.SelectedItem = c.Equipment;
        numQty.Value = Math.Clamp(c.Quantity, (int)numQty.Minimum, (int)numQty.Maximum);
        dtpBorrow.Value = c.BorrowDate;
        dtpDue.Value = c.DueDate;
        if (c.ReturnDate is DateTime r) { dtpReturn.Value = r; dtpReturn.Checked = true; }
        else { dtpReturn.Value = DateTime.Today; dtpReturn.Checked = false; }
        ShowStatus(c.Status);
        errorProvider.Clear();
    }

    private void ClearForm()
    {
        editing = null;
        dgv.ClearSelection();
        txtId.Clear();
        txtStudentId.Clear();
        txtStudentName.Clear();
        cboEquipment.SelectedIndex = -1;
        numQty.Value = 0;
        dtpBorrow.Value = DateTime.Today;
        dtpDue.Value = DateTime.Today.AddDays(7);
        dtpReturn.Value = DateTime.Today;
        dtpReturn.Checked = false;
        ShowStatus("");
        errorProvider.Clear();
        txtId.Focus();
    }

    private void ShowStatus(string status)
    {
        lblStatusValue.Text = status;
        lblStatusValue.ForeColor = status switch
        {
            "Quá hạn" => Color.Firebrick,
            "Đã trả" => Color.SeaGreen,
            "Đang mượn" => Color.SteelBlue,
            _ => ForeColor
        };
    }

    // ============================ NGHIỆP VỤ ============================
    private bool ValidateInputs(Checkout? current)
    {
        errorProvider.Clear();
        bool ok = true;
        void Fail(Control c, string msg) { errorProvider.SetError(c, msg); ok = false; }

        string id = txtId.Text.Trim();
        if (id.Length == 0)
            Fail(txtId, "Mã phiếu không được để trống.");
        else if (all.Any(x => !ReferenceEquals(x, current) && string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)))
            Fail(txtId, "Mã phiếu đã tồn tại.");

        if (txtStudentId.Text.Trim().Length == 0)
            Fail(txtStudentId, "Mã sinh viên không được để trống.");
        if (txtStudentName.Text.Trim().Length == 0)
            Fail(txtStudentName, "Họ tên sinh viên không được để trống.");
        if (cboEquipment.SelectedIndex < 0)
            Fail(cboEquipment, "Hãy chọn thiết bị.");
        if (numQty.Value <= 0)
            Fail(numQty, "Số lượng phải lớn hơn 0.");
        if (dtpDue.Value.Date < dtpBorrow.Value.Date)
            Fail(dtpDue, "Hạn trả không được trước ngày mượn.");
        if (dtpReturn.Checked && dtpReturn.Value.Date < dtpBorrow.Value.Date)
            Fail(dtpReturn, "Ngày trả thực tế không được trước ngày mượn.");

        if (!ok) lblMessage.Text = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra các ô báo lỗi.";
        return ok;
    }

    private void FillFromForm(Checkout c)
    {
        c.Id = txtId.Text.Trim();
        c.StudentId = txtStudentId.Text.Trim();
        c.StudentName = txtStudentName.Text.Trim();
        c.Equipment = (string)cboEquipment.SelectedItem!;
        c.Quantity = (int)numQty.Value;
        c.BorrowDate = dtpBorrow.Value.Date;
        c.DueDate = dtpDue.Value.Date;
        c.ReturnDate = dtpReturn.Checked ? dtpReturn.Value.Date : null;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        if (!ValidateInputs(null)) return;
        if (MessageBox.Show($"Thêm phiếu mượn {txtId.Text.Trim()} của sinh viên {txtStudentName.Text.Trim()}?",
                "Xác nhận thêm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var c = new Checkout();
        FillFromForm(c);
        all.Add(c);
        ApplyFilter(c);
        lblMessage.Text = $"Đã thêm phiếu {c.Id} ({c.Status}).";
    }

    private void BtnUpdate_Click(object? sender, EventArgs e)
    {
        if (editing == null)
        {
            MessageBox.Show("Hãy chọn một phiếu trong danh sách để cập nhật.", "Chưa chọn phiếu",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ValidateInputs(editing)) return;
        if (MessageBox.Show($"Cập nhật phiếu {editing.Id}?", "Xác nhận cập nhật",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        FillFromForm(editing);
        ApplyFilter(editing);
        lblMessage.Text = $"Đã cập nhật phiếu {editing.Id} ({editing.Status}).";
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (editing == null)
        {
            MessageBox.Show("Hãy chọn một phiếu trong danh sách để xóa.", "Chưa chọn phiếu",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show($"Xóa phiếu {editing.Id} của {editing.StudentName}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        string id = editing.Id;
        all.Remove(editing);
        ClearForm();
        ApplyFilter();
        lblMessage.Text = $"Đã xóa phiếu {id}.";
    }

    /// <summary>Đánh dấu đã trả: ghi ngày trả thực tế (ngày chọn ở ô "Ngày trả thực tế", mặc định hôm nay).</summary>
    private void BtnReturn_Click(object? sender, EventArgs e)
    {
        if (editing == null)
        {
            MessageBox.Show("Hãy chọn phiếu cần đánh dấu đã trả.", "Chưa chọn phiếu",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (editing.ReturnDate != null)
        {
            MessageBox.Show($"Phiếu {editing.Id} đã được trả ngày {editing.ReturnDate:dd/MM/yyyy}.", "Đã trả",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DateTime returnDate = dtpReturn.Checked ? dtpReturn.Value.Date : DateTime.Today;
        if (returnDate < editing.BorrowDate.Date)
        {
            errorProvider.SetError(dtpReturn, "Ngày trả thực tế không được trước ngày mượn.");
            return;
        }
        if (MessageBox.Show($"Đánh dấu phiếu {editing.Id} đã trả ngày {returnDate:dd/MM/yyyy}?", "Xác nhận trả thiết bị",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        editing.ReturnDate = returnDate;
        ApplyFilter(editing);
        lblMessage.Text = $"Đã trả phiếu {editing.Id} ngày {returnDate:dd/MM/yyyy}.";
    }

    // ============================ XUẤT CSV ============================
    private void ExportCsv()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Xuất danh sách phiếu mượn",
            Filter = "CSV (*.csv)|*.csv",
            FileName = "phieu_muon_thiet_bi.csv"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("Mã phiếu,Mã sinh viên,Họ tên sinh viên,Thiết bị,Số lượng,Ngày mượn,Hạn trả,Ngày trả thực tế,Tình trạng");
        foreach (var c in view)
            sb.AppendLine(string.Join(",", Csv(c.Id), Csv(c.StudentId), Csv(c.StudentName), Csv(c.Equipment), c.Quantity,
                c.BorrowDate.ToString("dd/MM/yyyy"), c.DueDate.ToString("dd/MM/yyyy"), c.ReturnDateText, Csv(c.Status)));

        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
        lblMessage.Text = $"Đã xuất {view.Count} phiếu ra {dlg.FileName}";
        MessageBox.Show($"Đã xuất {view.Count} phiếu ra:\n{dlg.FileName}", "Xuất CSV",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
