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

        CaseSwitchLabelSyntax CreateCaseSwitchLabel(ISymbol symbol)
        {
            return
                SyntaxFactory.CaseSwitchLabel(
                    SyntaxFactory.ParseName(
                        symbol.ToMinimalDisplayString(
                            SemanticModel,
                            SwitchStatement.Span.Start
                        )),
                    SyntaxFactory.Token(SyntaxKind.ColonToken)
                );
        }

        SwitchSectionSyntax CreateSwitchSection(IEnumerable<ISymbol> symbols)
        {
            return
                SyntaxFactory.SwitchSection(
                    SyntaxFactory.List<SwitchLabelSyntax>(symbols.Select(CreateCaseSwitchLabel)),
                    SyntaxFactory.List<StatementSyntax>().Add(
                        SyntaxFactory.ThrowStatement(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.ParseTypeName(nameof(NotImplementedException)),
                                SyntaxFactory.ArgumentList(),
                                initializer: null
                            ))));
        }

        /// <summary>
        /// Updates the switch statement by inserting new section
        /// for each missing enum member at appropriate position
        /// if its sections except for default one are sorted in ascending order.
        /// </summary>
        bool TryInsertSectionsInAscendingOrder(out SwitchStatementSyntax result)
        {
            // Tries to find the enum member which appears firstly
            // as case label in the specified section.
            bool TryFirstEnumMember(SwitchSectionSyntax section, out ISymbol symbol)
            {
                foreach (var label in section.Labels)
                {
                    if (!(label is CaseSwitchLabelSyntax caseLabel)) continue;
                    if (caseLabel.Value == null) continue;

                    symbol = SemanticModel.GetSymbolInfo(caseLabel.Value).Symbol;
                    if (symbol == null) continue;
                    return true;
                }

                symbol = default;
                return false;
            }

            var sections = new List<SwitchSectionSyntax>(SwitchStatement.Sections.Count);
            var offset = 0;

            // Tries to find the enum member represented by the specified symbol
            // from the rest of member list and return its index.
            bool TryIndexOfEnumMember(ISymbol symbol, out int index)
            {
                for (var i = offset; i < EnumMembers.Length; i++)
                {
                    if (EnumMembers[i].Symbol == symbol)
                    {
                        index = i;
                        return true;
                    }
                }

                index = default;
                return false;
            }

            // Inserts new section for each enum member in the specified range
            // which isn't used as case label.
            void AddSections(int index, int endIndex)
            {
                for (var i = index; i < endIndex; i++)
                {
                    var em = EnumMembers[i];
                    if (em.IsFound) continue;
                    sections.Add(CreateSwitchSection(new[] { em.Symbol }));
                }
            }

            for (var si = 0; si < SwitchStatement.Sections.Count; si++)
            {
                var section = SwitchStatement.Sections[si];

                if (section == DefaultSectionOrNull)
                {
                    if (si == SwitchStatement.Sections.Count - 1)
                    {
                        // It's likely the final default section.
                        // Insert rest of new sections before it.
                        AddSections(offset, EnumMembers.Length);
                        offset = EnumMembers.Length;
                    }
                }
                else if (TryFirstEnumMember(section, out var firstEnumMember))
                {
                    if (!TryIndexOfEnumMember(firstEnumMember, out var mi))
                    {
                        result = default;
                        return false;
                    }

                    AddSections(offset, mi);
                    offset = mi + 1;
                }

                sections.Add(section);
            }

            AddSections(offset, EnumMembers.Length);

            result = SwitchStatement.WithSections(SyntaxFactory.List(sections));
            return true;
        }

        SwitchStatementSyntax AddCasesToDefaultSection(SwitchSectionSyntax defaultSection)
        {
            return
                SwitchStatement.ReplaceNode(
                    defaultSection,
                    defaultSection.WithLabels(
                        SyntaxFactory.List(
                            MissingEnumMembers.Select(CreateCaseSwitchLabel)
                            .Concat(defaultSection.Labels)
                        )));
        }

        public SwitchStatementSyntax Fix()
        {
            if (TryInsertSectionsInAscendingOrder(out var result))
            {
                return result;
            }
            else if (DefaultSectionOrNull != null)
            {
                return AddCasesToDefaultSection(DefaultSectionOrNull);
            }
            else
            {
                return SwitchStatement.AddSections(CreateSwitchSection(MissingEnumMembers));
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
            analysis = default;

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
