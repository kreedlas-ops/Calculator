using System;
using System.Drawing;
using System.Windows.Forms;

namespace Calculator
{
    internal sealed class MainWindow : Form
    {
        private readonly CalculatorEngine _engine = new CalculatorEngine();
        private readonly Timer _copyTimer = new Timer();

        private Label _modeLabel;
        private Label _expressionLabel;
        private Label _resultLabel;
        private CalcButton _hamburger;
        private CalcButton _infoButton;
        private CalcButton _memoryClear;
        private CalcButton _memoryRecall;
        private CalcButton _memoryChevron;

        private ContextMenuStrip _mainMenu;
        private ContextMenuStrip _memoryMenu;
        private Font _resultFont;
        private string _fitText;
        private int _fitWidth;

        public MainWindow()
        {
            Text = "Калькулятор";
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.DialogFont;
            ClientSize = new Size(332, 498);
            MinimumSize = new Size(320, 520);
            MaximumSize = new Size(620, 900);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            DoubleBuffered = true;
            KeyPreview = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
                // The default application icon is fine if extraction fails.
            }

            BuildInterface();

            _copyTimer.Interval = 1100;
            _copyTimer.Tick += delegate
            {
                _copyTimer.Stop();
                _expressionLabel.Text = _engine.ExpressionText;
            };

            _engine.Changed += delegate { UpdateFromEngine(); };
            Resize += delegate { FitResultFont(); };
            UpdateFromEngine();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.BackColor = Theme.Background;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.Padding = new Padding(0);
            root.Margin = new Padding(0);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel toolbar = new Panel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.BackColor = Theme.Background;
            toolbar.Margin = new Padding(0);

            _hamburger = new CalcButton();
            _hamburger.Style = KeyStyle.Ghost;
            _hamburger.Icon = KeyIcon.Menu;
            _hamburger.Bounds = new Rectangle(6, 4, 40, 38);
            _hamburger.AccessibleName = "Меню";
            _hamburger.Click += delegate { ShowMainMenu(); };

            _modeLabel = new Label();
            _modeLabel.Text = "Обычный";
            _modeLabel.Font = Theme.ModeFont;
            _modeLabel.ForeColor = Theme.Text;
            _modeLabel.BackColor = Theme.Background;
            _modeLabel.AutoSize = false;
            _modeLabel.TextAlign = ContentAlignment.MiddleLeft;
            _modeLabel.UseMnemonic = false;
            _modeLabel.Bounds = new Rectangle(50, 4, ClientSize.Width - 100, 38);

            _infoButton = new CalcButton();
            _infoButton.Style = KeyStyle.Ghost;
            _infoButton.Icon = KeyIcon.Info;
            _infoButton.Bounds = new Rectangle(ClientSize.Width - 46, 4, 40, 38);
            _infoButton.AccessibleName = "О программе";
            _infoButton.Click += delegate { ShowInfo(); };

            toolbar.Controls.Add(_hamburger);
            toolbar.Controls.Add(_modeLabel);
            toolbar.Controls.Add(_infoButton);
            toolbar.Resize += delegate
            {
                _modeLabel.Width = Math.Max(0, toolbar.ClientSize.Width - 100);
                _infoButton.Left = toolbar.ClientSize.Width - 46;
            };

            _expressionLabel = new Label();
            _expressionLabel.Dock = DockStyle.Fill;
            _expressionLabel.Font = Theme.ExpressionFont;
            _expressionLabel.ForeColor = Theme.TextSecondary;
            _expressionLabel.BackColor = Theme.Background;
            _expressionLabel.TextAlign = ContentAlignment.MiddleRight;
            _expressionLabel.Padding = new Padding(0, 0, 17, 0);
            _expressionLabel.UseMnemonic = false;
            _expressionLabel.AutoEllipsis = true;

            _resultLabel = new Label();
            _resultLabel.Dock = DockStyle.Fill;
            _resultLabel.Font = Theme.ResultFont(38f);
            _resultFont = _resultLabel.Font;
            _resultLabel.ForeColor = Theme.Text;
            _resultLabel.BackColor = Theme.Background;
            _resultLabel.TextAlign = ContentAlignment.MiddleRight;
            _resultLabel.Padding = new Padding(0, 0, 15, 0);
            _resultLabel.UseMnemonic = false;

