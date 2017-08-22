using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SwitchCoverageAnalyzer.SwitchOnEnumCoverageAnalysing
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MyAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor Rule { get; } =
            new DiagnosticDescriptor(
                "ScaSwitchOnEnumCoverage",
                "Some enum members are missing",
                "Missing enum members: {0}.",
                "Refactoring",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeStatement, SyntaxKind.SwitchStatement);
        }

        static void AnalyzeStatement(SyntaxNodeAnalysisContext context)
        {
            var switchStatement = (SwitchStatementSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (!SwitchAnalysis.TryAnalyze(switchStatement, semanticModel, out var analysis)) return;
            if (analysis.IsAllFound) return;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rule,
                    switchStatement.SwitchKeyword.GetLocation(),
                    string.Join(", ",  analysis.MissingEnumMembers.Select(c => $"'{c.Name}'"))
                ));
        }
    }
}
