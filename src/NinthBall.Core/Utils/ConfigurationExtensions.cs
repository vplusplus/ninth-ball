
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
        public static IConfigurationBuilder AddRequiredYamlFile(this IConfigurationBuilder builder, string yamlFileName) => 
            File.Exists(yamlFileName)
                ? builder.AddYamlContent(File.ReadAllText(yamlFileName)) 
                : throw new FatalWarning($"Yaml config file not found | {Path.GetFullPath(yamlFileName)}");

        public static IConfigurationBuilder AddOptionalYamlFile(this IConfigurationBuilder builder, string? yamlFileName) =>
            !string.IsNullOrWhiteSpace(yamlFileName) && File.Exists(yamlFileName)
                ? builder.AddYamlContent(File.ReadAllText(yamlFileName))
                : builder;

        public static IConfigurationBuilder AddYamlResources(this IConfigurationBuilder builder, Assembly resourceAssembly, string resourcePathSelector)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(resourceAssembly);
            ArgumentNullException.ThrowIfNull(resourcePathSelector);

            var chosenResourceNames = resourceAssembly
                .GetManifestResourceNames()
                .Where(name => null != name)
                .Where(name => name.Contains(resourcePathSelector, StringComparison.OrdinalIgnoreCase))
                .Where(name => name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (0 == chosenResourceNames.Count) throw new FatalWarning($"Zero resources found | Assembly: {resourceAssembly.GetName().Name} | **{resourcePathSelector}**.yaml");

            foreach (var resourceName in chosenResourceNames) builder.AddYamlResource(resourceAssembly, resourceName);

            return builder;
        }

        static IConfigurationBuilder AddYamlResource(this IConfigurationBuilder builder, Assembly resourceAssembly, string exactResourceName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(resourceAssembly);
            ArgumentNullException.ThrowIfNull(exactResourceName);

            using var resStream = resourceAssembly.GetManifestResourceStream(exactResourceName)
                ?? throw new Exception($"Missing or invalid YAML resource | Assembly: {resourceAssembly.GetName().Name} | '{exactResourceName}'");

            using var reader = new StreamReader(resStream);
            var yamlText = reader.ReadToEnd();
            return builder.AddYamlContent(yamlText);
        }

        static IConfigurationBuilder AddYamlContent(this IConfigurationBuilder builder, string yamlContent)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(yamlContent);

            // Convert YAML content to JSON text
            // Patch string formatted numbers and string formatted percentage values.
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
            // RegisterConfigSection() will ensure that the section exists and if present is valid.

            // Type name is the section name if not provided.
            var sectionName = string.IsNullOrWhiteSpace(optionalSectionName) ? typeof(TSection).Name : optionalSectionName.Trim();

            // Register validated TSection
            services.AddSingleton<TSection>(sp =>
                sp.GetRequiredService<IConfiguration>().ReadAndValidateRequiredSestion<TSection>(optionalSectionName)
            );

            // Done.
            return services;
        }

        public static TSection ReadAndValidateRequiredSestion<TSection>(this IConfiguration configuration, string? optionalSectionName = null) where TSection: class
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var sectionName = string.IsNullOrWhiteSpace(optionalSectionName) ? typeof(TSection).Name : optionalSectionName.Trim();
            var configSection = configuration.GetSection(sectionName);
            var options = configSection.Exists() ? configSection.Get<TSection>() : null;

            return null != options ? Validate(options, sectionName) : throw new Exception($"Missing config section | '{sectionName}'");

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

 