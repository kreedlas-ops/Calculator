using System;
using System.Globalization;
using System.Text;

namespace Calculator
{
    /// <summary>
    /// All calculator state and arithmetic. Independent of the UI, so it can be tested on its own.
    /// </summary>
    public sealed class CalculatorEngine
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        private string _display = "0";
        private bool _typing;
        private decimal? _accumulator;
        private string _pendingOp;
        private bool _waiting;
        private bool _justEvaluated;
        private string _expression = string.Empty;
        private string _error;
        private string _repeatOp;
        private decimal _repeatOperand;
        private decimal? _memory;

        public event EventHandler Changed;

        public string DisplayText
        {
            get
            {
                if (_error != null)
                {
                    return _error;
                }

                if (!_typing)
                {
                    return FormatValue(ParseDisplay());
                }

                return FormatTyped(_display);
            }
        }

        public string ExpressionText
        {
            get { return _expression; }
        }

        public bool HasError
        {
            get { return _error != null; }
        }

        public bool HasMemory
        {
            get { return _memory.HasValue; }
        }

        public decimal MemoryValue
        {
            get { return _memory.GetValueOrDefault(); }
        }

        public string MemoryText
        {
            get { return _memory.HasValue ? FormatValue(_memory.Value) : string.Empty; }
        }

        public void InputDigit(int digit)
        {
            if (digit < 0 || digit > 9)
            {
                return;
            }

            if (_error != null)
            {
                ResetAll();
            }

            if (_justEvaluated)
            {
                ResetAll();
            }

            if (_waiting)
            {
                _display = digit.ToString(Invariant);
                _waiting = false;
                _typing = true;
            }
            else
            {
                string digits = _display.Replace("-", string.Empty).Replace(".", string.Empty);
                if (digits.Length >= 16)
                {
                    return;
                }

                if (_display == "0")
                {
                    _display = digit.ToString(Invariant);
                }
                else if (_display == "-0")
                {
                    _display = "-" + digit.ToString(Invariant);
                }
                else
                {
                    _display += digit.ToString(Invariant);
                }

                _typing = true;
            }

            _justEvaluated = false;
            UpdatePreview();
            OnChanged();
        }

        public void InputDecimalSeparator()
        {
            if (_error != null)
            {
                ResetAll();
            }

            if (_justEvaluated)
            {
                ResetAll();
            }

            if (_waiting)
            {
                _display = "0.";
                _waiting = false;
                _typing = true;
            }
            else if (_display.IndexOf('.') < 0)
            {
                _display += ".";
                _typing = true;
            }

            _justEvaluated = false;
            UpdatePreview();
            OnChanged();
        }

        public void Backspace()
        {
            if (_error != null)
            {
                ResetAll();
                OnChanged();
                return;
            }

            if (_justEvaluated)
            {
                ResetAll();
                OnChanged();
                return;
            }

            if (_waiting)
            {
                return;
            }

            if (_display.Length <= 1 || (_display.Length == 2 && _display[0] == '-'))
            {
                _display = "0";
                _typing = false;
            }
            else
            {
                _display = _display.Substring(0, _display.Length - 1);
                if (_display == "-" || _display.Length == 0)
                {
                    _display = "0";
                    _typing = false;
                }
                else
                {
                    _typing = true;
                }
            }

            UpdatePreview();
            OnChanged();
        }

        public void ClearEntry()
        {
            if (_error != null)
            {
                ResetAll();
                OnChanged();
                return;
            }

            _display = "0";
            _typing = false;
            _waiting = false;
            _justEvaluated = false;
            _repeatOp = null;
            UpdatePreview();
            OnChanged();
        }

        public void ClearAll()
        {
            ResetAll();
            OnChanged();
        }

        public void ToggleSign()
        {
            if (_error != null)
            {
                return;
            }

            bool wasEvaluated = _justEvaluated;
            if (wasEvaluated)
            {
                _justEvaluated = false;
                _expression = string.Empty;
                _repeatOp = null;
            }

            if (_waiting)
            {
                _waiting = false;
            }

            if (_display == "0")
            {
                OnChanged();
                return;
            }

            if (_display.StartsWith("-", StringComparison.Ordinal))
            {
                _display = _display.Substring(1);
            }
            else
            {
                _display = "-" + _display;
            }

            _typing = false;

            if (wasEvaluated)
            {
                _waiting = true;
            }

            OnChanged();
        }

