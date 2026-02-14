
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace UnitTests.WhatIf
{
    public sealed class InputOverrides : Dictionary<string, string?> 
    {
        public static InputOverrides<T> For<T>(string? configSectionName = null) => new(new(), configSectionName);
    }

    public readonly record struct InputOverrides<TTarget>(InputOverrides Overrides, string? ConfigSectionName = null)
    {
        // Set single value
        public InputOverrides<TTarget> With<TValue>(Expression<Func<TTarget, TValue>> expression, TValue value)
        {
            var propertyPath = GetConfigurationPath(expression);
            Overrides[propertyPath] = value?.ToString();
            return this;
        }

        // Append suggested value to the collection. Target property MUST be a primitive-collection. 
        public InputOverrides<TTarget> Append<TCollection, TItem>(Expression<Func<TTarget, TCollection>> expression, TItem value, IConfiguration baseConfig) where TCollection : IEnumerable<TItem>
        {
            var propertyPathPrefix = GetConfigurationPath(expression);

            // We leverage the fact that all IConfiguration primitives are string(s)
            var oneOrMoreValues = GetArrayValues(baseConfig, propertyPathPrefix);
            oneOrMoreValues.Add(value?.ToString() ?? "");
            SetArrayValues(Overrides, propertyPathPrefix, oneOrMoreValues);

            return this;
        }

        /// <summary>
        /// Transition to next target with optional ConfigSction name if different from convention.
        /// </summary>
        public InputOverrides<TNext> For<TNext>(string? configSectionName = null) => new (Overrides, configSectionName);
        
        /// <summary>
        /// Returns IConfiguration compatible config path using the property expression
        /// </summary>
        private string GetConfigurationPath<TProperty>(Expression<Func<TTarget, TProperty>> expression)
        {
            var members = new Stack<string>();

            Expression? current = expression.Body;

            // Defensive: Unwrap boxing conversion for value types (e.g., int, double).
            if (current is UnaryExpression unary && unary.NodeType == ExpressionType.Convert) current = unary.Operand;

            // Visits the property expression, right to left
            while (current is MemberExpression member)
            {
                // Remember the property names, right-to-left
                members.Push(member.Member.Name);
                current = member.Expression;
            }

            // If not specified, top level ConfigSection name is same as simple type name of the target
            members.Push(ConfigSectionName ?? typeof(TTarget).Name);

            // Convert to IConfiguration style config path
            return string.Join(":", members);
        }

        private static List<string> GetArrayValues(IConfiguration baseConfig, string pathPrefix)
        {
            return baseConfig
                .GetSection(pathPrefix)
                .GetChildren()
                .Select(child => child.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList()!;
        }

        private static void SetArrayValues(InputOverrides overrides, string pathPrefix, IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                overrides[$"{pathPrefix}:{i}"] = values[i];
            }
        }

        // Syntax sugar: Allows friction free assignment of Override<T> to InputOverrides
        public static implicit operator InputOverrides(InputOverrides<TTarget> context) => context.Overrides;

    }
}
