namespace Milki.Extensions.Configuration;

public interface IConfigurationConverter
{
    public object DeserializeSettings(string content, Type type);
    public string SerializeSettings(object obj);
}