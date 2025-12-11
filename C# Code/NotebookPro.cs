using GemBox.Document;
using GemBox.Document.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Xps.Packaging;
using GuiLabs.Undo;
using DrawingColor = System.Drawing.Color;

namespace NotebookPro
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitializeSearchPanel();
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            string licensePath = Path.Combine(Application.StartupPath, "source/gembox_license.txt");
            string licenseKey = "FREE-LIMITED-KEY";

            if (File.Exists(licensePath))
            {
                try
                {
                    licenseKey = File.ReadAllText(licensePath).Trim();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading license file:\n" + ex.Message,
                        "License Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            ComponentInfo.SetLicense(licenseKey);

            InitializeNotebook();
            InitializeToolbox();
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        private ToolStrip translationToolStrip;
        private ToolStripComboBox cbFromLang;
        private ToolStripComboBox cbToLang;
        private ToolStripButton btnTranslate;
        private ToolStripButton btnNewTab;
        private ToolStripButton btnSaveTab;
        private ToolStripButton btnLoadTab;
        private ToolStripButton BtnComment;

        string iconPath1 = Path.Combine(Application.StartupPath, "icons/font.png");
        string iconPath2 = Path.Combine(Application.StartupPath, "icons/translate.png");
        string iconPath3 = Path.Combine(Application.StartupPath, "icons/paint.png");
        string iconPath4 = Path.Combine(Application.StartupPath, "icons/statistics.png");
        string iconPath5 = Path.Combine(Application.StartupPath, "icons/new_tab.png");
        string iconPath6 = Path.Combine(Application.StartupPath, "icons/save_tab.png");
        string iconPath7 = Path.Combine(Application.StartupPath, "icons/load_tab.png");
        string iconPath8 = Path.Combine(Application.StartupPath, "icons/help.png");
        string iconPath9 = Path.Combine(Application.StartupPath, "icons/undo.png");
        string iconPath10 = Path.Combine(Application.StartupPath, "icons/redo.png");
        string iconPath11 = Path.Combine(Application.StartupPath, "icons/search.png");

        private static readonly HttpClient httpClient = new HttpClient();

        private ToolStripComboBox cbFont, cbFontSize, cbFontStyle;
        private ToolStripButton btnFontColor;

        private readonly Dictionary<string, string> languages = new Dictionary<string, string>()
        {
            {"auto", "Auto Detect"},
            {"en", "English"},
            {"es", "Spanish"},
            {"fr", "French"},
            {"de", "German"},
            {"ru", "Russian"},
            {"pt", "Portuguese"},
            {"pt-BR", "Portuguese (Brazil)"},
            {"it", "Italian"},
            {"nl", "Dutch"},
            {"pl", "Polish"},
            {"tr", "Turkish"},
            {"zh-CN", "Chinese (Simplified)"},
            {"zh-TW", "Chinese (Traditional)"},
            {"ja", "Japanese"},
            {"ko", "Korean"},
            {"ar", "Arabic"},
            {"hi", "Hindi"},
            {"bn", "Bengali"},
            {"pa", "Punjabi"},
            {"ur", "Urdu"},
            {"vi", "Vietnamese"},
            {"th", "Thai"},
            {"id", "Indonesian"},
            {"ms", "Malay"},
            {"sv", "Swedish"},
            {"no", "Norwegian"},
            {"fi", "Finnish"},
            {"da", "Danish"},
            {"cs", "Czech"},
            {"sk", "Slovak"},
            {"hu", "Hungarian"},
            {"el", "Greek"},
            {"he", "Hebrew"},
            {"ro", "Romanian"},
            {"bg", "Bulgarian"},
            {"sr", "Serbian"},
            {"hr", "Croatian"},
            {"lt", "Lithuanian"},
            {"lv", "Latvian"},
            {"et", "Estonian"},
            {"sl", "Slovenian"},
            {"mk", "Macedonian"},
            {"af", "Afrikaans"},
            {"sw", "Swahili"}
        };

        private void MarkTabAsDirty(TabPage tab, bool isDirty)
        {
            if (tab == null) return;

            tab.Tag = isDirty;

            if (isDirty && !tab.Text.EndsWith("*"))
                tab.Text += "*";
            else if (!isDirty && tab.Text.EndsWith("*"))
                tab.Text = tab.Text.TrimEnd('*');
        }

        private bool IsTabDirty(TabPage tab)
        {
            if (tab?.Tag is bool dirty)
                return dirty;
            return false;
        }

        public void LoadFileOnStartup(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                switch (ext)
                {
                    case ".txt":
                    case ".npd":
                    case ".md":
                    case ".cs":
                    case ".c":
                    case ".cpp":
                    case ".h":
                    case ".py":
                    case ".lua":
                    case ".java":
                    case ".js":
                    case ".ts":
                    case ".php":
                    case ".go":
                    case ".rs":
                    case ".json":
                    case ".xml":
                    case ".yml":
                    case ".yaml":
                        string textContent = File.ReadAllText(filePath);
                        AddNewTabFromContent(Path.GetFileNameWithoutExtension(filePath), textContent);
                        break;

                    case ".html":
                    case ".htm":
                        string htmlContent = File.ReadAllText(filePath);
                        AddNewTabWithBrowser(Path.GetFileNameWithoutExtension(filePath), htmlContent);
                        break;

                    case ".rtf":
                        string rtf = File.ReadAllText(filePath);
                        AddNewTabFromRtf(Path.GetFileNameWithoutExtension(filePath), rtf);
                        break;

                    case ".docx":
                    case ".doc":
                        var docLoad = DocumentModel.Load(filePath);
                        using (var ms = new MemoryStream())
                        {
                            docLoad.Save(ms, SaveOptions.RtfDefault);
                            ms.Position = 0;
                            using (var reader = new StreamReader(ms))
                            {
                                string rtfDoc = reader.ReadToEnd();
                                AddNewTabFromRtf(Path.GetFileNameWithoutExtension(filePath), rtfDoc);
                            }
                        }
                        break;

                    default:
                        string fallback = File.ReadAllText(filePath);
                        AddNewTabFromContent(Path.GetFileNameWithoutExtension(filePath), fallback);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeToolbox()
        {
            translationToolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                ImageScalingSize = new System.Drawing.Size(32, 32)
            };

            ToolStripButton btnUndo = new ToolStripButton("Undo")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath9)
            };
            btnUndo.Click += (s, e) => UndoInCurrentTab();

            ToolStripButton btnRedo = new ToolStripButton("Redo")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath10)
            };
            btnRedo.Click += (s, e) => RedoInCurrentTab();

            ToolStripButton btnSearch = new ToolStripButton("Search")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath11)
            };
            btnSearch.Click += (s, e) =>
            {
                InitializeSearchPanel();
                searchPanel.Visible = !searchPanel.Visible;
                if (searchPanel.Visible)
                {
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                }
            };

            ToolStripLabel spacer = new ToolStripLabel()
            {
                AutoSize = false,
                Width = 10
            };

            ToolStripDropDownButton translateDrop = new ToolStripDropDownButton("Translate")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath2)
            };

            btnNewTab = new ToolStripButton("New Tab")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath5)
            };
            btnNewTab.Click += BtnNewTab_Click;

            btnSaveTab = new ToolStripButton("Save Tab")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath6)
            };
            btnSaveTab.Click += BtnSaveTab_Click;

            btnLoadTab = new ToolStripButton("Load Tab")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath7)
            };
            btnLoadTab.Click += BtnLoadTab_Click;

            ToolStripMenuItem fromLangItem = new ToolStripMenuItem("From Language");
            cbFromLang = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
            foreach (var kv in languages)
                cbFromLang.Items.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
            cbFromLang.ComboBox.DisplayMember = "Value";
            cbFromLang.ComboBox.ValueMember = "Key";
            fromLangItem.DropDownItems.Add(cbFromLang);

            ToolStripMenuItem toLangItem = new ToolStripMenuItem("To Language");
            cbToLang = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
            foreach (var kv in languages)
                cbToLang.Items.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
            cbToLang.ComboBox.DisplayMember = "Value";
            cbToLang.ComboBox.ValueMember = "Key";
            toLangItem.DropDownItems.Add(cbToLang);

            btnTranslate = new ToolStripButton("Translate");
            btnTranslate.Click += async (s, e) => await BtnTranslate_Click(s, e);

            translateDrop.DropDownItems.Add(fromLangItem);
            translateDrop.DropDownItems.Add(toLangItem);
            translateDrop.DropDownItems.Add(new ToolStripSeparator());
            translateDrop.DropDownItems.Add(btnTranslate);

            ToolStripDropDownButton textDrop = new ToolStripDropDownButton("Text")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath1)
            };

            ToolStripMenuItem fontItem = new ToolStripMenuItem("Font Family");
            cbFont = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            foreach (FontFamily f in FontFamily.Families)
                cbFont.Items.Add(f.Name);
            cbFont.SelectedItem = "Segoe UI";
            fontItem.DropDownItems.Add(cbFont);

            ToolStripMenuItem sizeItem = new ToolStripMenuItem("Font Size");
            cbFontSize = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60 };
            cbFontSize.Items.AddRange(new string[]
            { "8", "9", "10", "11", "12", "14", "16", "18", "20", "22", "24", "28", "32", "36", "48", "72" });
            cbFontSize.SelectedItem = "12";
            sizeItem.DropDownItems.Add(cbFontSize);

            ToolStripMenuItem styleItem = new ToolStripMenuItem("Font Style");
            cbFontStyle = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cbFontStyle.Items.AddRange(new string[] { "Regular", "Bold", "Italic", "Bold+Italic" });
            cbFontStyle.SelectedItem = "Regular";
            styleItem.DropDownItems.Add(cbFontStyle);

            ToolStripMenuItem colorItem = new ToolStripMenuItem("Font Color");
            btnFontColor = new ToolStripButton("Pick Color")
            {
                AutoSize = false,
                Width = 100
            };
            colorItem.DropDownItems.Add(btnFontColor);
            textDrop.DropDownItems.Add(fontItem);
            textDrop.DropDownItems.Add(sizeItem);
            textDrop.DropDownItems.Add(styleItem);
            textDrop.DropDownItems.Add(colorItem);

            ToolStripDropDownButton paintDrop = new ToolStripDropDownButton("Paint")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath3)
            };

            ToolStripMenuItem newDrawingItem = new ToolStripMenuItem("Create New Drawing");
            newDrawingItem.Click += (s, e) => OpenPaintForm(null);

            ToolStripMenuItem uploadImageItem = new ToolStripMenuItem("Upload Image");
            uploadImageItem.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog()
                {
                    Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    Title = "Select an image to mark"
                })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        Bitmap uploaded = new Bitmap(ofd.FileName);
                        OpenPaintForm(uploaded);
                    }
                }
            };

            paintDrop.DropDownItems.Add(newDrawingItem);
            paintDrop.DropDownItems.Add(uploadImageItem);

            ToolStripButton btnHelp = new ToolStripButton("Help")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath8)
            };

            ToolStripButton btnStats = new ToolStripButton("Statistics")
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                Image = Image.FromFile(iconPath4)
            };
            btnStats.Click += (s, e) => ShowTextStatistics();

            btnHelp.Click += (s, e) =>
            {
                MessageBox.Show(
                    "NotebookPro Help:\n\n" +
                    "- New Tab: Create a new tab\n" +
                    "- Save/Load: Save or open files\n" +
                    "- Undo/Redo: Save or open files\n" +
                    "- Text: Change font family, size, style, color\n" +
                    "- Paint: Draw or upload image\n" +
                    "- Translate: Translate text\n" +
                    "- Statistics: View document statistics\n" +
                    "- Help: Open this help window",
                    "Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Question
                );
            };

            translationToolStrip.Items.Add(spacer);
            translationToolStrip.Items.Add(btnNewTab);
            translationToolStrip.Items.Add(new ToolStripSeparator());
            translationToolStrip.Items.Add(btnSaveTab);
            translationToolStrip.Items.Add(btnLoadTab);
            translationToolStrip.Items.Add(new ToolStripSeparator());
            translationToolStrip.Items.Add(btnUndo);
            translationToolStrip.Items.Add(btnRedo);
            translationToolStrip.Items.Add(new ToolStripSeparator());
            translationToolStrip.Items.Add(textDrop);
            translationToolStrip.Items.Add(paintDrop);
            translationToolStrip.Items.Add(translateDrop);
            translationToolStrip.Items.Add(new ToolStripSeparator());
            translationToolStrip.Items.Add(btnSearch);
            translationToolStrip.Items.Add(btnStats);
            translationToolStrip.Items.Add(btnHelp);

            this.Controls.Add(translationToolStrip);
            translationToolStrip.BringToFront();

            HookTextFormattingEvents();
        }

        private void ShowTextStatistics()
        {
            RichTextBox rtb = GetCurrentRichTextBox();
            if (rtb == null) return;

            string text = rtb.Text;

            int totalCharsWithSpaces = text.Length;
            int totalCharsNoSpaces = text.Count(c => !char.IsWhiteSpace(c));
            int totalWords = string.IsNullOrWhiteSpace(text) ? 0 :
                             text.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            int totalLetters = text.Count(char.IsLetter);
            int totalDigits = text.Count(char.IsDigit);

            MessageBox.Show(
                $"Characters (with spaces): {totalCharsWithSpaces}\n" +
                $"Characters (no spaces): {totalCharsNoSpaces}\n" +
                $"Words: {totalWords}\n" +
                $"Letters: {totalLetters}\n" +
                $"Digits: {totalDigits}",
                "Statistics",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void OpenPaintForm(System.Drawing.Bitmap baseImage)
        {
            using (Form paintForm = new Form())
            {
                paintForm.Text = "Draw Image";
                paintForm.Width = 1000;
                paintForm.Height = 700;
                paintForm.StartPosition = FormStartPosition.CenterParent;

                System.Drawing.Color currentColor = System.Drawing.Color.Black;
                int brushSize = 6;
                bool drawing = false;
                System.Drawing.Point lastPoint = System.Drawing.Point.Empty;

                Panel scrollPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = System.Drawing.Color.DarkGray
                };
                paintForm.Controls.Add(scrollPanel);

                PictureBox canvas = new PictureBox
                {
                    BackColor = System.Drawing.Color.White,
                    SizeMode = PictureBoxSizeMode.Normal,
                    Location = new System.Drawing.Point(0, 0)
                };
                scrollPanel.Controls.Add(canvas);

                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(canvas, true, null);

                System.Drawing.Bitmap baseLayer;

                if (baseImage != null)
                {
                    baseLayer = new System.Drawing.Bitmap(baseImage);
                }
                else
                {
                    using (Form sizeForm = new Form())
                    {
                        sizeForm.Text = "New Canvas Size";
                        sizeForm.Width = 300;
                        sizeForm.Height = 150;
                        sizeForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                        sizeForm.StartPosition = FormStartPosition.CenterParent;
                        sizeForm.MaximizeBox = false;
                        sizeForm.MinimizeBox = false;

                        Label lblW = new Label { Text = "Width (px):", Left = 10, Top = 20, Width = 80 };
                        Label lblH = new Label { Text = "Height (px):", Left = 10, Top = 50, Width = 80 };
                        NumericUpDown numW = new NumericUpDown { Left = 100, Top = 18, Width = 120, Minimum = 100, Maximum = 10000, Value = 800 };
                        NumericUpDown numH = new NumericUpDown { Left = 100, Top = 48, Width = 120, Minimum = 100, Maximum = 10000, Value = 600 };

                        Button btnOk = new Button { Text = "OK", Left = 70, Width = 60, Top = 80, DialogResult = DialogResult.OK };
                        Button btnCancel = new Button { Text = "Cancel", Left = 150, Width = 60, Top = 80, DialogResult = DialogResult.Cancel };

                        sizeForm.Controls.Add(lblW);
                        sizeForm.Controls.Add(lblH);
                        sizeForm.Controls.Add(numW);
                        sizeForm.Controls.Add(numH);
                        sizeForm.Controls.Add(btnOk);
                        sizeForm.Controls.Add(btnCancel);
                        sizeForm.AcceptButton = btnOk;
                        sizeForm.CancelButton = btnCancel;

                        if (sizeForm.ShowDialog() == DialogResult.OK)
                        {
                            baseLayer = new System.Drawing.Bitmap((int)numW.Value, (int)numH.Value);
                        }
                        else
                        {
                            return;
                        }
                    }

                    using (var g = System.Drawing.Graphics.FromImage(baseLayer))
                        g.Clear(System.Drawing.Color.White);
                }

                System.Drawing.Bitmap drawingLayer = new System.Drawing.Bitmap(baseLayer.Width, baseLayer.Height);
                using (var g = System.Drawing.Graphics.FromImage(drawingLayer))
                    g.Clear(System.Drawing.Color.Transparent);

                canvas.Size = new System.Drawing.Size(baseLayer.Width, baseLayer.Height);
                scrollPanel.AutoScrollMinSize = canvas.Size;

                ToolStrip toolStrip = new ToolStrip
                {
                    GripStyle = ToolStripGripStyle.Hidden,
                    ImageScalingSize = new System.Drawing.Size(24, 24),
                    Dock = DockStyle.Top
                };
                paintForm.Controls.Add(toolStrip);

                ToolStripLabel lblBrush = new ToolStripLabel("Brush:");
                ToolStripComboBox cbBrush = new ToolStripComboBox { Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
                cbBrush.Items.AddRange(new object[] { "2", "4", "6", "8", "10", "12", "16", "20", "30", "40", "50" });
                cbBrush.SelectedItem = "6";
                cbBrush.SelectedIndexChanged += (s, e) =>
                {
                    brushSize = int.Parse(cbBrush.SelectedItem.ToString());
                };

                ToolStripButton btnColor = new ToolStripButton("Color") { DisplayStyle = ToolStripItemDisplayStyle.Text };
                Panel colorSwatch = new Panel
                {
                    Width = 22,
                    Height = 16,
                    BackColor = currentColor,
                    BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };

                void PickColor()
                {
                    using (ColorDialog cd = new ColorDialog())
                    {
                        cd.Color = currentColor;
                        if (cd.ShowDialog() == DialogResult.OK)
                        {
                            currentColor = cd.Color;
                            colorSwatch.BackColor = currentColor;
                        }
                    }
                }
                btnColor.Click += (s, e) => PickColor();
                colorSwatch.Click += (s, e) => PickColor();

                ToolStripButton btnClear = new ToolStripButton("Clear") { DisplayStyle = ToolStripItemDisplayStyle.Text };
                btnClear.Click += (s, e) =>
                {
                    using (var g = System.Drawing.Graphics.FromImage(drawingLayer))
                        g.Clear(System.Drawing.Color.Transparent);

                    canvas.Invalidate();
                };

                ToolStripButton btnSave = new ToolStripButton("Save As...") { DisplayStyle = ToolStripItemDisplayStyle.Text };
                btnSave.Click += (s, e) =>
                {
                    using (var composite = new System.Drawing.Bitmap(baseLayer.Width, baseLayer.Height))
                    using (var g = System.Drawing.Graphics.FromImage(composite))
                    {
                        g.DrawImageUnscaled(baseLayer, 0, 0);
                        g.DrawImageUnscaled(drawingLayer, 0, 0);

                        using (SaveFileDialog sfd = new SaveFileDialog
                        {
                            Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap|*.bmp",
                            FileName = "Untitled.png"
                        })
                        {
                            if (sfd.ShowDialog() == DialogResult.OK)
                            {
                                var format = System.Drawing.Imaging.ImageFormat.Png;
                                string ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                                if (ext == ".jpg" || ext == ".jpeg") format = System.Drawing.Imaging.ImageFormat.Jpeg;
                                if (ext == ".bmp") format = System.Drawing.Imaging.ImageFormat.Bmp;

                                composite.Save(sfd.FileName, format);
                            }
                        }
                    }
                };

                ToolStripButton btnDone = new ToolStripButton("Insert") { DisplayStyle = ToolStripItemDisplayStyle.Text };
                btnDone.Click += (s, e) =>
                {
                    using (var composite = new System.Drawing.Bitmap(baseLayer.Width, baseLayer.Height))
                    using (var g = System.Drawing.Graphics.FromImage(composite))
                    {
                        g.DrawImageUnscaled(baseLayer, 0, 0);
                        g.DrawImageUnscaled(drawingLayer, 0, 0);

                        RichTextBox rtb = GetCurrentRichTextBox();
                        if (rtb != null)
                        {
                            Clipboard.SetImage(composite);
                            rtb.Paste();
                        }
                    }
                    paintForm.Close();
                };

                toolStrip.Items.Add(lblBrush);
                toolStrip.Items.Add(cbBrush);
                toolStrip.Items.Add(new ToolStripSeparator());
                toolStrip.Items.Add(btnColor);
                toolStrip.Items.Add(new ToolStripSeparator());
                toolStrip.Items.Add(btnClear);
                toolStrip.Items.Add(new ToolStripSeparator());
                toolStrip.Items.Add(btnSave);
                toolStrip.Items.Add(new ToolStripSeparator());
                toolStrip.Items.Add(btnDone);

                canvas.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.DrawImageUnscaled(baseLayer, 0, 0);
                    e.Graphics.DrawImageUnscaled(drawingLayer, 0, 0);
                };

                canvas.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        drawing = true;
                        lastPoint = e.Location;
                    }
                };

                canvas.MouseMove += (s, e) =>
                {
                    if (!drawing) return;

                    using (var g = System.Drawing.Graphics.FromImage(drawingLayer))
                    using (var pen = new System.Drawing.Pen(currentColor, brushSize)
                    {
                        StartCap = System.Drawing.Drawing2D.LineCap.Round,
                        EndCap = System.Drawing.Drawing2D.LineCap.Round
                    })
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.DrawLine(pen, lastPoint, e.Location);
                    }
                    lastPoint = e.Location;
                    canvas.Invalidate();
                };

                canvas.MouseUp += (s, e) => drawing = false;

                paintForm.ShowDialog();

                drawingLayer.Dispose();
                baseLayer.Dispose();
            }
        }

        private void RedrawComposite(PictureBox canvas, System.Drawing.Bitmap baseLayer, System.Drawing.Bitmap drawingLayer)
        {
            System.Drawing.Bitmap composite = new System.Drawing.Bitmap(baseLayer.Width, baseLayer.Height);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(composite))
            {
                g.DrawImage(baseLayer, 0, 0);
                g.DrawImage(drawingLayer, 0, 0);
            }
            canvas.Image = composite;
        }



        private void HookTextFormattingEvents()
        {
            cbFont.SelectedIndexChanged += (s, e) =>
            {
                RichTextBox rtb = GetCurrentRichTextBox();
                if (rtb != null && cbFont.SelectedItem != null)
                {
                    var currentFont = rtb.SelectionFont ?? rtb.Font;
                    rtb.SelectionFont = new Font(cbFont.SelectedItem.ToString(), currentFont.Size, currentFont.Style);
                }
            };

            cbFontSize.SelectedIndexChanged += (s, e) =>
            {
                RichTextBox rtb = GetCurrentRichTextBox();
                if (rtb != null && cbFontSize.SelectedItem != null && float.TryParse(cbFontSize.SelectedItem.ToString(), out float newSize))
                {
                    var currentFont = rtb.SelectionFont ?? rtb.Font;
                    rtb.SelectionFont = new Font(currentFont.FontFamily, newSize, currentFont.Style);
                }
            };

            cbFontStyle.SelectedIndexChanged += (s, e) =>
            {
                RichTextBox rtb = GetCurrentRichTextBox();
                if (rtb != null && cbFontStyle.SelectedItem != null)
                {
                    var currentFont = rtb.SelectionFont ?? rtb.Font;
                    rtb.SelectionFont = new Font(currentFont.FontFamily, currentFont.Size, GetSelectedFontStyle(cbFontStyle.SelectedItem.ToString()));
                }
            };

            btnFontColor.Click += (s, e) =>
            {
                RichTextBox rtb = GetCurrentRichTextBox();
                if (rtb != null)
                {
                    using ColorDialog cd = new ColorDialog();
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        rtb.SelectionColor = cd.Color;
                        btnFontColor.BackColor = cd.Color;
                    }
                }
            };
        }

        private FontStyle GetSelectedFontStyle(string style)
        {
            return style switch
            {
                "Bold" => FontStyle.Bold,
                "Italic" => FontStyle.Italic,
                "Bold+Italic" => FontStyle.Bold | FontStyle.Italic,
                _ => FontStyle.Regular
            };
        }

        private void HookRichTextBoxEvents(RichTextBox rtb)
        {
            rtb.SelectionChanged += (s, e) =>
            {
                if (rtb.SelectionFont != null)
                {
                    cbFont.SelectedItem = rtb.SelectionFont.FontFamily.Name;
                    cbFontSize.SelectedItem = rtb.SelectionFont.Size.ToString();
                    cbFontStyle.SelectedItem = rtb.SelectionFont.Style.ToString();
                }
                btnFontColor.BackColor = rtb.SelectionColor;
            };
        }


        private async Task BtnTranslate_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == null) return;

            Panel panel = tabControl1.SelectedTab.Controls.OfType<Panel>().FirstOrDefault();
            if (panel == null) return;
            RichTextBox rtbMain = panel.Controls.OfType<RichTextBox>().FirstOrDefault();
            if (rtbMain == null) return;

            string original;
            bool useSelection = !string.IsNullOrEmpty(rtbMain.SelectedText);
            original = useSelection ? rtbMain.SelectedText : rtbMain.Text;

            if (string.IsNullOrWhiteSpace(original))
            {
                MessageBox.Show("Nothing to translate.", "Translate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (cbFromLang.ComboBox.SelectedItem == null || cbToLang.ComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select source and target languages.", "Translate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fromCode = ((KeyValuePair<string, string>)cbFromLang.ComboBox.SelectedItem).Key;
            string toCode = ((KeyValuePair<string, string>)cbToLang.ComboBox.SelectedItem).Key;

            if (toCode == "auto")
            {
                MessageBox.Show("Please select a target language (not Auto Detect).", "Translate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (fromCode == toCode && fromCode != "auto")
            {
                MessageBox.Show("Source and target languages are the same.", "Translate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                btnTranslate.Enabled = false;
                btnTranslate.Text = "Translating...";

                string translated = await TranslateTextAsync(original, fromCode, toCode);

                if (translated == null)
                {
                    MessageBox.Show("Translation failed (no response).", "Translate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (useSelection)
                {
                    int selStart = rtbMain.SelectionStart;
                    rtbMain.SelectedText = translated;
                    rtbMain.SelectionStart = selStart + translated.Length;
                }
                else
                {
                    rtbMain.Text = translated;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Translation error:\n" + ex.Message, "Translate", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTranslate.Enabled = true;
                btnTranslate.Text = "Translate";
            }
        }

        private Control GetCurrentEditor()
        {
            if (tabControl1.SelectedTab?.Controls.Count == 0)
                return null;

            var tab = tabControl1.SelectedTab;

            if (tab.Controls[0] is Panel panel && panel.Controls.Count > 0)
                return panel.Controls[0];

            return tab.Controls[0];
        }

        private void AttachDirtyTracking(TabPage tab)
        {
            if (tab.Controls.Count > 0 && tab.Controls[0] is RichTextBox editor)
            {
                editor.TextChanged += (s, e) => MarkTabAsDirty(tab, true);
            }
        }

        private async Task<string> TranslateTextAsync(string text, string source, string target)
        {
            var url = "https://libretranslate.com/translate";
            var payload = new
            {
                q = text,
                source = source,
                target = target,
                format = "text",
                api_key = LoadTranslateApiKey()
            };

            string json = JsonSerializer.Serialize(payload);

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var resp = await httpClient.PostAsync(url, content))
            {
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync();
                    throw new Exception($"Translate API failed: {resp.StatusCode}: {err}");
                }

                var respString = await resp.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(respString))
                {
                    if (doc.RootElement.TryGetProperty("translatedText", out var el))
                        return el.GetString();
                }
            }

            return null;
        }

        private string LoadTranslateApiKey()
        {
            string path = Path.Combine(Application.StartupPath, "source/translate_api_key.txt");
            if (File.Exists(path))
            {
                try
                {
                    return File.ReadAllText(path).Trim();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading translate API key file:\n" + ex.Message,
                        "Translate Key Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            return null;
        }
        private void InitializeNotebook()
        {
            ContextMenuStrip tabContextMenu = new ContextMenuStrip();
            tabContextMenu.Items.Add("Close Tab", null, (s, e) => CloseSelectedTab());
            tabContextMenu.Items.Add("Rename Tab", null, (s, e) => RenameSelectedTab());
            tabContextMenu.Items.Add("Tab Properties", null, (s, e) => OpenProperties());
            tabControl1.ContextMenuStrip = tabContextMenu;
            tabControl1.MouseUp += TabControl1_MouseUp;

            AddNewTab("Untitled 1");
        }
        private RichTextBox GetCurrentRichTextBox()
        {
            if (tabControl1?.SelectedTab == null) return null;
            return FindControlRecursive<RichTextBox>(tabControl1.SelectedTab);
        }

        private void AddNewTab(string title)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill };

            RichTextBox rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                WordWrap = false,
                Font = new Font("Segoe UI", 12),
                BackColor = System.Drawing.Color.White,
                ForeColor = System.Drawing.Color.Black
            };

            panel.Controls.Add(rtb);

            TabPage tab = new TabPage(title);
            tab.Controls.Add(panel);

            rtb.TextChanged += (s, e) => MarkTabAsDirty(tab, true);

            tabControl1.TabPages.Add(tab);
            tabControl1.SelectedTab = tab;

            panel.Controls.Add(rtb);
        }

        private string lastSearchQuery = "";
        private int lastSearchIndex = 0;
        private Panel searchPanel;
        private System.Windows.Forms.TextBox txtSearch;
        private Button btnCloseSearch;
        private Button btnNext;
        private Button btnPrev;
        private string lastSearchTerm = "";
        private int lastFoundIndex = 0;

        private void InitializeSearchPanel()
        {
            if (searchPanel != null) return;

            searchPanel = new Panel
            {
                Height = 34,
                Dock = DockStyle.Bottom,
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240),
                Visible = false
            };

            Label lbl = new Label
            {
                Text = "Search:",
                Left = 8,
                Top = 8,
                AutoSize = true
            };

            txtSearch = new System.Windows.Forms.TextBox
            {
                Left = 64,
                Top = 6,
                Width = 300,
                PlaceholderText = "Type to search..."
            };

            txtSearch.TextChanged += (s, e) =>
            {
                UpdateSearchCount();
            };

            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    FindInCurrentTab();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    HideSearchPanel();
                    e.SuppressKeyPress = true;
                }
            };

            lblSearchCount = new Label
            {
                Left = 380,
                Top = 8,
                AutoSize = true,
                Text = "Result(s): 0"
            };

            searchPanel.Controls.Add(lbl);
            searchPanel.Controls.Add(txtSearch);
            searchPanel.Controls.Add(lblSearchCount);

            this.Controls.Add(searchPanel);
            searchPanel.BringToFront();
        }

        private void ShowSearchPanel()
        {
            InitializeSearchPanel();
            searchPanel.Visible = true;
            txtSearch.Focus();
            txtSearch.SelectAll();
            UpdateSearchCount();
        }

        private void HideSearchPanel()
        {
            if (searchPanel == null) return;
            searchPanel.Visible = false;
            if (txtSearch != null)
                txtSearch.Text = "";
            if (lblSearchCount != null)
                lblSearchCount.Text = "Result(s): 0";
        }

        private void ClearHighlights(RichTextBox rtb)
        {
            if (rtb == null) return;
            var tab = tabControl1.SelectedTab;
            if (tab != null && rtfBackup.TryGetValue(tab, out var originalRtf))
            {
                try
                {
                    rtb.Rtf = originalRtf;
                }
                catch
                {
                    int selStart = rtb.SelectionStart;
                    int selLen = rtb.SelectionLength;
                    rtb.SelectAll();
                    rtb.SelectionBackColor = rtb.BackColor;
                    rtb.Select(selStart, selLen);
                }
                rtfBackup.Remove(tab);
            }
            else
            {
                int selStart = rtb.SelectionStart;
                int selLen = rtb.SelectionLength;
                rtb.SelectAll();
                rtb.SelectionBackColor = rtb.BackColor;
                rtb.Select(selStart, selLen);
            }
        }


        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            lastSearchTerm = txtSearch.Text;
            lastFoundIndex = 0;
            HighlightAllMatches(GetCurrentRichTextBox(), lastSearchTerm);
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FindNext();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                HideSearchPanel();
                e.SuppressKeyPress = true;
            }
        }

        private void FindNext()
        {
            var rtb = GetCurrentRichTextBox();
            if (rtb == null || string.IsNullOrEmpty(lastSearchTerm)) return;

            int index = rtb.Text.IndexOf(lastSearchTerm, lastFoundIndex, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                lastFoundIndex = 0;
                index = rtb.Text.IndexOf(lastSearchTerm, lastFoundIndex, StringComparison.OrdinalIgnoreCase);
            }
            if (index >= 0)
            {
                rtb.Select(index, lastSearchTerm.Length);
                rtb.ScrollToCaret();
                rtb.Focus();
                lastFoundIndex = index + lastSearchTerm.Length;
            }
        }

        private void FindPrevious()
        {
            var rtb = GetCurrentRichTextBox();
            if (rtb == null || string.IsNullOrEmpty(lastSearchTerm)) return;

            int index = rtb.Text.LastIndexOf(lastSearchTerm, lastFoundIndex - 1, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
                index = rtb.Text.LastIndexOf(lastSearchTerm, rtb.Text.Length - 1, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                rtb.Select(index, lastSearchTerm.Length);
                rtb.ScrollToCaret();
                rtb.Focus();
                lastFoundIndex = index;
            }
        }

        private void HighlightAllMatches(RichTextBox rtb, string query)
        {
            if (rtb == null || string.IsNullOrEmpty(query)) return;

            var tab = tabControl1.SelectedTab;
            if (tab != null && !rtfBackup.ContainsKey(tab))
            {
                try { rtfBackup[tab] = rtb.Rtf; }
                catch { /* ignore if not supported */ }
            }

            int origSelStart = rtb.SelectionStart;
            int origSelLen = rtb.SelectionLength;

            if (tab != null && rtfBackup.TryGetValue(tab, out var _))
            {
                try { rtb.Rtf = rtfBackup[tab]; } catch { /* ignore */ }
            }
            else
            {
                rtb.SelectAll();
                rtb.SelectionBackColor = rtb.BackColor;
            }

            string text = rtb.Text;
            int index = 0;
            var cmp = StringComparison.CurrentCultureIgnoreCase;
            while ((index = text.IndexOf(query, index, cmp)) != -1)
            {
                rtb.Select(index, query.Length);
                rtb.SelectionBackColor = System.Drawing.Color.Yellow;
                index += query.Length;
            }

            rtb.Select(origSelStart, origSelLen);
            rtb.ScrollToCaret();
        }


        private void MarkTabAsSaved(TabPage tab)
        {
            MarkTabAsDirty(tab, false);
        }


        private void BtnNewTab_Click(object sender, EventArgs e)
        {
            AddNewTab("Untitled " + (tabControl1.TabCount + 1));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            var unsavedTabs = tabControl1.TabPages
                .Cast<TabPage>()
                .Where(t => IsTabDirty(t))
                .Select(t => t.Text.TrimEnd('*'))
                .ToList();

            if (unsavedTabs.Count > 0)
            {
                string tabList = string.Join(", ", unsavedTabs.Take(5));
                if (unsavedTabs.Count > 5)
                    tabList += $" ... (+{unsavedTabs.Count - 5} more)";

                var result = MessageBox.Show(
                    "You have unsaved changes. Leaving now will discard any progress that hasn't been saved. Do you want to leave?",
                    "Unsaved Work",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void OpenProperties()
        {
            if (tabControl1.SelectedTab == null) return;

            Control editor = null;

            if (tabControl1.SelectedTab.Controls.Count > 0)
            {
                if (tabControl1.SelectedTab.Controls[0] is Panel panel && panel.Controls.Count > 0)
                    editor = panel.Controls[0];
                else
                    editor = tabControl1.SelectedTab.Controls[0];
            }

            if (editor == null)
            {
                MessageBox.Show("No editor found in this tab.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Font currentFont = editor.Font;
            System.Drawing.Color currentColor = editor.ForeColor;


            Form propForm = new Form
            {
                Width = 450,
                Height = 480,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                Text = "Tab Properties"
            };

            Label lblName = new Label() { Text = "Tab Name:", Left = 10, Top = 10 };
            System.Windows.Forms.TextBox txtName = new System.Windows.Forms.TextBox() { Left = 170, Top = 10, Width = 250, Text = tabControl1.SelectedTab.Text };

            Label lblFont = new Label() { Text = "Font:", Left = 10, Top = 50 };
            ComboBox cbFonts = new ComboBox() { Left = 170, Top = 50, Width = 250 };
            foreach (FontFamily f in FontFamily.Families) cbFonts.Items.Add(f.Name);
            cbFonts.SelectedItem = currentFont.FontFamily.Name;

            Label lblSize = new Label() { Text = "Size:", Left = 10, Top = 90 };
            NumericUpDown nudSize = new NumericUpDown
            {
                Left = 170,
                Top = 90,
                Width = 250,
                Minimum = 6,
                Maximum = 72,
                Value = (decimal)currentFont.Size
            };

            Label lblStyle = new Label() { Text = "Style:", Left = 10, Top = 130 };
            ComboBox cbStyle = new ComboBox() { Left = 170, Top = 130, Width = 250 };
            cbStyle.Items.AddRange(new string[] { "Regular", "Bold", "Italic", "Bold+Italic" });
            if (currentFont.Bold && currentFont.Italic) cbStyle.SelectedItem = "Bold+Italic";
            else if (currentFont.Bold) cbStyle.SelectedItem = "Bold";
            else if (currentFont.Italic) cbStyle.SelectedItem = "Italic";
            else cbStyle.SelectedItem = "Regular";

            Label lblColor = new Label() { Text = "Text Color:", Left = 10, Top = 170 };
            Button btnColor = new Button() { Left = 170, Top = 170, Width = 250, BackColor = currentColor, Text = "Select" };
            System.Drawing.Color selectedColor = currentColor;

            Panel previewPanel = new Panel { Left = 10, Top = 210, Width = 410, Height = 180, BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle };
            RichTextBox rtbPreview = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Text = "Hello World!",
                WordWrap = false,
                ReadOnly = true
            };
            previewPanel.Controls.Add(rtbPreview);

            void UpdatePreview()
            {
                FontStyle style = FontStyle.Regular;
                switch (cbStyle.SelectedItem.ToString())
                {
                    case "Bold": style = FontStyle.Bold; break;
                    case "Italic": style = FontStyle.Italic; break;
                    case "Bold+Italic": style = FontStyle.Bold | FontStyle.Italic; break;
                }
                rtbPreview.Font = new Font(cbFonts.SelectedItem.ToString(), (float)nudSize.Value, style);
                rtbPreview.ForeColor = selectedColor;
            }

            btnColor.Click += (s, e) =>
            {
                using ColorDialog cd = new ColorDialog();
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    selectedColor = cd.Color;
                    btnColor.BackColor = selectedColor;
                    UpdatePreview();
                }
            };

            cbFonts.SelectedIndexChanged += (s, e) => UpdatePreview();
            nudSize.ValueChanged += (s, e) => UpdatePreview();
            cbStyle.SelectedIndexChanged += (s, e) => UpdatePreview();

            Button btnOk = new Button() { Text = "Apply", Left = 10, Width = 80, Top = 405, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "Cancel", Left = 100, Width = 80, Top = 405, DialogResult = DialogResult.Cancel };

            btnOk.Click += (s, e) =>
            {
                tabControl1.SelectedTab.Text = txtName.Text;

                FontStyle style = FontStyle.Regular;
                switch (cbStyle.SelectedItem.ToString())
                {
                    case "Bold": style = FontStyle.Bold; break;
                    case "Italic": style = FontStyle.Italic; break;
                    case "Bold+Italic": style = FontStyle.Bold | FontStyle.Italic; break;
                }

                editor.Font = new Font(cbFonts.SelectedItem.ToString(), (float)nudSize.Value, style);
                editor.ForeColor = selectedColor;

                propForm.Close();
            };


            propForm.Controls.AddRange(new Control[]
            {
        lblName, txtName, lblFont, cbFonts, lblSize, nudSize, lblStyle, cbStyle,
        lblColor, btnColor, previewPanel, btnOk, btnCancel
            });

            propForm.AcceptButton = btnOk;
            propForm.CancelButton = btnCancel;

            propForm.ShowDialog();
        }

        private class RtfSnapshot
        {
            public string Rtf { get; }
            public int SelStart { get; }
            public int SelLength { get; }
            public RtfSnapshot(string rtf, int selStart, int selLength)
            {
                Rtf = rtf ?? string.Empty;
                SelStart = selStart;
                SelLength = selLength;
            }
        }

        private class EditorHistory
        {
            public List<RtfSnapshot> History { get; } = new List<RtfSnapshot>();
            public int Position { get; set; } = -1;
            public int MaxEntries { get; set; } = 400;
            public bool SuppressRecording { get; set; } = false;
            public System.Windows.Forms.Timer DebounceTimer { get; set; }

            public bool CanUndo => Position > 0;
            public bool CanRedo => Position < History.Count - 1;

            public void PushSnapshot(RtfSnapshot snap)
            {
                if (snap == null) return;

                if (Position >= 0 && Position < History.Count)
                {
                    var cur = History[Position];
                    if (cur.Rtf == snap.Rtf)
                        return;
                }

                if (Position < History.Count - 1)
                {
                    History.RemoveRange(Position + 1, History.Count - Position - 1);
                }

                History.Add(snap);
                Position = History.Count - 1;

                if (History.Count > MaxEntries)
                {
                    History.RemoveAt(0);
                    Position--;
                }
            }

            public RtfSnapshot UndoSnapshot()
            {
                if (!CanUndo) return null;
                Position--;
                return History[Position];
            }

            public RtfSnapshot RedoSnapshot()
            {
                if (!CanRedo) return null;
                Position++;
                return History[Position];
            }
        }


        private readonly Dictionary<TabPage, EditorHistory> _histories = new Dictionary<TabPage, EditorHistory>();

        private TabPage FindParentTabPageOfControl(Control c)
        {
            Control cur = c;
            while (cur != null)
            {
                if (cur is TabPage tp) return tp;
                cur = cur.Parent;
            }
            return null;
        }

        private void AttachHistoryToRichTextBox(RichTextBox rtb, TabPage tab, bool allowUndoToEmpty = true)
        {
            if (rtb == null || tab == null) return;

            if (!_histories.TryGetValue(tab, out var history))
            {
                history = new EditorHistory();
                _histories[tab] = history;

                var timer = new System.Windows.Forms.Timer { Interval = 400 };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    RecordSnapshotForTab(tab, rtb);
                };
                history.DebounceTimer = timer;
            }

            rtb.TextChanged -= Rtb_TextChanged_Debounced;
            rtb.TextChanged += Rtb_TextChanged_Debounced;

            if (history.Position == -1)
            {
                if (allowUndoToEmpty)
                {
                    history.PushSnapshot(new RtfSnapshot(string.Empty, 0, 0));
                }
                history.PushSnapshot(new RtfSnapshot(rtb.Rtf, rtb.SelectionStart, rtb.SelectionLength));
            }
        }
        private void Rtb_TextChanged_Debounced(object sender, EventArgs e)
        {
            if (!(sender is RichTextBox rtb)) return;
            var tab = FindParentTabPageOfControl(rtb);
            if (tab == null) return;
            if (!_histories.TryGetValue(tab, out var history)) return;
            if (history.SuppressRecording) return;

            var timer = history.DebounceTimer;
            if (timer == null) return;
            timer.Stop();
            timer.Start();
        }
        private void RecordSnapshotForTab(TabPage tab, RichTextBox rtb)
        {
            if (rtb == null || tab == null) return;
            if (!_histories.TryGetValue(tab, out var history)) return;
            if (history.SuppressRecording) return;

            var snap = new RtfSnapshot(rtb.Rtf, rtb.SelectionStart, rtb.SelectionLength);

            if (history.Position >= 0 && history.Position < history.History.Count)
            {
                if (history.History[history.Position].Rtf == snap.Rtf)
                {
                    return;
                }
            }

            if (history.Position < history.History.Count - 1)
            {
                history.History.RemoveRange(history.Position + 1, history.History.Count - history.Position - 1);
            }

            history.History.Add(snap);
            history.Position = history.History.Count - 1;

            if (history.History.Count > history.MaxEntries)
            {
                history.History.RemoveAt(0);
                history.Position--;
            }

            MarkTabAsDirty(tab, true);
            UpdateSearchCount();
        }

        private void ApplySnapshotToRichTextBox(RichTextBox rtb, TabPage tab, RtfSnapshot snap)
        {
            if (rtb == null || snap == null || tab == null) return;
            if (!_histories.TryGetValue(tab, out var history)) return;

            try
            {
                history.SuppressRecording = true;
                rtb.Rtf = snap.Rtf ?? string.Empty;
                rtb.SelectionStart = Math.Min(snap.SelStart, rtb.TextLength);
                rtb.SelectionLength = Math.Min(snap.SelLength, Math.Max(0, rtb.TextLength - rtb.SelectionStart));
            }
            finally
            {
                history.SuppressRecording = false;
            }

            MarkTabAsDirty(tab, true);
            UpdateSearchCount();
        }
        private void UndoInCurrentTab()
        {
            var rtb = GetCurrentRichTextBox();
            var tab = tabControl1.SelectedTab;
            if (rtb == null || tab == null) return;
            if (!_histories.TryGetValue(tab, out var history))
            {
                AttachHistoryToRichTextBox(rtb, tab, allowUndoToEmpty: true);
                return;
            }

            if (history.Position > 0)
            {
                history.Position--;
                var snap = history.History[history.Position];
                ApplySnapshotToRichTextBox(rtb, tab, snap);
            }
            else
            {
                if (rtb.CanUndo)
                {
                    history.SuppressRecording = true;
                    rtb.Undo();
                    history.SuppressRecording = false;
                    MarkTabAsDirty(tab, true);
                    UpdateSearchCount();
                }
            }
        }

        private void RedoInCurrentTab()
        {
            var rtb = GetCurrentRichTextBox();
            var tab = tabControl1.SelectedTab;
            if (rtb == null || tab == null) return;
            if (!_histories.TryGetValue(tab, out var history))
            {
                AttachHistoryToRichTextBox(rtb, tab, allowUndoToEmpty: true);
                return;
            }

            if (history.Position < history.History.Count - 1)
            {
                history.Position++;
                var snap = history.History[history.Position];
                ApplySnapshotToRichTextBox(rtb, tab, snap);
            }
            else
            {
                if (rtb.CanRedo)
                {
                    history.SuppressRecording = true;
                    rtb.Redo();
                    history.SuppressRecording = false;
                    MarkTabAsDirty(tab, true);
                    UpdateSearchCount();
                }
            }
        }

        private void UpdateSearchCount()
        {
            if (txtSearch == null) return;
            RichTextBox rtb = GetCurrentRichTextBox();
            if (rtb == null)
            {
                if (lblSearchCount != null) lblSearchCount.Text = "Result(s): 0";
                return;
            }

            string query = txtSearch.Text ?? "";
            if (string.IsNullOrWhiteSpace(query))
            {
                if (lblSearchCount != null) lblSearchCount.Text = "Result(s): 0";
                ClearHighlights(rtb);
                return;
            }

            int count = 0;
            int idx = 0;
            while ((idx = rtb.Text.IndexOf(query, idx, StringComparison.CurrentCultureIgnoreCase)) != -1)
            {
                count++;
                idx += Math.Max(1, query.Length);
            }

            if (lblSearchCount != null) lblSearchCount.Text = $"Result(s): {count}";

            HighlightAllMatches(rtb, query);
        }

        private readonly Dictionary<TabPage, string> rtfBackup = new Dictionary<TabPage, string>();


        private void FindInCurrentTab()
        {
            var rtb = GetCurrentRichTextBox();
            if (rtb == null) return;

            string query = txtSearch?.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                query = Prompt.ShowDialog("Enter text to find:", "Find", "");
                if (string.IsNullOrWhiteSpace(query)) return;
            }

            int startPos = Math.Max(0, rtb.SelectionStart + rtb.SelectionLength);
            int found = rtb.Text.IndexOf(query, startPos, StringComparison.CurrentCultureIgnoreCase);

            if (found == -1)
            {
                found = rtb.Text.IndexOf(query, 0, StringComparison.CurrentCultureIgnoreCase);
            }

            if (found >= 0)
            {
                rtb.Select(found, query.Length);
                rtb.ScrollToCaret();
                rtb.Focus();
            }
            else
            {
                MessageBox.Show($"'{query}' not found.", "Find", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private Label lblSearchCount;

        private T FindControlRecursive<T>(Control parent) where T : Control
        {
            if (parent == null) return null;
            foreach (Control c in parent.Controls)
            {
                if (c is T t) return t;
                var child = FindControlRecursive<T>(c);
                if (child != null) return child;
            }
            return null;
        }

        public static class Prompt
        {
            public static string ShowDialog(string text, string caption, string defaultValue)
            {
                Form prompt = new Form()
                {
                    Width = 400,
                    Height = 160,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = caption,
                    StartPosition = FormStartPosition.CenterScreen
                };
                Label textLabel = new Label() { Left = 20, Top = 20, Text = text, Width = 340 };
                System.Windows.Forms.TextBox inputBox = new System.Windows.Forms.TextBox() { Left = 20, Top = 50, Width = 340, Text = defaultValue };
                Button confirmation = new Button() { Text = "OK", Left = 280, Width = 80, Top = 80, DialogResult = DialogResult.OK };
                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;
                return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : "";
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                ToggleSearchPanel();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                UndoInCurrentTab();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                RedoInCurrentTab();
                e.SuppressKeyPress = true;
            }
        }

        private void ToggleSearchPanel()
        {
            if (searchPanel == null || txtSearch == null) return;

            if (searchPanel.Visible)
            {
                HideSearchPanel();
            }
            else
            {
                ShowSearchPanel();
            }
        }

        private List<Image> ExtractImagesFromCurrentTab()
        {
            List<Image> images = new List<Image>();
            Panel panel = tabControl1.SelectedTab.Controls[0] as Panel;

            foreach (Control ctl in panel.Controls)
            {
                if (ctl is PictureBox pb && pb.Image != null)
                {
                    images.Add(pb.Image);
                }
            }

            return images;
        }

        private void BtnSaveTab_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == null) return;

            Panel panel = tabControl1.SelectedTab.Controls[0] as Panel;
            RichTextBox rtbMain = panel.Controls[0] as RichTextBox;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|" +
                         "C# Source (*.cs)|*.cs|" +
                         "C/C++ Source (*.c;*.cpp;*.h)|*.c;*.cpp;*.h|" +
                         "Python Files (*.py)|*.py|" +
                         "Lua Files (*.lua)|*.lua|" +
                         "Java Files (*.java)|*.java|" +
                         "JavaScript Files (*.js)|*.js|" +
                         "TypeScript Files (*.ts)|*.ts|" +
                         "PHP Files (*.php)|*.php|" +
                         "Go Files (*.go)|*.go|" +
                         "Rust Files (*.rs)|*.rs|" +
                         "JSON Files (*.json)|*.json|" +
                         "XML Files (*.xml)|*.xml|" +
                         "YAML Files (*.yml;*.yaml)|*.yml;*.yaml|" +
                         "Markdown Files (*.md)|*.md|" +
                         "HTML Files (*.html;*.htm)|*.html;*.htm|" +
                         "Rich Text Format (*.rtf)|*.rtf|" +
                         "Word Document (*.docx)|*.docx|" +
                         "Word 97-2003 Document (*.doc)|*.doc|" +
                         "Notebook Pro Document (*.npd)|*.npd|" +
                         "All Files (*.*)|*.*",
                FileName = tabControl1.SelectedTab.Text,
                Title = "Save file"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            string ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();

            try
            {
                {
                    switch (ext)
                    {
                        case ".txt":
                        case ".npd":
                        case ".md":
                        case ".html":
                        case ".htm":
                        case ".cs":
                        case ".c":
                        case ".cpp":
                        case ".h":
                        case ".py":
                        case ".lua":
                        case ".java":
                        case ".js":
                        case ".ts":
                        case ".php":
                        case ".go":
                        case ".rs":
                        case ".json":
                        case ".xml":
                        case ".yml":
                        case ".yaml":
                            File.WriteAllText(sfd.FileName, rtbMain.Text);
                            break;

                        case ".rtf":
                            rtbMain.SaveFile(sfd.FileName, RichTextBoxStreamType.RichText);
                            break;

                        case ".docx":
                        case ".doc":
                            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rtbMain.Rtf)))
                            {
                                var rtfDoc = DocumentModel.Load(ms, LoadOptions.RtfDefault);
                                rtfDoc.Save(sfd.FileName);
                            }
                            break;

                        default:
                            File.WriteAllText(sfd.FileName, rtbMain.Text);
                            break;
                    }
                }

                MarkTabAsSaved(tabControl1.SelectedTab);
                MessageBox.Show("Saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MarkTabAsDirty(tabControl1.SelectedTab, false);
        }


        private void BtnLoadTab_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|" +
                         "C# Source (*.cs)|*.cs|" +
                         "C/C++ Source (*.c;*.cpp;*.h)|*.c;*.cpp;*.h|" +
                         "Python Files (*.py)|*.py|" +
                         "Lua Files (*.lua)|*.lua|" +
                         "Java Files (*.java)|*.java|" +
                         "JavaScript Files (*.js)|*.js|" +
                         "TypeScript Files (*.ts)|*.ts|" +
                         "PHP Files (*.php)|*.php|" +
                         "Go Files (*.go)|*.go|" +
                         "Rust Files (*.rs)|*.rs|" +
                         "JSON Files (*.json)|*.json|" +
                         "XML Files (*.xml)|*.xml|" +
                         "YAML Files (*.yml;*.yaml)|*.yml;*.yaml|" +
                         "Markdown Files (*.md)|*.md|" +
                         "HTML Files (*.html;*.htm)|*.html;*.htm|" +
                         "Rich Text Format (*.rtf)|*.rtf|" +
                         "Word Document (*.docx)|*.docx|" +
                         "Word 97-2003 Document (*.doc)|*.doc|" +
                         "Notebook Pro Document (*.npd)|*.npd|" +
                         "All Files (*.*)|*.*",
                Title = "Open file"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            string ext = Path.GetExtension(ofd.FileName).ToLowerInvariant();

            try
            {
                {
                    switch (ext)
                    {
                        case ".txt":
                        case ".npd":
                        case ".md":
                        case ".cs":
                        case ".c":
                        case ".cpp":
                        case ".h":
                        case ".py":
                        case ".lua":
                        case ".java":
                        case ".js":
                        case ".ts":
                        case ".php":
                        case ".go":
                        case ".rs":
                        case ".json":
                        case ".xml":
                        case ".yml":
                        case ".yaml":
                            string textContent = File.ReadAllText(ofd.FileName);
                            AddNewTabFromContent(Path.GetFileNameWithoutExtension(ofd.FileName), textContent);
                            break;

                        case ".html":
                        case ".htm":
                            string htmlContent = File.ReadAllText(ofd.FileName);
                            AddNewTabWithBrowser(Path.GetFileNameWithoutExtension(ofd.FileName), htmlContent);
                            break;

                        case ".rtf":
                            string rtf = File.ReadAllText(ofd.FileName);
                            AddNewTabFromRtf(Path.GetFileNameWithoutExtension(ofd.FileName), rtf);
                            break;

                        case ".docx":
                        case ".doc":
                            var docLoad = DocumentModel.Load(ofd.FileName);
                            using (var ms = new MemoryStream())
                            {
                                docLoad.Save(ms, SaveOptions.RtfDefault);
                                ms.Position = 0;
                                using (var reader = new StreamReader(ms))
                                {
                                    string rtfDoc = reader.ReadToEnd();
                                    AddNewTabFromRtf(Path.GetFileNameWithoutExtension(ofd.FileName), rtfDoc);
                                }
                            }
                            break;

                        default:
                            string fallback = File.ReadAllText(ofd.FileName);
                            AddNewTabFromContent(Path.GetFileNameWithoutExtension(ofd.FileName), fallback);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddNewTabWithBrowser(string tabName, string htmlContent)
        {
            TabPage tab = new TabPage(tabName);
            WebBrowser browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                DocumentText = htmlContent
            };
            tab.Controls.Add(browser);
            tabControl1.TabPages.Add(tab);
            tabControl1.SelectedTab = tab;
        }

        private void AddNewTabFromContent(string title, string content)
        {
            RichTextBox rtbMain = new RichTextBox
            {
                Dock = DockStyle.Fill,
                WordWrap = false,
                Text = content
            };

            TabPage tab = new TabPage(title);
            tab.Controls.Add(rtbMain);
            tabControl1.TabPages.Add(tab);
            tabControl1.SelectedTab = tab;

            AttachDirtyTracking(tab);
            MarkTabAsDirty(tab, false);
        }

        private void AddNewTabFromRtf(string title, string rtf)
        {
            RichTextBox rtbMain = new RichTextBox
            {
                Dock = DockStyle.Fill,
                WordWrap = false,
                Rtf = rtf
            };

            TabPage tab = new TabPage(title);
            tab.Controls.Add(rtbMain);
            tabControl1.TabPages.Add(tab);
            tabControl1.SelectedTab = tab;

            AttachDirtyTracking(tab);
            MarkTabAsDirty(tab, false);
        }

        private void TabControl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                for (int i = 0; i < tabControl1.TabCount; i++)
                {
                    Rectangle r = tabControl1.GetTabRect(i);
                    if (r.Contains(e.Location))
                    {
                        tabControl1.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void CloseSelectedTab()
        {
            if (tabControl1.SelectedTab != null)
                tabControl1.TabPages.Remove(tabControl1.SelectedTab);

            if (tabControl1.TabCount == 0)
                Application.Exit();
        }

        private void RenameSelectedTab()
        {
            if (tabControl1.SelectedTab != null)
            {
                string currentName = tabControl1.SelectedTab.Text;
                string newName = Prompt.ShowDialog("Enter new tab name:", "Rename Tab", currentName);
                if (!string.IsNullOrWhiteSpace(newName))
                    tabControl1.SelectedTab.Text = newName;
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
