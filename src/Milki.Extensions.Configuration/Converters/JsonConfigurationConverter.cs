using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Milki.Extensions.Configuration.Converters;

public class JsonConfigurationConverter : IConfigurationConverter
{
    public virtual object DeserializeSettings(string content, 
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        Type type)
    {
        var obj = JsonConvert.DeserializeObject(content, type,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            });
        return obj!;
    }

    public virtual string SerializeSettings(object obj)
    {
        var content = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        });
        return content;
    }
}