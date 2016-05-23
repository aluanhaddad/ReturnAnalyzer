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
    public class BlockMethodTests : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [TestMethod]
        public void NoDiagnostics()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void StringReturningMethod()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    string GetValue()
    {
        return null;
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 16) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    string GetValue()
    {
        return string.Empty;
    }
}";

            VerifyCSharpFix(test, fixtest, 1);

            var fixtest2 = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    string GetValue()
    {
        throw new InvalidOperationException();
    }
}";
            VerifyCSharpFix(test, fixtest2, 0);
        }

        [TestMethod]
        public void EnumerableReturningMethod()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    public IEnumerable<int> GetValues()
    {
        return null;
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 16) }
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
    public IEnumerable<int> GetValues()
    {
        throw new InvalidOperationException();
    }
}";
            VerifyCSharpFix(test, fixtest2, 0);
            var fixtest3 = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    public IEnumerable<int> GetValues()
    {
        return Enumerable.Empty<int>();
    }
}";
            VerifyCSharpFix(test, fixtest3, 1);

        }

        [TestMethod]
        public void GenericMethod()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    T GetValue<T>() where T: class
    {
        return null;
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 16) }
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
    T GetValue<T>() where T: class
    {
        return default(T);
    }
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new ReturnAnalyzerCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ReturnAnalyzerAnalyzer();
    }
}
