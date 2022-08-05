﻿using Newtonsoft.Json;

namespace Milki.Extensions.Configuration.Converters;

public class JsonConfigurationConverter : IConfigurationConverter
{
    public object DeserializeSettings(string content, Type type)
    {
        var obj = JsonConvert.DeserializeObject(content, type,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            });
        return obj!;
    }

    public string SerializeSettings(object obj)
    {
        var content = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        });
        return content;
    }
}