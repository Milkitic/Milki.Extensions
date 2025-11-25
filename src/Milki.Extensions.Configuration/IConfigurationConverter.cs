using System.Diagnostics.CodeAnalysis;

namespace Milki.Extensions.Configuration;

public interface IConfigurationConverter
{
    public object DeserializeSettings(string content,
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        Type type);
    public string SerializeSettings(object obj);
}