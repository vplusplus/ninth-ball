using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace UnitTests.WhatIf
{
    public sealed class InputOverrides : Dictionary<string, string?> { }

    public static class Overridable
    {
        public static Override<T> For<T>(this InputOverrides overrides) => new(overrides);
    }

    public readonly record struct Override<T>(InputOverrides Overrides)
    {
        // Set single value
        public Override<T> With<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            var propertyPath = GetConfigurationPath(expression);
            Overrides[propertyPath] = value?.ToString();
            return this;
        }

        public Override<T> Append<TCollection, TItem>(Expression<Func<T, TCollection>> expression, TItem value, IConfiguration baseConfig) where TCollection : IEnumerable<TItem>
        {
            // Append() expects a collection style property
            // We leverage the fact that all IConfiguration primitives are string(s)

            var propertyPathPrefix = GetConfigurationPath(expression);
            var oneOrMoreValues = GetArrayValues(baseConfig, propertyPathPrefix);
            oneOrMoreValues.Add(value?.ToString() ?? "");
            SetArrayValues(Overrides, propertyPathPrefix, oneOrMoreValues);

            return this;
        }

        // Lead to next 
        public Override<TNext> For<TNext>() => new (Overrides);

        private static string GetConfigurationPath<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var members = new Stack<string>();

            Expression? current = expression.Body;

            if (current is UnaryExpression unary && unary.NodeType == ExpressionType.Convert) current = unary.Operand;

            while (current is MemberExpression member)
            {
                members.Push(member.Member.Name);
                current = member.Expression;
            }

            var tail = members.Count == 0 ? string.Empty : $":{string.Join(":", members)}";
            return $"{typeof(T).Name}{tail}";
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

        public static implicit operator InputOverrides(Override<T> context) => context.Overrides;

    }
}
