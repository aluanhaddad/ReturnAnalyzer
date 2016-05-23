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
    public class LambdaExpressionTests : CodeFixVerifier
    {
        [TestMethod]
        public void LambdaExpressionWithStringTargetType()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    Func<string> getValue = () => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 35) }
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
    Func<string> getValue = () => string.Empty;
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
    Func<string> getValue = () =>
    {
        throw new InvalidOperationException();
    };
}";
            VerifyCSharpFix(test, fixtest2, 0);
        }

        [TestMethod]
        public void SimpleLambdaExpressionWithStringTargetType()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    Func<int, string> getValue = x => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 39) }
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
    Func<int, string> getValue = x => string.Empty;
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
    Func<int, string> getValue = x =>
    {
        throw new InvalidOperationException();
    };
}";
            VerifyCSharpFix(test, fixtest2, 0);
        }

        [TestMethod]
        public void SimpleLambdaExpressionWithEnumerableTargetType()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    public static Func<IEnumerable<int>> getValues = x => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 59) }
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
    public static Func<IEnumerable<int>> getValues = x => Enumerable.Empty<int>();
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }

        [TestMethod]
        public void ParenthesizedLambdaExpressionWithEnumerableTargetType()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    Func<IEnumerable<int>> getValues = () => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 46) }
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
    Func<IEnumerable<int>> getValues = () => Enumerable.Empty<int>();
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }

        [TestMethod]
        public void LambdaExpressionWithGenericTargetType()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Program<T>
{
    Func<T> getValue = () => null;
}";
            var expected = new DiagnosticResult
            {
                Id = "ReturnAnalyzer",
                Message = $"null returned",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 30) }
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
    Func<T> getValue = () => default(T);
}";
            VerifyCSharpFix(test, fixtest2, 1);
        }
        
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new ReturnAnalyzerCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ReturnAnalyzerAnalyzer();
    }
}