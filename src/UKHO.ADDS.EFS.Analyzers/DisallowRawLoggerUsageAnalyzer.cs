using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UKHO.ADDS.EFS.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisallowNonLoggerMessageLoggingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG001";
        private const string Category = "Logging";

        private static readonly LocalizableString _title = "Only LoggerMessage-based logging is allowed";

        private static readonly LocalizableString _messageFormat =
            "This project uses Structured logging. Avoid calling '{0}' directly. Use LoggerMessage source-generated methods instead.";

        private static readonly LocalizableString _description =
            "Structured logging must use LoggerMessage source-generated methods only. Do not use Serilog.Log or ILogger directly.";

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Error,
            true,
            _description);

        private static readonly string[] _disallowedMethodNames =
        [
            "LogTrace", "LogDebug", "LogInformation", "LogWarning", "LogError", "LogCritical", "Log",
            "Verbose", "Debug", "Information", "Warning", "Error", "Fatal"
        ];

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            var expression = invocation.Expression;

            // Handle static and instance member access: e.g., Log.Information(...) or logger.LogInformation(...)
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                if (!_disallowedMethodNames.Contains(methodName))
                {
                    return;
                }

                var methodSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;
                if (methodSymbol is null)
                {
                    return;
                }

                // Allow LoggerMessage-generated methods
                if (methodSymbol.GetAttributes().Any(attr =>
                        attr.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
                {
                    return;
                }

                var containingType = methodSymbol.ContainingType;

                // Block Serilog.Log static method calls
                if (containingType.ToDisplayString() == "Serilog.Log")
                {
                    context.ReportDiagnostic(Diagnostic.Create(_rule, memberAccess.Name.GetLocation(), $"Serilog.Log.{methodName}"));
                    return;
                }

                // Block Microsoft.Extensions.Logging calls
                if (IsMicrosoftLogging(containingType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(_rule, memberAccess.Name.GetLocation(), $"{containingType.ToDisplayString()}.{methodName}"));
                }
            }
        }

        private static bool IsMicrosoftLogging(INamedTypeSymbol containingType) => containingType.ToDisplayString().StartsWith("Microsoft.Extensions.Logging");
    }
}
