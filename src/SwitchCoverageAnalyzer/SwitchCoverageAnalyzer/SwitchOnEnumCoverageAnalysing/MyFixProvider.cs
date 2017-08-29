using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace SwitchCoverageAnalyzer.SwitchOnEnumCoverageAnalysing
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MyFixProvider)), Shared]
    public sealed class MyFixProvider
        : CodeFixProvider
    {
        static readonly ImmutableArray<string> s_fixableDiagnosticIds =
            ImmutableArray.Create(MyAnalyzer.Rule.Id);

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            s_fixableDiagnosticIds;

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var ct = context.CancellationToken;

            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null) return;

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root == null) return;

            var switchStatement =
                root
                .FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<SwitchStatementSyntax>()
                .FirstOrDefault();
            if (switchStatement == null) return;

            var semanticModel = await context.Document.GetSemanticModelAsync(ct);
            if (semanticModel == null) return;

            if (!SwitchAnalysis.TryAnalyze(switchStatement, semanticModel, out var analysis)) return;
            if (analysis.IsAllFound) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    diagnostic.Descriptor.Title.ToString(),
                    _ =>
                        Task.FromResult(
                            context.Document.WithSyntaxRoot(
                                root.ReplaceNode(
                                    switchStatement,
                                    analysis.Fix()
                                ))),
                    equivalenceKey: diagnostic.GetMessage()
                ),
                diagnostic
            );
        }
    }
}