        public void Percent()
        {
            if (_error != null)
            {
                return;
            }

            decimal value = CurrentValue;

            if (_pendingOp == null)
            {
                value = value / 100m;
                SetComputedDisplay(value);
                _waiting = true;
                _justEvaluated = false;
                _repeatOp = null;
                _expression = string.Empty;
            }
            else if (_pendingOp == "+" || _pendingOp == "-")
            {
                decimal accumulator = _accumulator.GetValueOrDefault();
                value = accumulator * value / 100m;
                SetComputedDisplay(value);
                _waiting = false;
                _justEvaluated = false;
            }
            else
            {
                value = value / 100m;
                SetComputedDisplay(value);
                _waiting = false;
                _justEvaluated = false;
            }

            OnChanged();
        }

        public void SetOperator(string op)
        {
            if (_error != null || !IsOperator(op))
            {
                return;
            }

            if (_pendingOp != null)
            {
                if (_waiting)
                {
                    _pendingOp = op;
                    UpdatePreview();
                    OnChanged();
                    return;
                }

                decimal left = _accumulator.GetValueOrDefault();
                decimal right = CurrentValue;
                decimal result;
                if (!TryCalculate(left, right, _pendingOp, out result))
                {
                    return;
                }

                SetComputedDisplay(result);
                _accumulator = result;
            }
            else
            {
                _accumulator = CurrentValue;
            }

            _pendingOp = op;
            _waiting = true;
            _justEvaluated = false;
            _repeatOp = null;
            _expression = FormatValue(_accumulator.GetValueOrDefault()) + " " + OperatorSymbol(op);
            OnChanged();
        }

        public void Evaluate()
        {
            if (_error != null)
            {
                return;
            }

            if (_pendingOp != null)
            {
                decimal left = _accumulator.GetValueOrDefault();
                decimal right = _waiting ? left : CurrentValue;
                decimal result;
                if (!TryCalculate(left, right, _pendingOp, out result))
                {
                    return;
                }

                _expression = FormatValue(left) + " " + OperatorSymbol(_pendingOp) + " " +
                              FormatValue(right) + " =";
                _repeatOp = _pendingOp;
                _repeatOperand = right;
                SetComputedDisplay(result);
                _pendingOp = null;
                _accumulator = null;
                _waiting = true;
                _justEvaluated = true;
                OnChanged();
                return;
            }

            if (_justEvaluated && _repeatOp != null)
            {
                decimal left = CurrentValue;
                decimal result;
                if (!TryCalculate(left, _repeatOperand, _repeatOp, out result))
                {
                    return;
                }

                _expression = FormatValue(left) + " " + OperatorSymbol(_repeatOp) + " " +
                              FormatValue(_repeatOperand) + " =";
                SetComputedDisplay(result);
                _waiting = true;
                _typing = false;
                OnChanged();
            }
        }

        public void MemoryClear()
        {
            if (!_memory.HasValue)
            {
                return;
            }

            _memory = null;
            OnChanged();
        }

        public void MemoryRecall()
        {
            if (!_memory.HasValue)
            {
                return;
            }

            if (_error != null)
            {
                ResetAll();
            }

            _display = _memory.Value.ToString(Invariant);
            _typing = false;
            _error = null;
            _justEvaluated = false;

            if (_pendingOp == null)
            {
                _expression = string.Empty;
                _waiting = true;
            }
            else
            {
                _waiting = false;
            }

            OnChanged();
        }

        public void MemoryStore()
        {
            if (_error != null)
            {
                return;
            }

            _memory = CurrentValue;
            OnChanged();
        }

        public void MemoryAdd()
        {
            if (_error != null)
            {
                return;
            }

            try
            {
                _memory = _memory.GetValueOrDefault() + CurrentValue;
            }
            catch (OverflowException)
            {
                SetError("Переполнение");
                return;
            }

            OnChanged();
        }

