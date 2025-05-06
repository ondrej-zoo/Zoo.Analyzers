using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zoo.Analyzers.CodeFixes.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class FileScopedNamespaceShouldBeFollowedByEmptyLineFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifier.FileScopedNamespaceShouldBeFollowedByEmptyLine);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Diagnostic diagnostic = context.Diagnostics.First();

        var codeAction = CodeAction.Create(
            title: "Insert empty line after file-scoped namespace",
            createChangedDocument: cancellationToken => InsertEmptyLineAfterFileScopedNamespaceAsync(context.Document, diagnostic, cancellationToken),
            equivalenceKey: "InsertEmptyLineAfterFileScopedNamespace");

        context.RegisterCodeFix(codeAction, diagnostic);

        return Task.CompletedTask;
    }

    private async Task<Document> InsertEmptyLineAfterFileScopedNamespaceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var namespaceNode = root.FindNode(diagnostic.Location.SourceSpan);

        if (namespaceNode is not FileScopedNamespaceDeclarationSyntax namespaceDeclaration)
        {
            return document;
        }

        var trailingTriviaList = namespaceDeclaration.SemicolonToken.TrailingTrivia
                .Add(SyntaxFactory.CarriageReturnLineFeed);

        var newNamespaceDeclaration = namespaceDeclaration.WithSemicolonToken(
            namespaceDeclaration.SemicolonToken.WithTrailingTrivia(trailingTriviaList));

        var newRoot = root.ReplaceNode(namespaceDeclaration, newNamespaceDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }
}
