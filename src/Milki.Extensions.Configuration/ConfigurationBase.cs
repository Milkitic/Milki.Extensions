namespace Milki.Extensions.Configuration;

public abstract class ConfigurationBase
{
    public void Save()
    {
        ConfigurationFactory.Save(this);
    }
}