using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SwitchCoverageAnalyzer.SwitchOnEnumCoverageAnalysing
{
    public sealed class SwitchAnalysis
    {
        SwitchStatementSyntax SwitchStatement { get; }
        SemanticModel SemanticModel { get; }
        SwitchSectionSyntax DefaultSectionOrNull { get; }
        ITypeSymbol EnumTypeSymbol { get; }
        ImmutableArray<ISymbol> AllCases { get; }
        public ImmutableArray<ISymbol> MissingCases { get; }

        public SwitchAnalysis(SwitchStatementSyntax switchStatement, SemanticModel semanticModel, SwitchSectionSyntax defaultSectionOrNull, ITypeSymbol enumTypeSymbol, ImmutableArray<ISymbol> allCases, ImmutableArray<ISymbol> missingCases)
        {
            SwitchStatement = switchStatement;
            SemanticModel = semanticModel;
            DefaultSectionOrNull = defaultSectionOrNull;
            EnumTypeSymbol = enumTypeSymbol;
            AllCases = allCases;
            MissingCases = missingCases;
        }

        public SwitchStatementSyntax Fix()
        {
            var additionalLabels =
                MissingCases.Select(caseSymbol =>
                    SyntaxFactory.CaseSwitchLabel(
                        SyntaxFactory.ParseName(
                            caseSymbol.ToMinimalDisplayString(
                                SemanticModel,
                                SwitchStatement.Span.Start
                            )),
                        SyntaxFactory.Token(SyntaxKind.ColonToken)
                    ))
                .ToArray();

            if (DefaultSectionOrNull != null)
            {
                var section = DefaultSectionOrNull;
                return
                    SwitchStatement.ReplaceNode(
                        section,
                        section.WithLabels(
                            SyntaxFactory.List(additionalLabels.Concat(section.Labels))
                        ));
            }
            else
            {
                var section =
                    SyntaxFactory.SwitchSection(
                        SyntaxFactory.List<SwitchLabelSyntax>(additionalLabels),
                        SyntaxFactory.List<StatementSyntax>().Add(
                            SyntaxFactory.ThrowStatement(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.ParseTypeName(nameof(NotImplementedException)),
                                    SyntaxFactory.ArgumentList(),
                                    initializer: null
                                ))));
                return SwitchStatement.AddSections(section);
            }
        }

        public static bool TryAnalyze(SwitchStatementSyntax switchStatement, SemanticModel semanticModel, out SwitchAnalysis analysis)
        {
            analysis = default(SwitchAnalysis);

            if (switchStatement.Expression == null) return false;

            var typeSymbol = semanticModel.GetTypeInfo(switchStatement.Expression).Type;
            if (typeSymbol == null || typeSymbol.TypeKind != TypeKind.Enum) return false;

            var cases =
                typeSymbol.GetMembers()
                .Where(m => m.Kind == SymbolKind.Field)
                .ToImmutableArray();

            var foundCases = new HashSet<ISymbol>();
            var defaultSectionOrNull = default(SwitchSectionSyntax);

            foreach (var section in switchStatement.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label is CaseSwitchLabelSyntax caseLabel)
                    {
                        if (caseLabel.Value == null) continue;

                        var symbol = semanticModel.GetSymbolInfo(caseLabel.Value).Symbol;
                        if (symbol == null) continue;

                        foundCases.Add(symbol);
                    }
                    else if (
                        label.Keyword.IsKind(SyntaxKind.DefaultKeyword)
                        && defaultSectionOrNull == null
                        )
                    {
                        defaultSectionOrNull = section;
                    }
                }
            }

            var unfoundCases = cases.Where(c => !foundCases.Contains(c)).ToImmutableArray();

            analysis =
                new SwitchAnalysis(
                    switchStatement,
                    semanticModel,
                    defaultSectionOrNull,
                    typeSymbol,
                    cases,
                    unfoundCases
                );
            return true;
        }
    }
}
