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
        public struct EnumMember
        {
            public ISymbol Symbol { get; }

            /// <summary>
            /// Determines if this member appeared as case label.
            /// </summary>
            public bool IsFound { get; }

            public EnumMember(ISymbol symbol, bool isFound)
            {
                Symbol = symbol;
                IsFound = isFound;
            }
        }

        SwitchStatementSyntax SwitchStatement { get; }
        SemanticModel SemanticModel { get; }
        SwitchSectionSyntax DefaultSectionOrNull { get; }
        ITypeSymbol EnumTypeSymbol { get; }
        ImmutableArray<EnumMember> EnumMembers { get; }

        public bool IsAllFound =>
            EnumMembers.All(em => em.IsFound);

        public IEnumerable<ISymbol> MissingEnumMembers =>
            EnumMembers.Where(em => !em.IsFound).Select(em => em.Symbol);

        public SwitchAnalysis(SwitchStatementSyntax switchStatement, SemanticModel semanticModel, SwitchSectionSyntax defaultSectionOrNull, ITypeSymbol enumTypeSymbol, ImmutableArray<EnumMember> enumMembers)
        {
            SwitchStatement = switchStatement;
            SemanticModel = semanticModel;
            DefaultSectionOrNull = defaultSectionOrNull;
            EnumTypeSymbol = enumTypeSymbol;
            EnumMembers = enumMembers;
        }

        public SwitchStatementSyntax Fix()
        {
            var additionalLabels =
                MissingEnumMembers.Select(symbol =>
                    SyntaxFactory.CaseSwitchLabel(
                        SyntaxFactory.ParseName(
                            symbol.ToMinimalDisplayString(
                                SemanticModel,
                                SwitchStatement.Span.Start
                            )),
                        SyntaxFactory.Token(SyntaxKind.ColonToken)
                    ));

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

        static void AnalyzeSections(SwitchStatementSyntax switchStatement, SemanticModel semanticModel, out HashSet<ISymbol> foundEnumMembers, out SwitchSectionSyntax defaultSectionOrNull)
        {
            foundEnumMembers = new HashSet<ISymbol>();
            defaultSectionOrNull = null;

            foreach (var section in switchStatement.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label is CaseSwitchLabelSyntax caseLabel)
                    {
                        if (caseLabel.Value == null) continue;

                        var symbol = semanticModel.GetSymbolInfo(caseLabel.Value).Symbol;
                        if (symbol == null) continue;

                        foundEnumMembers.Add(symbol);
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
        }

        public static bool TryAnalyze(SwitchStatementSyntax switchStatement, SemanticModel semanticModel, out SwitchAnalysis analysis)
        {
            analysis = default(SwitchAnalysis);

            if (switchStatement.Expression == null) return false;

            var typeSymbol = semanticModel.GetTypeInfo(switchStatement.Expression).Type;
            if (typeSymbol == null || typeSymbol.TypeKind != TypeKind.Enum) return false;

            AnalyzeSections(switchStatement, semanticModel, out var foundEnumMembers, out var defaultSectionOrNull);

            var enumMembers =
                typeSymbol
                .GetMembers()
                .Where(symbol => symbol.Kind == SymbolKind.Field)
                .Select(symbol => new EnumMember(symbol, foundEnumMembers.Contains(symbol)))
                .ToImmutableArray();

            analysis =
                new SwitchAnalysis(
                    switchStatement,
                    semanticModel,
                    defaultSectionOrNull,
                    typeSymbol,
                    enumMembers
                );
            return true;
        }
    }
}
