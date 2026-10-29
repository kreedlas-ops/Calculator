using System;
using System.Globalization;
using Calculator;

internal static class EngineTests
{
    private static int _failures;

    private static void Main()
    {
        Check("1 + 2", Calculate("1+2="), "3");
        Check("10 + 1 expression", LastExpression("10+1="), "10 + 1 =");
        Check("0,1 + 0,2", Calculate("0.1+0.2="), "0,3");
        Check("chained operations", Calculate("5+3*2="), "16");
        Check("repeat equals", Calculate("5+3=="), "11");
        Check("operator replacement", Calculate("5+*2="), "10");
        Check("subtract", Calculate("7-10="), "-3");

        CalculatorEngine sign = new CalculatorEngine();
        sign.InputDigit(5);
        sign.ToggleSign();
        sign.SetOperator("*");
        sign.InputDigit(3);
        sign.Evaluate();
        Check("sign", sign.DisplayText, "-15");

        CalculatorEngine backspace = new CalculatorEngine();
        backspace.InputDigit(1);
        backspace.InputDigit(2);
        backspace.InputDigit(3);
        backspace.Backspace();
        Check("backspace", backspace.DisplayText, "12");

        CalculatorEngine clearEntry = new CalculatorEngine();
        clearEntry.InputDigit(5);
        clearEntry.SetOperator("+");
        clearEntry.InputDigit(7);
        clearEntry.ClearEntry();
        clearEntry.InputDigit(3);
        clearEntry.Evaluate();
        Check("clear entry", clearEntry.DisplayText, "8");

        Check("percent add", Calculate("200+10%="), "220");
        Check("percent multiply", Calculate("200*10%="), "20");
        Check("percent alone", Calculate("50%"), "0,5");
        Check("decimal typing", Calculate("1.50"), "1,50");
        Check("grouping", Calculate("1234567"), "1\u00A0234\u00A0567");
        Check("after equals digit", Calculate("2+2=7"), "7");

        CalculatorEngine divideByZero = new CalculatorEngine();
        divideByZero.InputDigit(9);
        divideByZero.SetOperator("/");
        divideByZero.InputDigit(0);
        divideByZero.Evaluate();
        if (!divideByZero.HasError)
        {
            Fail("divide by zero", "error state", divideByZero.DisplayText);
        }

        divideByZero.InputDigit(4);
        Check("error clears on digit", divideByZero.DisplayText, "4");

        CalculatorEngine memory = new CalculatorEngine();
        memory.InputDigit(8);
        memory.MemoryStore();
        memory.ClearAll();
        memory.MemoryRecall();
        Check("memory recall", memory.DisplayText, "8");
        memory.InputDigit(2);
        memory.MemoryAdd();
        memory.ClearAll();
        memory.MemoryRecall();
        Check("memory add", memory.DisplayText, "10");
        memory.MemoryClear();
        if (memory.HasMemory)
        {
            Fail("memory clear", "no memory", "has memory");
        }

        CalculatorEngine third = new CalculatorEngine();
        third.InputDigit(1);
        third.SetOperator("/");
        third.InputDigit(3);
        third.Evaluate();
        if (!third.DisplayText.StartsWith("0,333333333333333", StringComparison.Ordinal))
        {
            Fail("one third", "0,333333333333333...", third.DisplayText);
        }

        if (_failures == 0)
        {
            Console.WriteLine("All engine tests passed.");
            Environment.ExitCode = 0;
        }
        else
        {
            Console.WriteLine(_failures + " test(s) failed.");
            Environment.ExitCode = 1;
        }
    }

    private static string Calculate(string sequence)
    {
        CalculatorEngine engine = new CalculatorEngine();
        foreach (char c in sequence)
        {
            if (c >= '0' && c <= '9')
            {
                engine.InputDigit(c - '0');
            }
            else if (c == '.')
            {
                engine.InputDecimalSeparator();
            }
            else if (c == '=')
            {
                engine.Evaluate();
            }
            else if (c == '%')
            {
                engine.Percent();
            }
            else if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                engine.SetOperator(c.ToString());
            }
        }

        return engine.DisplayText;
    }

    private static string LastExpression(string sequence)
    {
        CalculatorEngine engine = new CalculatorEngine();
        foreach (char c in sequence)
        {
            if (c >= '0' && c <= '9')
            {
                engine.InputDigit(c - '0');
            }
            else if (c == '=')
            {
                engine.Evaluate();
            }
            else if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                engine.SetOperator(c.ToString());
            }
        }

        return engine.ExpressionText;
    }

    private static void Check(string name, string actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            Fail(name, expected, actual);
        }
    }

    private static void Fail(string name, string expected, string actual)
    {
        _failures++;
        Console.WriteLine("FAIL: " + name);
        Console.WriteLine("  expected: " + Escape(expected));
        Console.WriteLine("  actual:   " + Escape(actual));
    }

    private static string Escape(string value)
    {
        if (value == null)
        {
            return "<null>";
        }

        return value.Replace("\u00A0", "[NBSP]");
    }
}
