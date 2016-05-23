using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using ReturnAnalyzer;

namespace ReturnAnalyzer.Test
{
    [TestClass]
    public class ExpressionMethodTests : CodeFixVerifier
    {
        [TestMethod]
        public void ExpressionMethod()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    public string GetValue() => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 33) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest1 = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    public string GetValue() => string.Empty;
}";
            VerifyCSharpFix(test, fixtest1, 1);

            var fixtest2 = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    public string GetValue()
    {
        throw new InvalidOperationException();
    }
}";
            VerifyCSharpFix(test, fixtest2, 0);
        }

        [TestMethod]
        public void ExpressionBodiedMethod()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    string GetValue() => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 26) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest2 = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    string GetValue() => string.Empty;
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }

        [TestMethod]
        public void GenericExpressionMethod()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program<T>
{
    T GetValue() => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 21) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest2 = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program<T>
{
    T GetValue() => default(T);
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }

        [TestMethod]
        public void EnumerableExpressionMethod()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program<T>
{
    IEnumerable<int> GetValues() => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 37) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest2 = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program<T>
{
    IEnumerable<int> GetValues() => Enumerable.Empty<int>();
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new ReturnAnalyzerCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ReturnAnalyzerAnalyzer();
    }
}
