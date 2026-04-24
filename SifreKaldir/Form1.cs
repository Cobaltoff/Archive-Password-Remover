using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

namespace SifreKaldir
{
    public partial class Form1 : Form
    {
        // Colors
        static readonly Color BG = Color.FromArgb(15, 15, 19);
        static readonly Color CARD = Color.FromArgb(22, 22, 31);
        static readonly Color BORDER = Color.FromArgb(42, 42, 61);
        static readonly Color ACCENT = Color.FromArgb(68, 85, 255);
        static readonly Color GREEN = Color.FromArgb(68, 204, 136);
        static readonly Color TEXT = Color.White;
        static readonly Color GRAY = Color.FromArgb(136, 136, 153);
        static readonly Color ERROR = Color.FromArgb(255, 68, 85);

        string? selectedFile = null;
        bool isPasswordHidden = true;
        readonly string configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "password_remover.cfg");

        // UI Controls
        Panel pnlDrop = null!;
        Label lblDrop = null!, lblPath = null!, lblStatus = null!;
        TextBox txtPassword = null!, txtSavePath = null!;
        Button btnEye = null!, btnFolder = null!, btnStart = null!;
        ProgressBar progress = null!;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Password Remover";
            this.Size = new Size(540, 640);
            this.MinimumSize = new Size(540, 640);
            this.BackColor = BG;
            this.ForeColor = TEXT;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AllowDrop = true;
            this.Font = new Font("Segoe UI", 10f);