        public void MemorySubtract()
        {
            if (_error != null)
            {
                return;
            }

            try
            {
                _memory = _memory.GetValueOrDefault() - CurrentValue;
            }
            catch (OverflowException)
            {
                SetError("Переполнение");
                return;
            }

            OnChanged();
        }

        private decimal CurrentValue
        {
            get { return ParseDisplay(); }
        }

        private decimal ParseDisplay()
        {
            string raw = _display;
            if (raw.EndsWith(".", StringComparison.Ordinal))
            {
                raw = raw.Substring(0, raw.Length - 1);
            }

            if (raw.Length == 0 || raw == "-")
            {
                return 0m;
            }

            decimal value;
            if (decimal.TryParse(raw, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    Invariant, out value))
            {
                return value;
            }

            return 0m;
        }

        private void SetComputedDisplay(decimal value)
        {
            _display = value.ToString(Invariant);
            _typing = false;
        }

        private void UpdatePreview()
        {
            if (_pendingOp != null)
            {
                _expression = FormatValue(_accumulator.GetValueOrDefault()) + " " + OperatorSymbol(_pendingOp);
            }
            else if (!_justEvaluated)
            {
                _expression = string.Empty;
            }
        }

        private bool TryCalculate(decimal left, decimal right, string op, out decimal result)
        {
            result = 0m;

            if (op == "/" && right == 0m)
            {
                SetError("Деление на 0");
                return false;
            }

            try
            {
                switch (op)
                {
                    case "+":
                        result = left + right;
                        break;
                    case "-":
                        result = left - right;
                        break;
                    case "*":
                        result = left * right;
                        break;
                    case "/":
                        result = left / right;
                        break;
                    default:
                        return false;
                }
            }
            catch (OverflowException)
            {
                SetError("Переполнение");
                return false;
            }
            catch (DivideByZeroException)
            {
                SetError("Деление на 0");
                return false;
            }

            return true;
        }

        private void SetError(string message)
        {
            _error = message;
            _expression = string.Empty;
            _pendingOp = null;
            _accumulator = null;
            _waiting = true;
            _justEvaluated = false;
            _typing = false;
            _repeatOp = null;
            OnChanged();
        }

        private void ResetAll()
        {
            _display = "0";
            _typing = false;
            _accumulator = null;
            _pendingOp = null;
            _waiting = false;
            _justEvaluated = false;
            _expression = string.Empty;
            _error = null;
            _repeatOp = null;
        }

        private static bool IsOperator(string op)
        {
            return op == "+" || op == "-" || op == "*" || op == "/";
        }

        internal static string OperatorSymbol(string op)
        {
            switch (op)
            {
                case "*":
                    return "×";
                case "/":
                    return "÷";
                case "-":
                    return "−";
                default:
                    return "+";
            }
        }

        private static string FormatValue(decimal value)
        {
            if (value == 0m)
            {
                return "0";
            }

            decimal abs = Math.Abs(value);
            if (abs < 0.000000000001m || abs >= 10000000000000000m)
            {
                return value.ToString("0.###############E+0", Russian);
            }

            string text = value.ToString("#,##0.###############", Russian);
            if (text == "-0")
            {
                return "0";
            }

            return text;
        }

        private static string FormatTyped(string raw)
        {
            bool negative = raw.StartsWith("-", StringComparison.Ordinal);
            if (negative)
            {
                raw = raw.Substring(1);
            }

            int dot = raw.IndexOf('.');
            string integerPart = dot >= 0 ? raw.Substring(0, dot) : raw;
            string fractionPart = dot >= 0 ? raw.Substring(dot + 1) : null;

            StringBuilder grouped = new StringBuilder(integerPart.Length + 4);
            for (int i = 0; i < integerPart.Length; i++)
            {
                if (i > 0 && (integerPart.Length - i) % 3 == 0)
                {
                    grouped.Append('\u00A0');
                }

                grouped.Append(integerPart[i]);
            }

            string result = grouped.ToString();
            if (fractionPart != null)
            {
                result += "," + fractionPart;
            }

            if (negative)
            {
                result = "-" + result;
            }

            return result;
        }

        private void OnChanged()
        {
            EventHandler handler = Changed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
