using System.ComponentModel;
using System.Diagnostics;
using CleanArchitectureGenerator.App.UI;
using CleanArchitectureGenerator.Engine;
using CleanArchitectureGenerator.Engine.PostActions;
using CleanArchitectureGenerator.Engine.Validation;

namespace CleanArchitectureGenerator.App;

/// <summary>
/// Tek pencerelik arayüz: solda ayarlar ve "Oluştur" butonu, sağda üretilecek yapının canlı önizlemesi ve işlem günlüğü.
/// </summary>
internal sealed class MainForm : Form
{
    private const string RepositoryUrl = "https://github.com/yigitkalaycioglu/clean-architecture-generator";

    /// <summary>Çözüm klasörünün yolu bundan uzunsa derleme çıktıları Windows'un 260 karakter sınırını aşabilir.</summary>
    private const int LongPathWarningLength = 110;

    private static readonly (DatabaseProvider Value, string Text)[] DatabaseOptions =
    [
        (DatabaseProvider.SqlServer, "SQL Server"),
        (DatabaseProvider.PostgreSql, "PostgreSQL"),
        (DatabaseProvider.Sqlite, "SQLite")
    ];

    private static readonly (SolutionFormat Value, string Text)[] SolutionFormatOptions =
    [
        (SolutionFormat.Sln, ".sln (klasik)"),
        (SolutionFormat.Slnx, ".slnx (yeni XML)")
    ];

    private static readonly (AuthenticationMode Value, string Text)[] AuthenticationOptions =
    [
        (AuthenticationMode.Local, "Yerleşik: ASP.NET Core Identity + JWT"),
        (AuthenticationMode.External, "Harici sağlayıcı: OpenID Connect (Entra ID, Auth0, Keycloak…)")
    ];

    private readonly SolutionGenerator _generator;
    private readonly AppSettings _settings;

    private readonly TextBox _nameTextBox = new();
    private readonly Label _nameHintLabel = new();
    private readonly LinkLabel _suggestionLink = new();
    private readonly TextBox _locationTextBox = new();
    private readonly Button _browseButton = new();
    private readonly ComboBox _databaseComboBox = new();
    private readonly ComboBox _solutionFormatComboBox = new();
    private readonly ComboBox _authenticationComboBox = new();
    private readonly CheckBox _sampleCheckBox = new();
    private readonly CheckBox _testsCheckBox = new();
    private readonly CheckBox _gitCheckBox = new();
    private readonly CheckBox _buildCheckBox = new();
    private readonly CheckBox _openFolderCheckBox = new();
    private readonly Button _generateButton = new();
    private readonly Label _targetLabel = new();

