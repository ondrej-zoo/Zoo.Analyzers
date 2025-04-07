using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Zoo.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FileScopedNamespaceShouldBeFollowedByEmptyLineAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifier.FileScopedNamespaceShouldBeFollowedByEmptyLine,
        title: "File-scoped namespace should be followed by an empty line",
        messageFormat: "File-scoped namespace should be followed by an empty line",
        category: RuleCategory.Style,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/ondrej-zoo/Zoo.Analyzers");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.FileScopedNamespaceDeclaration);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var namespaceDeclaration = (FileScopedNamespaceDeclarationSyntax)context.Node;
        var classDeclaration = namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();

        // If there is no class declaration, then there is no need for an empty line
        if (classDeclaration is null)
        {
            return;
        }

        var firstClassLeadingTrivia = classDeclaration.GetLeadingTrivia()
            .FirstOrDefault();

        if (firstClassLeadingTrivia.Kind() is not SyntaxKind.EndOfLineTrivia)
        {
            // Get the syntax tree for the namespace declaration
            var syntaxTree = namespaceDeclaration.SyntaxTree;

            // Get the SourceText from the SyntaxTree
            var sourceText = syntaxTree.GetText();

            // Get the start position of the namespace declaration
            var startPosition = namespaceDeclaration.Span.Start;

            // Determine the line number where the FileScopedNamespaceDeclaration starts
            var line = sourceText.Lines.GetLineFromPosition(startPosition);

            // Get the TextSpan for the entire line
            var lineSpan = line.Span;

            // Create a Location for the entire line
            var lineLocation = Location.Create(syntaxTree, line.Span);

            var diagnostic = Diagnostic.Create(Rule, lineLocation, namespaceDeclaration.Name.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}