            BufferedTable memory = new BufferedTable();
            memory.Dock = DockStyle.Fill;
            memory.BackColor = Theme.Background;
            memory.ColumnCount = 6;
            memory.RowCount = 1;
            memory.Padding = new Padding(2, 0, 2, 0);
            memory.Margin = new Padding(0);
            for (int i = 0; i < 6; i++)
            {
                memory.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));
            }

            memory.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _memoryClear = AddMemoryKey(memory, 0, "MC", delegate { _engine.MemoryClear(); });
            _memoryRecall = AddMemoryKey(memory, 1, "MR", delegate { _engine.MemoryRecall(); });
            AddMemoryKey(memory, 2, "M+", delegate { _engine.MemoryAdd(); });
            AddMemoryKey(memory, 3, "M−", delegate { _engine.MemorySubtract(); });
            AddMemoryKey(memory, 4, "MS", delegate { _engine.MemoryStore(); });

            _memoryChevron = new CalcButton();
            _memoryChevron.Style = KeyStyle.Ghost;
            _memoryChevron.Icon = KeyIcon.Chevron;
            _memoryChevron.Font = Theme.MemoryFont;
            _memoryChevron.ForeColor = Theme.TextSecondary;
            _memoryChevron.Dock = DockStyle.Fill;
            _memoryChevron.Margin = new Padding(1, 0, 1, 0);
            _memoryChevron.AccessibleName = "Список памяти";
            _memoryChevron.Click += delegate { ShowMemoryMenu(); };
            memory.Controls.Add(_memoryChevron, 5, 0);

            BufferedTable keypad = new BufferedTable();
            keypad.Dock = DockStyle.Fill;
            keypad.BackColor = Theme.Background;
            keypad.ColumnCount = 4;
            keypad.RowCount = 5;
            keypad.Padding = new Padding(4);
            keypad.Margin = new Padding(0);
            for (int i = 0; i < 4; i++)
            {
                keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            }

            for (int i = 0; i < 5; i++)
            {
                keypad.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            }

            AddKey(keypad, 0, 0, "%", Theme.PercentFont, KeyStyle.Function,
                delegate { _engine.Percent(); }, "Процент");
            AddKey(keypad, 1, 0, "CE", Theme.SmallKeyFont, KeyStyle.Function,
                delegate { _engine.ClearEntry(); }, "Очистить ввод");
            AddKey(keypad, 2, 0, "C", Theme.SmallKeyFont, KeyStyle.Function,
                delegate { _engine.ClearAll(); }, "Очистить всё");
            CalcButton backspace = AddKey(keypad, 3, 0, string.Empty, Theme.SmallKeyFont, KeyStyle.Function,
                delegate { _engine.Backspace(); }, "Удалить символ");
            backspace.Icon = KeyIcon.Backspace;

            AddKey(keypad, 0, 1, "7", Theme.KeyFont, KeyStyle.Number, DigitHandler(7), "7");
            AddKey(keypad, 1, 1, "8", Theme.KeyFont, KeyStyle.Number, DigitHandler(8), "8");
            AddKey(keypad, 2, 1, "9", Theme.KeyFont, KeyStyle.Number, DigitHandler(9), "9");
            AddKey(keypad, 3, 1, "÷", Theme.KeyFont, KeyStyle.Number,
                delegate { _engine.SetOperator("/"); }, "Деление");

            AddKey(keypad, 0, 2, "4", Theme.KeyFont, KeyStyle.Number, DigitHandler(4), "4");
            AddKey(keypad, 1, 2, "5", Theme.KeyFont, KeyStyle.Number, DigitHandler(5), "5");
            AddKey(keypad, 2, 2, "6", Theme.KeyFont, KeyStyle.Number, DigitHandler(6), "6");
            AddKey(keypad, 3, 2, "×", Theme.KeyFont, KeyStyle.Number,
                delegate { _engine.SetOperator("*"); }, "Умножение");

            AddKey(keypad, 0, 3, "1", Theme.KeyFont, KeyStyle.Number, DigitHandler(1), "1");
            AddKey(keypad, 1, 3, "2", Theme.KeyFont, KeyStyle.Number, DigitHandler(2), "2");
            AddKey(keypad, 2, 3, "3", Theme.KeyFont, KeyStyle.Number, DigitHandler(3), "3");
            AddKey(keypad, 3, 3, "−", Theme.KeyFont, KeyStyle.Number,
                delegate { _engine.SetOperator("-"); }, "Вычитание");

            AddKey(keypad, 0, 4, "+/−", Theme.KeyFont, KeyStyle.Number,
                delegate { _engine.ToggleSign(); }, "Сменить знак");
            AddKey(keypad, 1, 4, "0", Theme.KeyFont, KeyStyle.Number, DigitHandler(0), "0");
            AddKey(keypad, 2, 4, ",", Theme.KeyFont, KeyStyle.Number,
                delegate { _engine.InputDecimalSeparator(); }, "Запятая");
            AddKey(keypad, 3, 4, "=", Theme.AccentFont, KeyStyle.Accent,
                delegate { _engine.Evaluate(); }, "Равно");

            root.Controls.Add(toolbar, 0, 0);
            root.Controls.Add(_expressionLabel, 0, 1);
            root.Controls.Add(_resultLabel, 0, 2);
            root.Controls.Add(memory, 0, 3);
            root.Controls.Add(keypad, 0, 4);

            Controls.Add(root);
        }

        private CalcButton AddKey(TableLayoutPanel panel, int column, int row, string text, Font font,
            KeyStyle style, EventHandler onClick, string accessibleName)
        {
            CalcButton button = new CalcButton();
            button.Text = text;
            button.Font = font;
            button.Style = style;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(2);
            button.AccessibleName = accessibleName;
            if (onClick != null)
            {
                button.Click += onClick;
            }

            panel.Controls.Add(button, column, row);
            return button;
        }

        private CalcButton AddMemoryKey(TableLayoutPanel panel, int column, string text, EventHandler onClick)
        {
            CalcButton button = new CalcButton();
            button.Text = text;
            button.Font = Theme.MemoryFont;
            button.Style = KeyStyle.Ghost;
            button.ForeColor = Theme.TextSecondary;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(1, 0, 1, 0);
            button.AccessibleName = text;
            button.Click += onClick;
            panel.Controls.Add(button, column, 0);
            return button;
        }

        private EventHandler DigitHandler(int digit)
        {
            return delegate { _engine.InputDigit(digit); };
        }

        private void UpdateFromEngine()
        {
            _copyTimer.Stop();
            _expressionLabel.Text = _engine.ExpressionText;
            _resultLabel.Text = _engine.DisplayText;
            FitResultFont();

            bool hasMemory = _engine.HasMemory;
            _memoryClear.Enabled = hasMemory;
            _memoryRecall.Enabled = hasMemory;
            _memoryChevron.Enabled = hasMemory;
        }

        private void FitResultFont()
        {
            if (_resultLabel == null)
            {
                return;
            }

            int width = _resultLabel.ClientSize.Width - _resultLabel.Padding.Horizontal;
            if (width <= 20)
            {
                return;
            }

            string text = _resultLabel.Text ?? string.Empty;
            if (text == _fitText && width == _fitWidth)
            {
                return;
            }

            float size = 38f;
            Font candidate = Theme.ResultFont(size);
            Size measured = TextRenderer.MeasureText(text, candidate, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            while (size > 10f && measured.Width > width)
            {
                size -= 1f;
                candidate.Dispose();
                candidate = Theme.ResultFont(size);
                measured = TextRenderer.MeasureText(text, candidate, new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }

            _resultLabel.Font = candidate;
            Font old = _resultFont;
            _resultFont = candidate;
            if (old != null && !ReferenceEquals(old, candidate))
            {
                old.Dispose();
            }

            _fitText = text;
            _fitWidth = width;
        }

        private void ShowMainMenu()
        {
            if (_mainMenu == null)
            {
                _mainMenu = new ContextMenuStrip();
                _mainMenu.Renderer = new DarkMenuRenderer();
                _mainMenu.BackColor = Theme.MenuBackground;
                _mainMenu.ForeColor = Theme.Text;
                _mainMenu.Font = Theme.DialogFont;
                _mainMenu.ShowImageMargin = false;
                _mainMenu.ShowCheckMargin = true;
                _mainMenu.Padding = new Padding(0, 4, 0, 4);

                ToolStripMenuItem standard = new ToolStripMenuItem("Обычный");
                standard.Checked = true;
                standard.Padding = new Padding(2, 4, 2, 4);
                standard.Click += delegate { _mainMenu.Close(); };

                ToolStripMenuItem hotkeys = new ToolStripMenuItem("Горячие клавиши");
                hotkeys.Padding = new Padding(2, 4, 2, 4);
                hotkeys.Click += delegate { ShowHotkeys(); };

                ToolStripMenuItem about = new ToolStripMenuItem("О программе");
                about.Padding = new Padding(2, 4, 2, 4);
                about.Click += delegate { ShowInfo(); };

                ToolStripMenuItem exit = new ToolStripMenuItem("Выход");
                exit.Padding = new Padding(2, 4, 2, 4);
                exit.Click += delegate { Close(); };

                _mainMenu.Items.Add(standard);
                _mainMenu.Items.Add(new ToolStripSeparator());
                _mainMenu.Items.Add(hotkeys);
                _mainMenu.Items.Add(about);
                _mainMenu.Items.Add(new ToolStripSeparator());
                _mainMenu.Items.Add(exit);
            }

            _mainMenu.Show(_hamburger, new Point(0, _hamburger.Height + 2));
        }

        private void ShowMemoryMenu()
        {
            if (!_engine.HasMemory)
            {
                return;
            }

            if (_memoryMenu != null)
            {
                _memoryMenu.Dispose();
            }

            _memoryMenu = new ContextMenuStrip();
            _memoryMenu.Renderer = new DarkMenuRenderer();
            _memoryMenu.BackColor = Theme.MenuBackground;
            _memoryMenu.ForeColor = Theme.Text;
            _memoryMenu.Font = Theme.DialogFont;
            _memoryMenu.ShowImageMargin = false;
            _memoryMenu.ShowCheckMargin = false;
            _memoryMenu.Padding = new Padding(0, 4, 0, 4);

            ToolStripMenuItem recall = new ToolStripMenuItem(_engine.MemoryText);
            recall.Padding = new Padding(2, 4, 2, 4);
            recall.Click += delegate { _engine.MemoryRecall(); };

            ToolStripMenuItem clear = new ToolStripMenuItem("Очистить память");
            clear.Padding = new Padding(2, 4, 2, 4);
            clear.Click += delegate { _engine.MemoryClear(); };

            _memoryMenu.Items.Add(recall);
            _memoryMenu.Items.Add(new ToolStripSeparator());
            _memoryMenu.Items.Add(clear);
            _memoryMenu.Show(_memoryChevron, new Point(0, _memoryChevron.Height + 2));
        }

        private void ShowInfo()
        {
            string version = Application.ProductVersion;
            string body = "Простой калькулятор для Windows 11.\r\n\r\n" +
                          "Сложение, вычитание, умножение, деление, проценты и память — " +
                          "без лишних функций.\r\n\r\n" +
                          "Версия " + version + " · .NET Framework 4.8";
            using (InfoForm form = new InfoForm("О программе", "Калькулятор " + version, body, false))
            {
                form.ShowDialog(this);
            }
        }

        private void ShowHotkeys()
        {
            string body = "0–9            цифры\r\n" +
                          "+ − × ÷        арифметика\r\n" +
                          "Enter, =       результат\r\n" +
                          "Backspace      удалить символ\r\n" +
                          "Esc            очистить всё\r\n" +
                          "Delete         очистить ввод\r\n" +
                          "%              процент\r\n" +
                          ", или .        десятичная запятая\r\n" +
                          "F9             сменить знак\r\n" +
                          "Ctrl+C         копировать результат";
            using (InfoForm form = new InfoForm("Горячие клавиши", "Клавиши и действия", body, true))
            {
                form.ShowDialog(this);
            }
        }

        private void CopyResult()
        {
            try
            {
                Clipboard.SetText(_engine.DisplayText);
                _expressionLabel.Text = "Скопировано";
                _copyTimer.Stop();
                _copyTimer.Start();
            }
            catch (Exception)
            {
                // The clipboard can be busy; the calculation itself is unaffected.
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((_mainMenu != null && _mainMenu.Visible) || (_memoryMenu != null && _memoryMenu.Visible))
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            Keys key = keyData & Keys.KeyCode;
            Keys modifiers = keyData & Keys.Modifiers;
            bool shift = (modifiers & Keys.Shift) == Keys.Shift;
            bool control = (modifiers & Keys.Control) == Keys.Control;
            bool alt = (modifiers & Keys.Alt) == Keys.Alt;

            if (control && !alt && key == Keys.C)
            {
                CopyResult();
                return true;
            }

            if (control || alt)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (key == Keys.Enter)
            {
                _engine.Evaluate();
                return true;
            }

            if (key == Keys.Back)
            {
                _engine.Backspace();
                return true;
            }

            if (key == Keys.Escape)
            {
                _engine.ClearAll();
                return true;
            }

            if (key == Keys.Delete)
            {
                _engine.ClearEntry();
                return true;
            }

            if (key == Keys.F9)
            {
                _engine.ToggleSign();
                return true;
            }

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                _engine.InputDigit((int)(key - Keys.NumPad0));
                return true;
            }

            char character;
            if (Native.TryGetCharacter(keyData, out character))
            {
                if (character >= '0' && character <= '9')
                {
                    _engine.InputDigit(character - '0');
                    return true;
                }

                switch (character)
                {
                    case '+':
                        _engine.SetOperator("+");
                        return true;
                    case '-':
                    case '−':
                        _engine.SetOperator("-");
                        return true;
                    case '*':
                    case '×':
                        _engine.SetOperator("*");
                        return true;
                    case '/':
                    case '÷':
                        _engine.SetOperator("/");
                        return true;
                    case '%':
                        _engine.Percent();
                        return true;
                    case '=':
                        _engine.Evaluate();
                        return true;
                    case ',':
                    case '.':
                        _engine.InputDecimalSeparator();
                        return true;
                }
            }

            switch (key)
            {
                case Keys.Add:
                    _engine.SetOperator("+");
                    return true;
                case Keys.Subtract:
                    _engine.SetOperator("-");
                    return true;
                case Keys.Multiply:
                    _engine.SetOperator("*");
                    return true;
                case Keys.Divide:
                    _engine.SetOperator("/");
                    return true;
                case Keys.Decimal:
                case Keys.Oemcomma:
                case Keys.OemPeriod:
                    _engine.InputDecimalSeparator();
                    return true;
                case Keys.Oemplus:
                    if (shift)
                    {
                        _engine.SetOperator("+");
                    }
                    else
                    {
                        _engine.Evaluate();
                    }

                    return true;
                case Keys.OemMinus:
                    _engine.SetOperator("-");
                    return true;
                case (Keys)0xBF:
                case (Keys)0xDC:
                    // The / and \ keys on layouts such as Russian produce other characters.
                    _engine.SetOperator("/");
                    return true;
                case Keys.D5:
                    if (shift)
                    {
                        _engine.Percent();
                        return true;
                    }

                    break;
                case Keys.D8:
                    if (shift)
                    {
                        _engine.SetOperator("*");
                        return true;
                    }

                    break;
                case Keys.D0:
                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D6:
                case Keys.D7:
                case Keys.D9:
                    if (!shift)
                    {
                        _engine.InputDigit((int)(key - Keys.D0));
                        return true;
                    }

                    break;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.ApplyDarkChrome(Handle);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _copyTimer.Stop();
            _copyTimer.Dispose();

            if (_mainMenu != null)
            {
                _mainMenu.Dispose();
                _mainMenu = null;
            }

            if (_memoryMenu != null)
            {
                _memoryMenu.Dispose();
                _memoryMenu = null;
            }

            if (_resultFont != null)
            {
                _resultFont.Dispose();
                _resultFont = null;
            }

            base.OnFormClosed(e);
        }
    }

    internal sealed class BufferedTable : TableLayoutPanel
    {
        public BufferedTable()
        {
            DoubleBuffered = true;
        }
    }
}