    private readonly Label _statsLabel = new();
    private readonly SplitContainer _mainSplit = new();
    private readonly SplitContainer _previewSplit = new();
    private readonly TreeView _tree = new();
    private readonly ImageList _treeImages = new();
    private readonly Label _filePathLabel = new();
    private readonly TextBox _codeTextBox = new();
    private readonly RichTextBox _logTextBox = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Button _openFolderButton = new();
    private readonly Button _openSolutionButton = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 15000 };
    private readonly System.Windows.Forms.Timer _previewTimer = new() { Interval = 250 };

    private ProjectRandomValues _randomValues = ProjectRandomValues.Create();
    private GenerationPlan? _plan;
    private GenerationResult? _lastResult;
    private CancellationTokenSource? _cancellation;
    private string? _selectedFilePath = "README.md";
    private bool _isBusy;

    public MainForm(SolutionGenerator generator, AppSettings settings)
    {
        _generator = generator;
        _settings = settings;

        BuildLayout();
        LoadSettings();
        WireEvents();
        RefreshPreview();
    }

    private enum LogKind
    {
        Info,
        Success,
        Warning,
        Error,
        Detail
    }

    private DatabaseProvider SelectedDatabase => DatabaseOptions[Math.Max(0, _databaseComboBox.SelectedIndex)].Value;

    private SolutionFormat SelectedSolutionFormat => SolutionFormatOptions[Math.Max(0, _solutionFormatComboBox.SelectedIndex)].Value;

    private AuthenticationMode SelectedAuthentication => AuthenticationOptions[Math.Max(0, _authenticationComboBox.SelectedIndex)].Value;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeTheming.SetCaptionColor(Handle, Theme.Header, Theme.HeaderText);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        NativeTheming.UseExplorerStyle(_tree);
        NativeTheming.UseDarkScrollBars(_codeTextBox);

        // SplitContainer sınırları ve oranları ancak kontroller boyutlandıktan sonra verilebilir.
        _mainSplit.Panel1MinSize = LogicalToDeviceUnits(160);
        _mainSplit.Panel2MinSize = LogicalToDeviceUnits(110);
        _mainSplit.SplitterDistance = Math.Max(_mainSplit.Panel1MinSize, (int)(_mainSplit.Height * 0.66));
        _previewSplit.Panel1MinSize = LogicalToDeviceUnits(180);
        _previewSplit.Panel2MinSize = LogicalToDeviceUnits(200);
        _previewSplit.SplitterDistance = Math.Max(_previewSplit.Panel1MinSize, (int)(_previewSplit.Width * 0.4));

        Log($"Hazır. Şablon kaynağı: {_generator.TemplateSource.Description}.", LogKind.Detail);
        Log("Ayarları seçin ve \"Çözümü oluştur\" butonuna basın.");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isBusy)
        {
            var answer = MessageBox.Show(this, "İşlem sürüyor. İptal edip çıkmak istiyor musunuz?", "İşlem sürüyor",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _cancellation?.Cancel();
        }

        SaveSettings();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewTimer.Dispose();
            _toolTip.Dispose();
            _treeImages.Dispose();
            _cancellation?.Dispose();
        }

        base.Dispose(disposing);
    }

    // ------------------------------------------------------------------ Yerleşim

    private void BuildLayout()
    {
        SuspendLayout();

        Text = "Clean Architecture Oluşturucu";
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1220, 780);
        MinimumSize = new Size(1000, 660);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        Icon = LoadApplicationIcon();

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14),
            BackColor = Theme.Background
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(CreateSettingsCard(), 0, 0);
        body.Controls.Add(CreatePreviewCard(), 1, 0);

        // Dock sırası: en son eklenen önce yerleşir (üst başlık, alt durum çubuğu, kalan alan gövde).
        Controls.Add(body);
        Controls.Add(CreateStatusStrip());
        Controls.Add(CreateHeader());

        ResumeLayout(false);
        PerformLayout();
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 82,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Header,
            Padding = new Padding(20, 14, 22, 14)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var logo = new PictureBox
        {
            Image = Theme.CreateLogo(LogicalToDeviceUnits(52)),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0)
        };

        var titles = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, BackColor = Theme.Header };
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        titles.Controls.Add(new Label
        {
            Text = "Clean Architecture Oluşturucu",
            Font = Theme.TitleFont,
            ForeColor = Theme.HeaderText,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Margin = Padding.Empty
        }, 0, 0);
        titles.Controls.Add(new Label
        {
            Text = "Güvenlik öncelikli Clean Architecture ASP.NET Core (.NET 10) çözümünü tek tıkla oluşturun",
            ForeColor = Theme.HeaderMuted,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(2, 2, 0, 0)
        }, 0, 1);

        var repositoryLink = new LinkLabel
        {
            Text = "Kaynak kod (GitHub)",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            LinkColor = Theme.HeaderMuted,
            ActiveLinkColor = Color.White,
            VisitedLinkColor = Theme.HeaderMuted,
            LinkBehavior = LinkBehavior.HoverUnderline
        };
        repositoryLink.LinkClicked += (_, _) => OpenWithShell(RepositoryUrl);

        header.Controls.Add(logo, 0, 0);
        header.Controls.Add(titles, 1, 0);
        header.Controls.Add(repositoryLink, 2, 0);
        return header;
    }

    private Control CreateSettingsCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(18, 14, 18, 16), Margin = new Padding(0, 0, 12, 0) };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            BackColor = Theme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        void AddRow(Control control, int top = 0)
        {
            control.Margin = new Padding(0, top, 0, 0);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(control, 0, layout.RowCount++);
        }

        AddRow(CreateSectionLabel("PROJE"));
        AddRow(CreateFieldLabel("Çözüm adı"), 8);

        _nameTextBox.Dock = DockStyle.Fill;
        _nameTextBox.MaxLength = SolutionNameValidator.MaxLength;
        AddRow(_nameTextBox, 3);

        _nameHintLabel.AutoSize = true;
        _nameHintLabel.MaximumSize = new Size(320, 0);
        _nameHintLabel.ForeColor = Theme.TextMuted;
        AddRow(_nameHintLabel, 4);

        _suggestionLink.AutoSize = true;
        _suggestionLink.Visible = false;
        _suggestionLink.LinkColor = Theme.Accent;
        AddRow(_suggestionLink, 2);

        AddRow(CreateFieldLabel("Konum"), 12);
        var locationRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, Margin = Padding.Empty };
        locationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        locationRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _locationTextBox.Dock = DockStyle.Fill;
        _locationTextBox.Margin = new Padding(0, 1, 6, 0);
        _browseButton.Text = "Gözat…";
        _browseButton.AutoSize = true;
        _browseButton.Margin = Padding.Empty;
        locationRow.Controls.Add(_locationTextBox, 0, 0);
        locationRow.Controls.Add(_browseButton, 1, 0);
        AddRow(locationRow, 3);

        var comboRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, AutoSize = true, Margin = Padding.Empty };
        comboRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        comboRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        comboRow.Controls.Add(CreateFieldLabel("Veritabanı"), 0, 0);
        comboRow.Controls.Add(CreateFieldLabel("Çözüm dosyası"), 1, 0);
        foreach (var comboBox in new[] { _databaseComboBox, _solutionFormatComboBox })
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.Dock = DockStyle.Fill;
        }

        _databaseComboBox.Margin = new Padding(0, 3, 6, 0);
        _solutionFormatComboBox.Margin = new Padding(6, 3, 0, 0);
        _databaseComboBox.Items.AddRange(DatabaseOptions.Select(option => (object)option.Text).ToArray());
        _solutionFormatComboBox.Items.AddRange(SolutionFormatOptions.Select(option => (object)option.Text).ToArray());
        comboRow.Controls.Add(_databaseComboBox, 0, 1);
        comboRow.Controls.Add(_solutionFormatComboBox, 1, 1);
        AddRow(comboRow, 12);

        AddRow(CreateFieldLabel("Kimlik doğrulama"), 12);
        _authenticationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _authenticationComboBox.Dock = DockStyle.Fill;
        _authenticationComboBox.Items.AddRange(AuthenticationOptions.Select(option => (object)option.Text).ToArray());
        _toolTip.SetToolTip(_authenticationComboBox,
            "Yerleşik: kullanıcılar bu API'de; kayıt, e-posta doğrulama, kilitleme, 2FA ve dönen yenileme token'ları hazır.\n" +
            "Harici: kullanıcılar Entra ID, Auth0, Keycloak gibi bir sağlayıcıda; API yalnızca token doğrular.");
        AddRow(_authenticationComboBox, 3);

        AddRow(CreateSectionLabel("ÖZELLİKLER"), 22);
        AddRow(SetupCheckBox(_sampleCheckBox, "Örnek özellik (TodoItems)",
            "Tüm katmanları uçtan uca gösteren örnek: varlık ve iş kuralları, komut/sorgu, doğrulayıcı, alan olayı, kayıt sahipliği denetimi ve uç noktalar."), 6);
        AddRow(SetupCheckBox(_testsCheckBox, "Test projeleri (birim, mimari, entegrasyon)",
            "xUnit ile birim testleri, katman kurallarını denetleyen mimari testleri ve uygulamayı bellekte çalıştıran güvenlik odaklı entegrasyon testleri."));

        AddRow(CreateSectionLabel("OLUŞTURDUKTAN SONRA"), 20);
        AddRow(SetupCheckBox(_gitCheckBox, "Git deposu başlat (git init)", "Çözüm klasöründe boş bir Git deposu oluşturur; .gitignore hazırdır."), 6);
        AddRow(SetupCheckBox(_buildCheckBox, "Derleyip doğrula (dotnet build)", "Çözümü hemen derler. İlk derlemede NuGet paketleri indirildiği için internet gerekir."));
        AddRow(SetupCheckBox(_openFolderCheckBox, "Bitince klasörü aç", "Oluşturulan çözüm klasörünü Windows Gezgini'nde açar."));

        // Name değerleri UI Automation (ekran okuyucular ve otomasyon testleri) için kimlik olarak kullanılır.
        _nameTextBox.Name = "SolutionNameTextBox";
        _locationTextBox.Name = "LocationTextBox";
        _generateButton.Name = "GenerateButton";

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 84, BackColor = Theme.Surface, Padding = new Padding(0, 10, 0, 0) };
        _generateButton.Text = "Çözümü oluştur";
        _generateButton.Dock = DockStyle.Top;
        _generateButton.Height = 46;
        _generateButton.Font = Theme.ButtonFont;
        _generateButton.ForeColor = Color.White;
        _generateButton.BackColor = Theme.Accent;
        _generateButton.FlatStyle = FlatStyle.Flat;
        _generateButton.FlatAppearance.BorderSize = 0;
        _generateButton.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
        _generateButton.FlatAppearance.MouseDownBackColor = Theme.AccentPressed;
        _generateButton.Cursor = Cursors.Hand;
        _generateButton.UseVisualStyleBackColor = false;
        _targetLabel.Dock = DockStyle.Top;
        _targetLabel.Height = 26;
        _targetLabel.AutoEllipsis = true;
        _targetLabel.ForeColor = Theme.TextMuted;
        _targetLabel.TextAlign = ContentAlignment.MiddleLeft;
        footer.Controls.Add(_targetLabel);
        footer.Controls.Add(_generateButton);

        card.Controls.Add(layout);
        card.Controls.Add(footer);
        return card;
    }

    private Control CreatePreviewCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 14), Margin = Padding.Empty };

        var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 28, ColumnCount = 2, RowCount = 1, BackColor = Theme.Surface };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var previewTitle = CreateSectionLabel("ÖNİZLEME");
        previewTitle.Anchor = AnchorStyles.Left;
        _statsLabel.AutoSize = true;
        _statsLabel.Anchor = AnchorStyles.Right;
        _statsLabel.ForeColor = Theme.TextMuted;
        header.Controls.Add(previewTitle, 0, 0);
        header.Controls.Add(_statsLabel, 1, 0);

        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.Orientation = Orientation.Horizontal;
        _mainSplit.BackColor = Theme.Surface;
        _mainSplit.SplitterWidth = 8;

        _previewSplit.Dock = DockStyle.Fill;
        _previewSplit.SplitterWidth = 6;
        _previewSplit.BackColor = Theme.Border;

        _treeImages.ColorDepth = ColorDepth.Depth32Bit;
        _treeImages.ImageSize = SystemInformation.SmallIconSize;
        _tree.Dock = DockStyle.Fill;
        _tree.BorderStyle = BorderStyle.None;
        _tree.ImageList = _treeImages;
        _tree.HideSelection = false;
        _tree.FullRowSelect = true;
        _tree.ShowLines = false;
        _tree.HotTracking = true;
        _tree.Indent = LogicalToDeviceUnits(18);
        _tree.ItemHeight = LogicalToDeviceUnits(22);
        _previewSplit.Panel1.BackColor = Theme.Surface;
        _previewSplit.Panel1.Padding = new Padding(0, 4, 6, 0);
        _previewSplit.Panel1.Controls.Add(_tree);

        _filePathLabel.Dock = DockStyle.Top;
        _filePathLabel.Height = 26;
        _filePathLabel.TextAlign = ContentAlignment.MiddleLeft;
        _filePathLabel.Padding = new Padding(10, 0, 0, 0);
        _filePathLabel.BackColor = Theme.Header;
        _filePathLabel.ForeColor = Theme.HeaderMuted;
        _filePathLabel.AutoEllipsis = true;
        _codeTextBox.Dock = DockStyle.Fill;
        _codeTextBox.Multiline = true;
        _codeTextBox.ReadOnly = true;
        _codeTextBox.AcceptsReturn = true;
        _codeTextBox.WordWrap = false;
        _codeTextBox.ScrollBars = ScrollBars.Both;
        _codeTextBox.BorderStyle = BorderStyle.None;
        _codeTextBox.Font = Theme.CodeFont;
        _codeTextBox.BackColor = Theme.CodeBackground;
        _codeTextBox.ForeColor = Theme.CodeText;
        _codeTextBox.MaxLength = 0;
        var codePanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CodeBackground, Padding = new Padding(10, 6, 0, 0) };
        codePanel.Controls.Add(_codeTextBox);
        _previewSplit.Panel2.Controls.Add(codePanel);
        _previewSplit.Panel2.Controls.Add(_filePathLabel);
        _mainSplit.Panel1.Controls.Add(_previewSplit);

        var logHeader = new TableLayoutPanel { Dock = DockStyle.Top, Height = 34, ColumnCount = 3, RowCount = 1, BackColor = Theme.Surface };
        logHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        logHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        logHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var logTitle = CreateSectionLabel("İŞLEM GÜNLÜĞÜ");
        logTitle.Anchor = AnchorStyles.Left;
        _openFolderButton.Text = "Klasörü aç";
        _openSolutionButton.Text = "Visual Studio'da aç";
        foreach (var button in new[] { _openFolderButton, _openSolutionButton })
        {
            button.AutoSize = true;
            button.Enabled = false;
            button.Anchor = AnchorStyles.Right;
            button.Margin = new Padding(6, 2, 0, 2);
        }

        logHeader.Controls.Add(logTitle, 0, 0);
        logHeader.Controls.Add(_openFolderButton, 1, 0);
        logHeader.Controls.Add(_openSolutionButton, 2, 0);

        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.ReadOnly = true;
        _logTextBox.BorderStyle = BorderStyle.None;
        _logTextBox.BackColor = Theme.Surface;
        _logTextBox.Font = new Font("Consolas", 9F);
        _logTextBox.DetectUrls = false;
        _logTextBox.HideSelection = false;

        _progressBar.Dock = DockStyle.Bottom;
        _progressBar.Height = 6;
        _progressBar.Visible = false;

        _mainSplit.Panel2.Padding = new Padding(0, 6, 0, 0);
        _mainSplit.Panel2.Controls.Add(_logTextBox);
        _mainSplit.Panel2.Controls.Add(_progressBar);
        _mainSplit.Panel2.Controls.Add(logHeader);

        card.Controls.Add(_mainSplit);
        card.Controls.Add(header);
        return card;
    }

    private Control CreateStatusStrip()
    {
        var statusStrip = new StatusStrip { SizingGrip = true, BackColor = Theme.Surface };
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Text = "Hazır";
        var version = typeof(MainForm).Assembly.GetName().Version;
        statusStrip.Items.Add(_statusLabel);
        statusStrip.Items.Add(new ToolStripStatusLabel($"v{version?.ToString(3)} · .NET 10") { ForeColor = Theme.TextMuted });
        return statusStrip;
    }

    private static Label CreateSectionLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.SectionFont,
        ForeColor = Theme.Accent
    };

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.TextPrimary,
        Margin = Padding.Empty
    };

    private CheckBox SetupCheckBox(CheckBox checkBox, string text, string tooltip)
    {
        checkBox.Text = text;
        checkBox.AutoSize = true;
        checkBox.ForeColor = Theme.TextPrimary;
        checkBox.Cursor = Cursors.Hand;
        _toolTip.SetToolTip(checkBox, tooltip);
        return checkBox;
    }

    private static Icon? LoadApplicationIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Application.ExecutablePath);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException)
        {
            return null;
        }
    }

    // ------------------------------------------------------------------ Olaylar

    private void WireEvents()
    {
        _nameTextBox.TextChanged += (_, _) => SchedulePreview();
        _locationTextBox.TextChanged += (_, _) => UpdateTargetLabel();
        _nameTextBox.KeyDown += OnInputKeyDown;
        _locationTextBox.KeyDown += OnInputKeyDown;
        _databaseComboBox.SelectedIndexChanged += (_, _) => SchedulePreview();
        _solutionFormatComboBox.SelectedIndexChanged += (_, _) => SchedulePreview();
        _authenticationComboBox.SelectedIndexChanged += (_, _) => SchedulePreview();
        _sampleCheckBox.CheckedChanged += (_, _) => SchedulePreview();
        _testsCheckBox.CheckedChanged += (_, _) => SchedulePreview();

        _browseButton.Click += (_, _) => BrowseForLocation();
        _suggestionLink.LinkClicked += (_, _) =>
        {
            if (_suggestionLink.Tag is string suggestion)
            {
                _nameTextBox.Text = suggestion;
                _nameTextBox.SelectionStart = suggestion.Length;
            }
        };

        _generateButton.Click += async (_, _) => await GenerateAsync();
        _generateButton.EnabledChanged += (_, _) => _generateButton.BackColor = _generateButton.Enabled ? Theme.Accent : Theme.AccentDisabled;
        _openFolderButton.Click += (_, _) => OpenInExplorer(_lastResult?.SolutionDirectory);
        _openSolutionButton.Click += (_, _) => OpenWithShell(_lastResult?.SolutionFilePath);

        _tree.AfterSelect += (_, e) => ShowNode(e.Node);
        _tree.AfterExpand += (_, e) => SetFolderImage(e.Node, expanded: true);
        _tree.AfterCollapse += (_, e) => SetFolderImage(e.Node, expanded: false);

        _previewTimer.Tick += (_, _) => RefreshPreview();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            _ = GenerateAsync();
        }
    }

    private void SchedulePreview()
    {
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void BrowseForLocation()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Çözüm klasörünün oluşturulacağı konumu seçin",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            InitialDirectory = Directory.Exists(_locationTextBox.Text) ? _locationTextBox.Text : AppContext.BaseDirectory
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _locationTextBox.Text = dialog.SelectedPath;
        }
    }

    // ------------------------------------------------------------------ Önizleme

    private GeneratorOptions? BuildOptions(out NameValidationResult nameValidation, out string? error)
    {
        var name = _nameTextBox.Text.Trim();
        nameValidation = SolutionNameValidator.Validate(name);
        if (!nameValidation.IsValid)
        {
            error = nameValidation.Error;
            return null;
        }

        var location = _locationTextBox.Text.Trim();
        error = null;
        return new GeneratorOptions
        {
            SolutionName = name,
            OutputDirectory = location.Length > 0 ? location : Environment.CurrentDirectory,
            Database = SelectedDatabase,
            SolutionFormat = SelectedSolutionFormat,
            Authentication = SelectedAuthentication,
            IncludeSampleModule = _sampleCheckBox.Checked,
            IncludeTests = _testsCheckBox.Checked,
            RandomValues = _randomValues
        };
    }

    private void RefreshPreview()
    {
        _previewTimer.Stop();
        var options = BuildOptions(out var nameValidation, out var error);

        _nameHintLabel.ForeColor = nameValidation.IsValid ? Theme.TextMuted : Theme.Error;
        _nameHintLabel.Text = nameValidation.IsValid
            ? "Kök namespace ve klasör adı olur. Örnek: Firma.Proje"
            : nameValidation.Error;
        _suggestionLink.Visible = nameValidation.Suggestion is not null;
        _suggestionLink.Tag = nameValidation.Suggestion;
        _suggestionLink.Text = $"Öneriyi kullan: {nameValidation.Suggestion}";

        if (options is null)
        {
            _plan = null;
            _statsLabel.Text = error ?? string.Empty;
            _statsLabel.ForeColor = Theme.Error;
        }
        else
        {
            try
            {
                _plan = _generator.CreatePlan(options);
                _statsLabel.ForeColor = Theme.TextMuted;
                _statsLabel.Text = $"{_plan.Projects.Count} proje  ·  {_plan.Directories.Count} klasör  ·  {_plan.Files.Count} dosya";
                PopulateTree(_plan);
            }
            catch (Exception exception)
            {
                _plan = null;
                _statsLabel.ForeColor = Theme.Error;
                _statsLabel.Text = "Şablon hatası";
                Log("Şablon işlenemedi: " + exception.Message, LogKind.Error);
            }
        }

        UpdateTargetLabel();
        UpdateGenerateButton();
    }

    private void PopulateTree(GenerationPlan plan)
    {
        // Seçenekler değişip ağaç yeniden çizilince kullanıcının açtığı klasörler açık kalır.
        var expandedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectExpandedFolders(_tree.Nodes, expandedFolders);
        var isFirstPopulation = _tree.Nodes.Count == 0;

        _tree.BeginUpdate();
        try
        {
            _tree.Nodes.Clear();
            var root = new TreeNode(plan.SolutionName) { ImageKey = "folder", SelectedImageKey = "folder" };
            EnsureFolderImages();
            _tree.Nodes.Add(root);

            var folders = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase) { [string.Empty] = root };
            foreach (var directory in plan.Directories)
            {
                var separator = directory.LastIndexOf('/');
                var parent = folders[separator < 0 ? string.Empty : directory[..separator]];
                var node = new TreeNode(directory[(separator + 1)..]) { ImageKey = "folder", SelectedImageKey = "folder", Tag = directory };
                parent.Nodes.Add(node);
                folders[directory] = node;
            }

            TreeNode? nodeToSelect = null;
            foreach (var file in plan.Files)
            {
                var separator = file.RelativePath.LastIndexOf('/');
                var parent = folders[separator < 0 ? string.Empty : file.RelativePath[..separator]];
                var imageKey = GetFileImageKey(file.FileName);
                var node = new TreeNode(file.FileName) { ImageKey = imageKey, SelectedImageKey = imageKey, Tag = file };
                parent.Nodes.Add(node);
                if (string.Equals(file.RelativePath, _selectedFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    nodeToSelect = node;
                }
            }

            root.Expand();
            foreach (var (directory, node) in folders)
            {
                var expand = isFirstPopulation ? directory is "src" or "tests" : expandedFolders.Contains(directory);
                if (expand && directory.Length > 0)
                {
                    node.Expand();
                }
            }

            _tree.SelectedNode = nodeToSelect ?? root;
            _tree.Nodes[0].EnsureVisible();
            nodeToSelect?.EnsureVisible();
        }
        finally
        {
            _tree.EndUpdate();
        }
    }

    private static void CollectExpandedFolders(TreeNodeCollection nodes, HashSet<string> expandedFolders)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.IsExpanded && node.Tag is string directory)
            {
                expandedFolders.Add(directory);
            }

            CollectExpandedFolders(node.Nodes, expandedFolders);
        }
    }

    private void ShowNode(TreeNode? node)
    {
        switch (node?.Tag)
        {
            case PlannedFile file:
                _selectedFilePath = file.RelativePath;
                _filePathLabel.Text = file.RelativePath;
                _codeTextBox.Text = file.Content.Length > 0
                    ? file.Content
                    : "(Boş dosya. Klasörün Git'te tutulması için eklenir; klasöre dosya ekleyince silebilirsiniz.)";
                break;
            case string directory:
                var fileCount = _plan?.Files.Count(file => file.RelativePath.StartsWith(directory + "/", StringComparison.OrdinalIgnoreCase)) ?? 0;
                _filePathLabel.Text = directory + "/";
                _codeTextBox.Text = $"Klasör: {directory}/\r\nİçindeki dosya sayısı: {fileCount}";
                break;
            default:
                _filePathLabel.Text = _plan?.SolutionName + "/";
                _codeTextBox.Text = "Soldaki ağaçtan bir dosya seçerek içeriğini görebilirsiniz.";
                break;
        }

        _codeTextBox.SelectionStart = 0;
        _codeTextBox.ScrollToCaret();
    }

    private void EnsureFolderImages()
    {
        if (_treeImages.Images.ContainsKey("folder"))
        {
            return;
        }

        _treeImages.Images.Add("folder", ShellIcons.GetFolderIcon(open: false) ?? new Bitmap(16, 16));
        _treeImages.Images.Add("folder-open", ShellIcons.GetFolderIcon(open: true) ?? new Bitmap(16, 16));
    }

    private string GetFileImageKey(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var key = "file" + (extension.Length > 0 ? extension.ToLowerInvariant() : fileName.ToLowerInvariant());
        if (!_treeImages.Images.ContainsKey(key))
        {
            _treeImages.Images.Add(key, ShellIcons.GetFileIcon(fileName) ?? new Bitmap(16, 16));
        }

        return key;
    }

    private static void SetFolderImage(TreeNode? node, bool expanded)
    {
        if (node is not null && node.Tag is not PlannedFile)
        {
            node.ImageKey = node.SelectedImageKey = expanded ? "folder-open" : "folder";
        }
    }

    private void UpdateTargetLabel()
    {
        var name = _nameTextBox.Text.Trim();
        var location = _locationTextBox.Text.Trim();
        _targetLabel.Text = name.Length > 0 && location.Length > 0
            ? "Hedef: " + Path.Combine(location, name)
            : "Hedef klasör seçilmedi";
        _toolTip.SetToolTip(_targetLabel, _targetLabel.Text);
    }

    private void UpdateGenerateButton() => _generateButton.Enabled = !_isBusy && _plan is not null;

    // ------------------------------------------------------------------ Oluşturma

    private async Task GenerateAsync()
    {
        RefreshPreview();
        if (_isBusy || _plan is null)
        {
            return;
        }

        var options = BuildOptions(out _, out _);
        var location = _locationTextBox.Text.Trim();
        if (options is null || location.Length == 0 || !Path.IsPathFullyQualified(location))
        {
            MessageBox.Show(this, "Lütfen tam bir klasör yolu seçin (örnek: C:\\Projeler).", "Konum geçersiz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _locationTextBox.Focus();
            return;
        }

        var target = options.SolutionDirectory;
        if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any())
        {
            var answer = MessageBox.Show(this,
                $"\"{target}\" klasörü boş değil.\n\nAynı adlı dosyaların üzerine yazılacak, diğer dosyalara dokunulmayacak. Devam edilsin mi?",
                "Klasör boş değil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            options = options with { OverwriteExisting = true };
        }

        SetBusy(true);
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Log($"\"{options.SolutionName}\" oluşturuluyor: {target}", LogKind.Info);
            if (target.Length > LongPathWarningLength)
            {
                Log("Uyarı: Klasör yolu uzun. Windows'taki 260 karakter sınırı yüzünden derleme ya da çalıştırma sırasında " +
                    "\"dosya adı çok uzun\" hatası alınabilir; daha kısa bir konum (ör. C:\\Projeler) seçmeniz önerilir.", LogKind.Warning);
            }

            var progress = new Progress<GenerationProgress>(value =>
            {
                _progressBar.Maximum = value.Total;
                _progressBar.Value = value.Completed;
                _statusLabel.Text = "Yazılıyor: " + value.CurrentFile;
            });

            var result = await Task.Run(() => _generator.GenerateAsync(options, progress, token), token);
            _lastResult = result;
            _openFolderButton.Enabled = _openSolutionButton.Enabled = true;
            Log($"{result.Plan.Projects.Count} proje ve {result.Plan.Files.Count} dosya yazıldı ({stopwatch.ElapsedMilliseconds} ms).", LogKind.Success);
            if (result.UserSecretsFilePath is not null)
            {
                Log("Geliştirme gizli değerleri (JWT anahtarı vb.) depo dışına, user-secrets'a yazıldı: " + result.UserSecretsFilePath, LogKind.Detail);
            }

            if (_gitCheckBox.Checked)
            {
                Log("Git deposu başlatılıyor…");
                var initialized = await PostGenerationActions.InitializeGitRepositoryAsync(result.SolutionDirectory, LogProcessOutput, token);
                Log(initialized ? "Git deposu hazır." : "Git deposu başlatılamadı (git kurulu ve PATH'te mi?).", initialized ? LogKind.Success : LogKind.Warning);
            }

            if (_buildCheckBox.Checked)
            {
                Log("dotnet build çalışıyor; ilk derlemede NuGet paketleri indirilir…");
                _statusLabel.Text = "Derleniyor…";
                _progressBar.Style = ProgressBarStyle.Marquee;
                var built = await PostGenerationActions.BuildSolutionAsync(result.SolutionFilePath, LogProcessOutput, token);
                _progressBar.Style = ProgressBarStyle.Continuous;
                Log(built ? "Derleme başarılı." : "Derleme başarısız; ayrıntılar yukarıda.", built ? LogKind.Success : LogKind.Error);
            }

            Log($"Tamamlandı ({stopwatch.Elapsed.TotalSeconds:0.0} sn): {result.SolutionFilePath}", LogKind.Success);
            _statusLabel.Text = "Tamamlandı: " + result.SolutionDirectory;

            if (_openFolderCheckBox.Checked)
            {
                OpenInExplorer(result.SolutionDirectory);
            }

            // Bir sonraki çözüm kendi JWT anahtarını, user-secrets kimliğini ve portlarını alır.
            _randomValues = ProjectRandomValues.Create();
            SaveSettings();
        }
        catch (OperationCanceledException)
        {
            Log("İşlem iptal edildi.", LogKind.Warning);
            _statusLabel.Text = "İptal edildi";
        }
        catch (Exception exception)
        {
            Log("Hata: " + exception.Message, LogKind.Error);
            _statusLabel.Text = "Hata oluştu";
            MessageBox.Show(this, exception.Message, "Çözüm oluşturulamadı", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _cancellation?.Dispose();
            _cancellation = null;
            SetBusy(false);
            RefreshPreview();
        }
    }

    /// <summary>Süreç çıktısı arka plan iş parçacığından gelir; sıranın korunması için senkron aktarılır.</summary>
    private void LogProcessOutput(string line)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        try
        {
            Invoke(() => Log("   " + line, LogKind.Detail));
        }
        catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException or InvalidAsynchronousStateException)
        {
            // Pencere kapanırken gelen satırlar yok sayılır.
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        UseWaitCursor = busy;
        _progressBar.Visible = busy;
        _progressBar.Value = 0;
        _generateButton.Text = busy ? "Oluşturuluyor…" : "Çözümü oluştur";

        foreach (var control in new Control[]
                 {
                     _nameTextBox, _locationTextBox, _browseButton, _databaseComboBox, _solutionFormatComboBox, _authenticationComboBox,
                     _sampleCheckBox, _testsCheckBox, _gitCheckBox, _buildCheckBox, _openFolderCheckBox
                 })
        {
            control.Enabled = !busy;
        }

        UpdateGenerateButton();
    }

    private void Log(string message, LogKind kind = LogKind.Info)
    {
        var color = kind switch
        {
            LogKind.Success => Theme.Success,
            LogKind.Warning => Theme.Warning,
            LogKind.Error => Theme.Error,
            LogKind.Detail => Theme.TextMuted,
            _ => Theme.TextPrimary
        };

        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.SelectionLength = 0;
        _logTextBox.SelectionColor = Theme.TextMuted;
        _logTextBox.AppendText($"{DateTime.Now:HH:mm:ss}  ");
        _logTextBox.SelectionColor = color;
        _logTextBox.AppendText(message + Environment.NewLine);
        _logTextBox.ScrollToCaret();
    }

    private void OpenInExplorer(string? directory)
    {
        if (directory is not null && Directory.Exists(directory))
        {
            Process.Start("explorer.exe", $"\"{directory}\"");
        }
    }

    private void OpenWithShell(string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            Log($"Açılamadı ({target}): {exception.Message}", LogKind.Warning);
        }
    }

    // ------------------------------------------------------------------ Ayarlar

    private void LoadSettings()
    {
        _nameTextBox.Text = _settings.SolutionName;
        _locationTextBox.Text = _settings.OutputDirectory;
        _databaseComboBox.SelectedIndex = Math.Max(0, Array.FindIndex(DatabaseOptions, option => option.Value == _settings.Database));
        _solutionFormatComboBox.SelectedIndex = Math.Max(0, Array.FindIndex(SolutionFormatOptions, option => option.Value == _settings.SolutionFormat));
        _authenticationComboBox.SelectedIndex = Math.Max(0, Array.FindIndex(AuthenticationOptions, option => option.Value == _settings.Authentication));
        _sampleCheckBox.Checked = _settings.IncludeSampleModule;
        _testsCheckBox.Checked = _settings.IncludeTests;
        _gitCheckBox.Checked = _settings.InitializeGit;
        _buildCheckBox.Checked = _settings.BuildAfterGeneration;
        _openFolderCheckBox.Checked = _settings.OpenFolderAfterGeneration;
        UpdateTargetLabel();
    }

    private void SaveSettings()
    {
        _settings.SolutionName = _nameTextBox.Text.Trim();
        _settings.OutputDirectory = _locationTextBox.Text.Trim();
        _settings.Database = SelectedDatabase;
        _settings.SolutionFormat = SelectedSolutionFormat;
        _settings.Authentication = SelectedAuthentication;
        _settings.IncludeSampleModule = _sampleCheckBox.Checked;
        _settings.IncludeTests = _testsCheckBox.Checked;
        _settings.InitializeGit = _gitCheckBox.Checked;
        _settings.BuildAfterGeneration = _buildCheckBox.Checked;
        _settings.OpenFolderAfterGeneration = _openFolderCheckBox.Checked;
        _settings.Save();
    }
}
