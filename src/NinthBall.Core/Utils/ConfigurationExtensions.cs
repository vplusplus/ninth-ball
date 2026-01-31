
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NinthBall.Utils;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace NinthBall.Core
{
    public static class ConfigurationExtensions
    {
        //......................................................................
        #region Can register YAML file or YAML text fragment to IConfiguration
        //......................................................................
        public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string yamlFileName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(yamlFileName);

            return File.Exists(yamlFileName)
                ? builder.AddYamlContent(File.ReadAllText(yamlFileName)) 
                : throw new FatalWarning($"Yaml config file not found | {Path.GetFullPath(yamlFileName)}");
        }

        public static IConfigurationBuilder AddYamlResources(this IConfigurationBuilder builder, Assembly resourceAssembly, string resourcePathSelector)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(resourceAssembly);
            ArgumentNullException.ThrowIfNull(resourcePathSelector);

            var simOutputDefaultsResourceNames = resourceAssembly
                .GetManifestResourceNames()
                .Where(name => null != name)
                .Where(name => name.Contains(resourcePathSelector, StringComparison.OrdinalIgnoreCase))
                .Where(name => name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (0 == simOutputDefaultsResourceNames.Count) throw new FatalWarning($"Zero resources found | Assembly: {resourceAssembly.GetName().Name} | **{resourcePathSelector}**.yaml");

            foreach (var res in simOutputDefaultsResourceNames) builder.AddYamlResource(resourceAssembly, res);

            return builder;
        }

        public static IConfigurationBuilder AddYamlResource(this IConfigurationBuilder builder, Assembly resourceAssembly, string resourceNameEndsWith)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(resourceAssembly);
            ArgumentNullException.ThrowIfNull(resourceNameEndsWith);

            var resourceName = resourceAssembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(resourceNameEndsWith, StringComparison.OrdinalIgnoreCase))
                ?? throw new Exception($"YAML resource not found | Assembly: {resourceAssembly.GetName().Name} | Resource: {resourceNameEndsWith}");

            using var resStream = resourceAssembly.GetManifestResourceStream(resourceName)
                ?? throw new Exception($"YAML resource stream was NULL | Assembly: {resourceAssembly.GetName().Name} | Resource: {resourceNameEndsWith}");

            using var reader = new StreamReader(resStream);
            var yamlText = reader.ReadToEnd();
            return builder.AddYamlContent(yamlText);
        }

        static IConfigurationBuilder AddYamlContent(this IConfigurationBuilder builder, string yamlContent)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(yamlContent);

            // Convert YAML content to JSON text
            var jsonContent = NinthBall.Utils.Yaml2Json.YamlTextToJsonText(yamlContent)
                .AsJsonObject()
                .PatchNumbersAndPercentage()
                .ToJsonString();

            return builder.AddJsonStream(
                new MemoryStream(
                    Encoding.UTF8.GetBytes(jsonContent)
            ));
        }

        #endregion

        //......................................................................
        #region RegisterConfigSection() - Opinionated alternate for Options pattern
        //......................................................................
        /// <summary>
        /// Registers a required config section.
        /// </summary>
        public static IServiceCollection RegisterConfigSection<TSection>(this IServiceCollection services, string? optionalSectionName = null) where TSection : class
        {
            // Options pattern returns an empty and uninitialized TOption if section not defined.
            // This is not our desired behavior.
            // Resolving TSection will throw an exception if section not defined or section is invalid.

            // Type name is the section name if not provided.
            var sectionName = string.IsNullOrWhiteSpace(optionalSectionName) ? typeof(TSection).Name : optionalSectionName.Trim();

            // Register TSection
            services.AddSingleton<TSection>(sp =>
            {
                // Look for requested config section
                var configSection = sp.GetRequiredService<IConfiguration>().GetSection(sectionName);

                // Parse TSection if config section is present, use null if not present
                var options = configSection.Exists() ? configSection.Get<TSection>() : null;

                // If available, ensure it is valid before serving the instance.
                return null != options ? Validate(options, sectionName) : throw new Exception($"Missing config section | '{sectionName}'");
            });

            // Done.
            return services;

            static TSomething Validate<TSomething>(TSomething options, string sectionName) where TSomething : class
            {
                var context = new ValidationContext(options);
                var results = new List<ValidationResult>();

                if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
                {
                    var messages = string.Join(Environment.NewLine, results.Select(x => x.ErrorMessage));
                    throw new ValidationException($"Invalid config section | '{sectionName}':{Environment.NewLine}{messages}");
                }

                return options;
            }
        }

        #endregion

    }
}

 