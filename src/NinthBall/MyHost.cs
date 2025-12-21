
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NinthBall.Hosting
{
    internal static class MyHost
    {
        public static IHost DefineMyApp()
        {
            var appBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            appBuilder.Configuration.AddConfiguration(CmdLine.Current);

            appBuilder.Configuration
                .AddInputYaml()
                ;

            appBuilder.Services
                .RegisterMyConfigSections(appBuilder.Configuration)
                .AddMyStrategies(appBuilder.Configuration)
                .AddTransient<App>()
                ;
            return appBuilder.Build();
        }
        static IServiceCollection RegisterMyConfigSections(this IServiceCollection services, IConfiguration config)
        {
            // Mandatory configurations
            services.RegisterMyConfigSection<SimParams>();
            services.RegisterMyConfigSection<InitialBalance>();

            // Historical data and bootstrappers
            services.RegisterMyConfigSection<ROIHistory>();
            services.RegisterMyConfigSection<FlatBootstrap>();
            services.RegisterMyConfigSection<MovingBlockBootstrap>();
            services.RegisterMyConfigSection<ParametricBootstrap>();

            return services;
        }

        static IServiceCollection AddMyStrategies(this IServiceCollection services, IConfiguration config)
        {
            var strategyTypes = typeof(Program).Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(SimInputAttribute), false).Any());

            foreach (var type in strategyTypes)
            {
                var attr = (SimInputAttribute)type.GetCustomAttributes(typeof(SimInputAttribute), false).First();
                var sectionName = attr.OptionsType.Name;
                var section = config.GetSection(sectionName);

                if (section.Exists())
                {
                    // Register Options
                    services.AddSingleton(attr.OptionsType, sp => 
                    {
                        var cfg = sp.GetRequiredService<IConfiguration>();
                        var options = cfg.GetSection(sectionName).GetEx(attr.OptionsType);
                        return ThrowIfInvalidOption(options, attr.OptionsType);
                    });

                    // Register Strategy as ISimObjective
                    services.AddSingleton(typeof(ISimObjective), type);
                }
            }

            return services
                .AddSingleton<HistoricalReturns>()
                .AddSingleton<FlatBootstrapper>()
                .AddSingleton<SequentialBootstrapper>()
                .AddSingleton<MovingBlockBootstrapper>()
                .AddSingleton<ParametricBootstrapper>()

                .AddSingleton<SimulationSeed>()
                .AddSingleton<SimBuilder>()
                .AddSingleton<Simulation>()
                ;
        }

        static IConfigurationBuilder AddInputYaml(this IConfigurationBuilder builder)
        {
            const string MyPathTag = "$(MyPath)";


            // Read yaml text
            var yamlFileName = CmdLine.Required("in");
            var yamlText = File.ReadAllText(yamlFileName);

            // The $(MyPath) token represents directory of the current config file.
            // Config entries can reference $(MyPath) to locate related files.
            // If present, replace $(MyPath) with the actual path of this config file.
            if (yamlText.Contains(MyPathTag, StringComparison.OrdinalIgnoreCase))
            {
                var myPath = Path.GetFullPath(Path.GetDirectoryName(yamlFileName) ?? "./")
                    .Replace('\\', '/')
                    .TrimEnd('/');

                yamlText = yamlText.Replace(MyPathTag, myPath, StringComparison.OrdinalIgnoreCase);
            }

            var jsonText = YamlTextToJsonText(yamlText);

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonText));
            return builder.AddJsonStream(jsonStream);
        }

        public static string YamlTextToJsonText(string yamlInput)
        {
            var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();
            var yamlObject = yamlDeserializer.Deserialize<object>(new StringReader(yamlInput));
            var jsonText = System.Text.Json.JsonSerializer.Serialize(yamlObject, options: new() { WriteIndented = true });

            return jsonText;
        }

        /// <summary>
        /// Registers TOption that represents a slice of configuration with the DI system.
        /// Registers singleton instances of TOptions and Lazy<TOptions>.
        /// </summary>
        static IServiceCollection RegisterMyConfigSection<TOptions>(this IServiceCollection services) where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<TOptions>(sp =>
            {
                var configSectionName = typeof(TOptions).Name;
                var configuration = sp.GetRequiredService<IConfiguration>();
                var configSection = configuration.GetSection(configSectionName);
                
                return configSection.Exists() 
                    ? (TOptions)ThrowIfInvalidOption(configSection.GetEx<TOptions>(), typeof(TOptions)) 
                    : throw new FatalWarning($"ConfigSection not defined | '{configSectionName}'");
            });

            return services;
        }

        private static object ThrowIfInvalidOption(object something, Type optionsType)
        {
            if (null == something) throw new Exception($"{optionsType.Name} was NULL.");

            var context = new ValidationContext(something, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(something, context, results, validateAllProperties: true);
            if (isValid) return something;

            var csvValidationErrors = string.Join(Environment.NewLine, results.Select(x => x.ErrorMessage));
            var errMsg = $"One or more {optionsType.Name} properties were invalid. {Environment.NewLine}{csvValidationErrors}";
            throw new ValidationException(errMsg);
        }
    }
}
