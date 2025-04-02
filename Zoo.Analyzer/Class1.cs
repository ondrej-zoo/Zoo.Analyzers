using FileScopedNamespaceWithEmptyLineAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Zoo.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InclusivenessAnalyzer : DiagnosticAnalyzer
{
    private static readonly Dictionary<string, string> InclusiveTerms = new Dictionary<string, string>() {
        {"whitelist", "allow list, access list, permit" },
        {"white list", "allow list, access list, permit" },
        {"blacklist", "deny list, blocklist, exclude list" },
        {"black list", "deny list, blocklist, exclude list" }
    };

    private static readonly DiagnosticDescriptor Rule = new (
        id: RuleIdentifier.FileScopedNamespaceShouldFollowedByEmptyLine,
        title: "File-scoped namespace should be followed by an empty line",
        messageFormat: "File-scoped namespace '{0}' should be followed by an empty line",
        category: RuleCategory.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "This is description string.",
        helpLinkUri: $"https://not-implemented/{RuleIdentifier.FileScopedNamespaceShouldFollowedByEmptyLine}");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
        try
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field,
                SymbolKind.Event, SymbolKind.Namespace, SymbolKind.Parameter);

            context.RegisterSyntaxNodeAction(CheckComments, SyntaxKind.VariableDeclaration, SyntaxKind.CatchDeclaration, SyntaxKind.NamespaceDeclaration,
                SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.EnumDeclaration, SyntaxKind.DelegateDeclaration,
                SyntaxKind.EnumMemberDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.EventFieldDeclaration, SyntaxKind.MethodDeclaration, SyntaxKind.OperatorDeclaration,
                SyntaxKind.ConversionOperatorDeclaration, SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration, SyntaxKind.PropertyDeclaration,
                SyntaxKind.EventDeclaration, SyntaxKind.IndexerDeclaration, SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration,
                SyntaxKind.AddAccessorDeclaration, SyntaxKind.RemoveAccessorDeclaration, SyntaxKind.UnknownAccessorDeclaration);
        }
        catch { }
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        try
        {
            var symbol = context.Symbol;

            // Find just those named type symbols with non-inclusive terms.
            foreach (KeyValuePair<string, string> entry in InclusiveTerms)
            {
                if (IsMatch(symbol.Name, entry.Key))
                {
                    // For all such symbols, produce a diagnostic.
                    var diagnostic = Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name, entry.Value);

                    context.ReportDiagnostic(diagnostic);
                    break;
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Checks if term was found in the symbol.
    /// Match whole word if it has less than 5 characters
    /// </summary>
    /// <param name="symbol">The phrase that is being checked.</param>
    /// <param name="term">The non-inclusive phrase we are looking for.</param>
    /// <returns></returns>
    private static bool IsMatch(string symbol, string term)
    {
        return term.Length < 5 ?
            symbol.Equals(term, StringComparison.InvariantCultureIgnoreCase) :
            symbol.IndexOf(term, StringComparison.InvariantCultureIgnoreCase) >= 0;

    }

    private void CheckComments(SyntaxNodeAnalysisContext context)
    {
        try
        {
            var node = context.Node;

            var xmlTrivia = node.GetLeadingTrivia()
                .Select(i => i.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();

            if (xmlTrivia == null) return;

            var content = xmlTrivia.ToFullString();
            foreach (KeyValuePair<string, string> entry in InclusiveTerms)
            {
                if (IsMatch(content, entry.Key))
                {
                    // For all such symbols, produce a diagnostic.
                    var diagnostic = Diagnostic.Create(Rule, xmlTrivia.GetLocation(), entry.Key, entry.Value);

                    context.ReportDiagnostic(diagnostic);
                    break;
                }
            }
        }
        catch { }
    }
}