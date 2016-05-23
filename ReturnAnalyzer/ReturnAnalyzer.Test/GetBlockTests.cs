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
    public class GetBlockTests : CodeFixVerifier
    {

        [TestMethod]
        public void GetterOnlyStringProperty()
        {
            var test = @"using System;
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
            return null;
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 20) }
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
    string Value
    {
        get
        {
            return string.Empty;
        }
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

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new ReturnAnalyzerCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ReturnAnalyzerAnalyzer();
    }
}
