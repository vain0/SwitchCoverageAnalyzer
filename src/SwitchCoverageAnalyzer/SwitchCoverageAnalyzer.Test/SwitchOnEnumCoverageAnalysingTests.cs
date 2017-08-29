using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace SwitchCoverageAnalyzer.Test
{
    [TestClass]
    public sealed class SwitchOnEnumCoverageAnalysingTests
        : ConventionCodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SwitchOnEnumCoverageAnalysing.MyAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new SwitchOnEnumCoverageAnalysing.MyFixProvider();
        }

        [TestMethod]
        public void FixSortedCase() => VerifyCSharpByConvention();

        [TestMethod]
        public void FixUnsortedWithDefaultCase() => VerifyCSharpByConvention();

        [TestMethod]
        public void FixUnsortedWithoutDefaultCase() => VerifyCSharpByConvention();

        [TestMethod]
        public void NoReportCase() => VerifyCSharpByConvention();
    }
}