            CreateUI();
            LoadConfig();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Designer requirement
        }

        void CreateUI()
        {
            int y = 25;

            // --- Emoji ---
            Add(new Label
            {
                Text = "🔓",
                Font = new Font("Segoe UI Emoji", 32f),
                ForeColor = TEXT,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(500, 52),
                Location = new Point(20, y),
                TextAlign = ContentAlignment.MiddleCenter
            }); y += 56;

            // --- Title ---
            Add(new Label
            {
                Text = "Password Remover",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = TEXT,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(500, 36),
                Location = new Point(20, y),
                TextAlign = ContentAlignment.MiddleCenter
            }); y += 38;

            Add(new Label
            {
                Text = "Removes passwords from ZIP and RAR archives",
                ForeColor = GRAY,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(500, 22),
                Location = new Point(20, y),
                TextAlign = ContentAlignment.MiddleCenter
            }); y += 32;

            // --- Drop Area ---
            pnlDrop = new Panel
            {
                BackColor = CARD,
                Location = new Point(30, y),
                Size = new Size(480, 85),
                Cursor = Cursors.Hand
            };
            pnlDrop.Paint += PnlDrop_Paint;
            pnlDrop.Click += (s, e) => OpenFileSelectionDialog();
            pnlDrop.MouseEnter += (s, e) => { if (selectedFile == null) pnlDrop.BackColor = Color.FromArgb(28, 28, 42); };
            pnlDrop.MouseLeave += (s, e) => { if (selectedFile == null) pnlDrop.BackColor = CARD; };
            pnlDrop.AllowDrop = true;
            pnlDrop.DragEnter += Panel_DragEnter;
            pnlDrop.DragDrop += Panel_DragDrop;
            this.Controls.Add(pnlDrop);

            lblDrop = new Label
            {
                Text = "📂   Drag & drop file here\r\n        or click to browse",
                ForeColor = GRAY,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(460, 65),
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblDrop.Click += (s, e) => OpenFileSelectionDialog();
            pnlDrop.Controls.Add(lblDrop);
            y += 95;

            lblPath = Add(new Label
            {
                Text = "",
                ForeColor = GREEN,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(480, 18),
                Location = new Point(30, y),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f)
            }); y += 26;

            // --- Password ---
            Add(CreateHeaderLabel("Password", new Point(30, y))); y += 24;

            var pwdPanel = CreateCardPanel(new Point(30, y), new Size(480, 46));
            txtPassword = new TextBox
            {
                UseSystemPasswordChar = true,
                BackColor = CARD,
                ForeColor = TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 12f),
                Location = new Point(12, 12),
                Size = new Size(418, 22)
            };
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _ = StartProcessingAsync(); };
            btnEye = CreateFlatButton("👁", new Point(438, 8));
            btnEye.Click += (s, e) => {
                isPasswordHidden = !isPasswordHidden;
                txtPassword.UseSystemPasswordChar = isPasswordHidden;
                btnEye.ForeColor = isPasswordHidden ? GRAY : TEXT;
            };
            pwdPanel.Controls.Add(txtPassword);
            pwdPanel.Controls.Add(btnEye);
            y += 56;

            // --- Save Location ---
            Add(CreateHeaderLabel("Save Location", new Point(30, y))); y += 24;

            var locPanel = CreateCardPanel(new Point(30, y), new Size(480, 46));
            txtSavePath = new TextBox
            {
                BackColor = CARD,
                ForeColor = GRAY,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                ReadOnly = true,
                Location = new Point(12, 13),
                Size = new Size(418, 20),
                Text = "Not selected yet — will remember once chosen"
            };
            btnFolder = CreateFlatButton("📁", new Point(438, 8));
            btnFolder.Click += (s, e) => SelectFolder();
            locPanel.Controls.Add(txtSavePath);
            locPanel.Controls.Add(btnFolder);
            y += 56;

            // --- Progress ---
            progress = new ProgressBar
            {
                Location = new Point(30, y),
                Size = new Size(480, 10),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visible = false
            };
            Add(progress); y += 20;

            lblStatus = Add(new Label
            {
                Text = "",
                ForeColor = GRAY,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(480, 20),
                Location = new Point(30, y),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f)
            }); y += 34;

            // --- Start Button ---
            btnStart = new Button
            {
                Text = "  🔓  Start  ",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = ACCENT,
                ForeColor = TEXT,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(210, 50),
                Location = new Point(165, y),
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += (s, e) => _ = StartProcessingAsync();
            btnStart.MouseEnter += (s, e) => { if (btnStart.Enabled) btnStart.BackColor = Color.FromArgb(85, 102, 255); };
            btnStart.MouseLeave += (s, e) => { if (btnStart.Enabled) btnStart.BackColor = ACCENT; };
            Add(btnStart);
        }

        // ── Helper UI Methods ─────────────────────────────────────────────
        T Add<T>(T ctrl) where T : Control { this.Controls.Add(ctrl); return ctrl; }

        Label CreateHeaderLabel(string text, Point loc) => new Label
        {
            Text = text,
            ForeColor = Color.FromArgb(170, 170, 204),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = loc,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };

        Panel CreateCardPanel(Point loc, Size size)
        {
            var p = new Panel { BackColor = CARD, Location = loc, Size = size };
            p.Paint += (s, e) => {
                using var pen = new Pen(BORDER, 2);
                e.Graphics.DrawRectangle(pen, 1, 1, p.Width - 3, p.Height - 3);
            };
            this.Controls.Add(p);
            return p;
        }

        Button CreateFlatButton(string text, Point loc)
        {
            var b = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = GRAY,
                Size = new Size(36, 30),
                Location = loc,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Emoji", 13f)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        void PnlDrop_Paint(object? s, PaintEventArgs e)
        {
            using var pen = new Pen(BORDER, 2);
            pen.DashStyle = DashStyle.Dash;
            e.Graphics.DrawRectangle(pen, 1, 1, pnlDrop.Width - 3, pnlDrop.Height - 3);
        }

        // ── Drag & Drop ───────────────────────────────────────────
        void Panel_DragEnter(object? s, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        void Panel_DragDrop(object? s, DragEventArgs e)
        {
            var files = e.Data?.GetData(DataFormats.FileDrop) as string[];
            if (files?.Length > 0) LoadSelectedFile(files[0]);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
            base.OnDragEnter(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            var files = e.Data?.GetData(DataFormats.FileDrop) as string[];
            if (files?.Length > 0) LoadSelectedFile(files[0]);
            base.OnDragDrop(e);
        }

        // ── File Operations ─────────────────────────────────────────
        void OpenFileSelectionDialog()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Select Archive",
                Filter = "Archive Files (*.zip;*.rar)|*.zip;*.rar"
            };
            if (dlg.ShowDialog() == DialogResult.OK) LoadSelectedFile(dlg.FileName);
        }

        void LoadSelectedFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (ext != ".zip" && ext != ".rar")
            {
                MessageBox.Show("Only .zip and .rar formats are supported.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            selectedFile = path;
            lblDrop.Text = $"✅   {Path.GetFileName(path)}";
            lblDrop.ForeColor = GREEN;
            pnlDrop.BackColor = Color.FromArgb(13, 31, 22);
            lblPath.Text = path;
        }

        void SelectFolder()
        {
            using var dlg = new FolderBrowserDialog { Description = "Select Save Location" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtSavePath.Text = dlg.SelectedPath;
                txtSavePath.ForeColor = TEXT;
                File.WriteAllText(configPath, dlg.SelectedPath);
            }
        }

        void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string path = File.ReadAllText(configPath).Trim();
                    if (Directory.Exists(path))
                    {
                        txtSavePath.Text = path;
                        txtSavePath.ForeColor = TEXT;
                    }
                }
            }
            catch { }
        }

        // ── Main Processing ───────────────────────────────────────────────
        async Task StartProcessingAsync()
        {
            if (selectedFile == null)
            {
                MessageBox.Show("Please select a file.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter the password.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }

            string saveDir = txtSavePath.Text;
            if (!Directory.Exists(saveDir))
            {
                SelectFolder();
                saveDir = txtSavePath.Text;
                if (!Directory.Exists(saveDir)) return;
            }

            string password = txtPassword.Text;
            string currentFile = selectedFile;
            string ext = Path.GetExtension(currentFile).ToLower();

            // Lock UI
            btnStart.Enabled = false;
            btnStart.BackColor = Color.FromArgb(51, 51, 68);
            btnStart.Text = "  ⏳  Processing...";
            progress.Value = 0;
            progress.Visible = true;
            lblStatus.ForeColor = GRAY;
            lblStatus.Text = "Removing password...";

            var prog = new Progress<int>(v => {
                if (v >= 0 && v <= 100) progress.Value = v;
            });

            string outputFile = "";
            try
            {
                if (ext == ".zip")
                    outputFile = await Task.Run(() => ProcessZip(currentFile, password, saveDir, prog));
                else
                    outputFile = await Task.Run(() => ProcessRar(currentFile, password, saveDir, prog));

                progress.Value = 100;
                lblStatus.ForeColor = GREEN;
                lblStatus.Text = $"✅ Completed: {Path.GetFileName(outputFile)}";
                MessageBox.Show($"Password removed successfully!\n\nFile saved to:\n{outputFile}",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                selectedFile = null;
                txtPassword.Text = "";
                lblDrop.Text = "📂   Drag & drop file here\r\n        or click to browse";
                lblDrop.ForeColor = GRAY;
                pnlDrop.BackColor = CARD;
                lblPath.Text = "";
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = ERROR;
                lblStatus.Text = "❌ An error occurred";
                string msg = ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
                          || ex.Message.Contains("Bad")
                    ? "Incorrect password! Please check."
                    : $"Error: {ex.Message}";
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStart.Enabled = true;
                btnStart.BackColor = ACCENT;
                btnStart.Text = "  🔓  Start  ";
            }
        }

        // ── ZIP Processing ─────────────────────────────────────────────────────
        static string ProcessZip(string src, string password, string outputDir, IProgress<int> prog)
        {
            string dst = Path.Combine(outputDir,
                Path.GetFileNameWithoutExtension(src) + "_unlocked.zip");
            byte[] pwd = System.Text.Encoding.UTF8.GetBytes(password);

            using var archive = ArchiveFactory.OpenArchive(new FileStream(src, FileMode.Open, FileAccess.Read),
                new SharpCompress.Readers.ReaderOptions { Password = password });

            var entries = archive.Entries;
            int total = 0;
            foreach (var e in entries) if (!e.IsDirectory) total++;

            using var fs = new FileStream(dst, FileMode.Create);
            using var zOut = new ZipArchiveWriter(fs);

            int completed = 0;
            foreach (var entry in entries)
            {
                if (entry.IsDirectory) continue;
                using var input = entry.OpenEntryStream();
                using var ms = new MemoryStream();
                input.CopyTo(ms);
                ms.Position = 0;
                zOut.Write(entry.Key!, ms);
                completed++;
                prog.Report((int)((double)completed / total * 100));
            }

            return dst;
        }

        // ── RAR Processing ─────────────────────────────────────────────────────
        static string ProcessRar(string src, string password, string outputDir, IProgress<int> prog)
        {
            string dst = Path.Combine(outputDir,
                Path.GetFileNameWithoutExtension(src) + "_unlocked.zip");

            using var archive = ArchiveFactory.OpenArchive(new FileStream(src, FileMode.Open, FileAccess.Read),
                new SharpCompress.Readers.ReaderOptions { Password = password });

            var entries = archive.Entries;
            int total = 0;
            foreach (var e in entries) if (!e.IsDirectory) total++;

            using var fs = new FileStream(dst, FileMode.Create);
            using var zOut = new ZipArchiveWriter(fs);

            int completed = 0;
            foreach (var entry in entries)
            {
                if (entry.IsDirectory) continue;
                using var input = entry.OpenEntryStream();
                using var ms = new MemoryStream();
                input.CopyTo(ms);
                ms.Position = 0;
                zOut.Write(entry.Key!, ms);
                completed++;
                prog.Report((int)((double)completed / total * 100));
            }

            return dst;
        }
    }

    // ── Helper: ZIP Writer ────────────────────────────────────────
    class ZipArchiveWriter : IDisposable
    {
        readonly ZipArchive _zip;
        public ZipArchiveWriter(Stream s) =>
            _zip = new ZipArchive(s, ZipArchiveMode.Create, leaveOpen: true);

        public void Write(string name, Stream data)
        {
            var entry = _zip.CreateEntry(name, CompressionLevel.Optimal);
            using var target = entry.Open();
            data.CopyTo(target);
        }

        public void Dispose() => _zip.Dispose();
    }
}