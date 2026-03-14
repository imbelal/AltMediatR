using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Accessibility = Microsoft.CodeAnalysis.Accessibility;

namespace AltMediatR.SourceGenerator
{
    /// <summary>
    /// Roslyn incremental source generator that discovers AltMediatR handler implementations at compile
    /// time and emits a <c>GeneratedHandlerRegistrations.AddGeneratedHandlers</c> extension method on
    /// <c>IServiceCollection</c>.  This replaces the runtime-reflection-based
    /// <c>AddHandlersFromAssembly</c> / <c>AddDddHandlersFromAssembly</c> calls.
    /// </summary>
    [Generator]
    public sealed class HandlerRegistrationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Collect all handler registrations discovered in the compilation.
            var entries = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                    transform: GetEntries)
                .SelectMany(static (array, _) => array)
                .Collect();

            context.RegisterSourceOutput(entries, Emit);
        }

        /// <summary>
        /// For a class declaration node, returns one <see cref="HandlerEntry"/> for every handler
        /// interface it directly or indirectly implements.  Returns empty if the class is abstract,
        /// open-generic, not accessible from generated code, or implements none of the target interfaces.
        /// </summary>
        private static ImmutableArray<HandlerEntry> GetEntries(
            GeneratorSyntaxContext ctx, CancellationToken ct)
        {
            if (ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct)
                is not INamedTypeSymbol classSymbol)
                return ImmutableArray<HandlerEntry>.Empty;

            // Skip abstract classes and open-generic types — they cannot be concretely registered.
            if (classSymbol.IsAbstract || classSymbol.IsGenericType)
                return ImmutableArray<HandlerEntry>.Empty;

            // Skip types that are not accessible from generated code (e.g. private nested classes).
            if (!IsAccessibleFromGeneratedCode(classSymbol))
                return ImmutableArray<HandlerEntry>.Empty;

            var builder = ImmutableArray.CreateBuilder<HandlerEntry>();

            foreach (var iface in classSymbol.AllInterfaces)
            {
                if (!iface.IsGenericType) continue;

                if (!IsHandlerInterface(iface.ConstructedFrom)) continue;

                var serviceType = iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var implType = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var entry = new HandlerEntry(serviceType, implType);

                // Avoid duplicates when a class appears in multiple partial files.
                if (!builder.Contains(entry))
                    builder.Add(entry);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="ifaceDefinition"/> is one of the AltMediatR
        /// handler interface definitions (the unbound generic form).
        /// </summary>
        private static bool IsHandlerInterface(INamedTypeSymbol ifaceDefinition)
        {
            var ns = ifaceDefinition.ContainingNamespace?.ToDisplayString();
            var name = ifaceDefinition.Name;

            if (ns == "AltMediatR.Core.Abstractions")
            {
                return name == "IRequestHandler"
                    || name == "INotificationHandler"
                    || name == "IRequestPreProcessor"
                    || name == "IRequestPostProcessor";
            }

            if (ns == "AltMediatR.DDD.Abstractions")
            {
                return name == "IIntegrationEventHandler";
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="symbol"/> and all of its containing types
        /// are <c>public</c> or <c>internal</c> (i.e. referenceable from generated code in the
        /// same compilation).  Private, protected, or private-protected nested types are skipped
        /// because the generated registration code cannot name them.
        /// </summary>
        private static bool IsAccessibleFromGeneratedCode(INamedTypeSymbol symbol)
        {
            ISymbol? current = symbol;
            while (current != null)
            {
                if (current is INamedTypeSymbol namedType)
                {
                    var acc = namedType.DeclaredAccessibility;
                    if (acc != Accessibility.Public && acc != Accessibility.Internal)
                        return false;
                }
                current = current.ContainingSymbol;
                // Stop at namespace level
                if (current is INamespaceSymbol)
                    break;
            }
            return true;
        }

        /// <summary>
        /// Emits the <c>GeneratedHandlerRegistrations</c> source file.
        /// </summary>
        private static void Emit(
            SourceProductionContext ctx, ImmutableArray<HandlerEntry> entries)
        {
            // De-duplicate entries that could arise from partial classes split across files.
            var seen = new HashSet<HandlerEntry>();
            var unique = new List<HandlerEntry>(entries.Length);
            foreach (var e in entries)
            {
                if (seen.Add(e))
                    unique.Add(e);
            }

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// This file was generated by AltMediatR.SourceGenerator.");
            sb.AppendLine("// Do not edit this file manually.");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine();
            sb.AppendLine("namespace AltMediatR.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Auto-generated handler registrations produced by AltMediatR.SourceGenerator.");
            sb.AppendLine("    /// Uses compile-time code generation instead of runtime reflection.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class GeneratedHandlerRegistrations");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Registers all AltMediatR handlers discovered at compile time.");
            sb.AppendLine("        /// Call this instead of <c>AddHandlersFromAssembly</c> /");
            sb.AppendLine("        /// <c>AddDddHandlersFromAssembly</c> to eliminate runtime reflection.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(
                "        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddGeneratedHandlers(");
            sb.AppendLine(
                "            this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
            sb.AppendLine("        {");

            foreach (var entry in unique)
            {
                sb.AppendLine(
                    $"            services.AddTransient<{entry.ServiceType}, {entry.ImplementationType}>();");
            }

            sb.AppendLine("            return services;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            ctx.AddSource("GeneratedHandlerRegistrations.g.cs", sb.ToString());
        }
    }

    /// <summary>Value-equality pair of (service interface type, implementation type) display strings.</summary>
    internal readonly struct HandlerEntry : System.IEquatable<HandlerEntry>
    {
        public readonly string ServiceType;
        public readonly string ImplementationType;

        public HandlerEntry(string serviceType, string implementationType)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
        }

        public bool Equals(HandlerEntry other) =>
            ServiceType == other.ServiceType && ImplementationType == other.ImplementationType;

        public override bool Equals(object? obj) =>
            obj is HandlerEntry other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (ServiceType?.GetHashCode() ?? 0);
                hash = hash * 31 + (ImplementationType?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
