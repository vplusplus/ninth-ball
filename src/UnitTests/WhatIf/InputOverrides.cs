using Microsoft.Extensions.Configuration;
using NinthBall.Core;
using System.Globalization;
using System.Linq.Expressions;

namespace UnitTests.WhatIf
{
    public sealed class InputOverrides : Dictionary<string, string?> { }

    public static class ConfigPath 
    {
        public static string GetPropertyPath<T, TProperty>(Expression<Func<T, TProperty>> expression)
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
    }

    public static class InputOverrideExtensions
    {

        public static InputOverrides InitialPreTaxAmount(this InputOverrides overrides, double value, IConfiguration baseConfig)
        {
            overrides[ConfigPath.GetPropertyPath<Initial, double>(x => x.PreTax.Amount)] = value.ToString(CultureInfo.InvariantCulture);
            return overrides;
        }

        public static InputOverrides InitialPreTaxAllocation(this InputOverrides overrides, double value, IConfiguration baseConfig)
        {
            overrides[ConfigPath.GetPropertyPath<Initial, double>(x => x.PreTax.Allocation)] = value.ToString(CultureInfo.InvariantCulture);
            return overrides;
        }

        public static InputOverrides InitialPostTaxAmount(this InputOverrides overrides, double value, IConfiguration baseConfig)
        {
            overrides[ConfigPath.GetPropertyPath<Initial, double>(x => x.PostTax.Amount)] = value.ToString(CultureInfo.InvariantCulture);
            return overrides;
        }

        public static InputOverrides InitialPostTaxAllocation(this InputOverrides overrides, double value, IConfiguration baseConfig)
        {
            overrides[ConfigPath.GetPropertyPath<Initial, double>(x => x.PostTax.Allocation)] = value.ToString(CultureInfo.InvariantCulture);
            return overrides;
        }

        public static InputOverrides WithObjective(this InputOverrides overrides, string value, IConfiguration baseConfig)
        {
            var pathPrefix = ConfigPath.GetPropertyPath<SimParams, IReadOnlyList<string>>(x => x.Objectives);

            var objectives = baseConfig.GetArrayValues(pathPrefix);
            objectives.Add(value);
            overrides.SetArrayValues(pathPrefix, objectives);
            
            return overrides;
        }


        private static List<string> GetArrayValues(this IConfiguration baseConfig, string pathPrefix)
        {
            return baseConfig
                .GetSection(pathPrefix)
                .GetChildren()
                .Select(child => child.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList()!;
        }

        private static void SetArrayValues(this InputOverrides overrides, string pathPrefix, IReadOnlyList<string> values)
        {
            for(int i=0; i<values.Count; i++)
            {
                overrides[$"{pathPrefix}:{i}"] = values[i];
            }
        }

    }

}
