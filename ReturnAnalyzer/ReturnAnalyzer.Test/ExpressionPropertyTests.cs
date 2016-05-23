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
    public class ExpressionPropertyTests : CodeFixVerifier
    {
        [TestMethod]
        public void ExpressionProperty()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    string Value => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 21) }
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
    string Value => string.Empty;
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
    string Value
    {
        get
        {
            throw new InvalidOperationException();
        }
    }
}";
            VerifyCSharpFix(test, fixtest2, 0);
        }

        [TestMethod]
        public void EnumerableExpressionProperty()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program<T>
{
    IEnumerable<int> Values => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 32) }
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
    IEnumerable<int> Values => Enumerable.Empty<int>();
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }

        [TestMethod]
        public void GenericExpressionProperty()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program<T>
{
    T Value => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 16) }
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
    T Value => default(T);
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new ReturnAnalyzerCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ReturnAnalyzerAnalyzer();
    }
}
