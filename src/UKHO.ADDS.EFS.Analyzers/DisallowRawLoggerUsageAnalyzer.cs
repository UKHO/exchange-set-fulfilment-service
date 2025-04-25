using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UKHO.ADDS.EFS.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisallowRawLoggerUsageAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG001";
        private const string Category = "Logging";

        private static readonly LocalizableString _title = "Raw logging usage is not allowed";
        private static readonly LocalizableString _messageFormat = "This project uses Structured Logging. Avoid calling '{0}' directly. Use LoggerMessage source-generated methods instead.";

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Error,
            true);

        private static readonly string[] _disallowedMethods = { "LogTrace", "LogDebug", "LogInformation", "LogWarning", "LogError", "LogCritical" };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var expression = invocation.Expression;
            if (expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            var methodName = memberAccess.Name.Identifier.Text;
            if (!_disallowedMethods.Contains(methodName))
            {
                return;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (methodSymbol.ContainingType.ToDisplayString() != "Microsoft.Extensions.Logging.LoggerExtensions")
            {
                return;
            }

            var diagnostic = Diagnostic.Create(_rule, memberAccess.GetLocation(), methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
