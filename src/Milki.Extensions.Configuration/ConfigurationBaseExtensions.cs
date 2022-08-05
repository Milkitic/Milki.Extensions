namespace Milki.Extensions.Configuration;

public static class ConfigurationBaseExtensions
{
    public static void Save(this IConfigurationBase configurationBase)
    {
        ConfigurationFactory.Save(configurationBase);
    }
}