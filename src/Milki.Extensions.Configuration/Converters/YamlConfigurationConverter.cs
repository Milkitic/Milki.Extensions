using Milki.Extensions.Configuration.Internal;
using Milki.Extensions.Configuration.Internal.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Milki.Extensions.Configuration.Converters;

public class YamlConfigurationConverter : IConfigurationConverter
{
    public virtual object DeserializeSettings(string content, Type type)
    {
        var builder = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .IgnoreFields();
        ConfigDeserializeBuilder(builder);
        var list = ConfigTagMapping();
        if (list != null) InnerConfigTagMapping(list, builder);

        var ymlDeserializer = builder.Build();

        return ymlDeserializer.Deserialize(content, type)!;
    }

    public virtual string SerializeSettings(object obj)
    {
        var builder = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
            .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor))
            .DisableAliases()
            .IgnoreFields();
        ConfigSerializeBuilder(builder);
        var list = ConfigTagMapping();
        if (list != null) InnerConfigTagMapping(list, builder);
        var converter = builder.Build();
        var content = converter.Serialize(obj);
        return content;
    }

    protected virtual void ConfigSerializeBuilder(SerializerBuilder builder)
    {
        builder.WithTypeConverter(new DateTimeOffsetConverter());
    }

    protected virtual void ConfigDeserializeBuilder(DeserializerBuilder builder)
    {
        builder.WithTypeConverter(new DateTimeOffsetConverter());
    }

    protected virtual List<Type>? ConfigTagMapping()
    {
        return null;
    }

    private static void InnerConfigTagMapping<TBuilder>(IEnumerable<Type> list, BuilderSkeleton<TBuilder> builder)
        where TBuilder : BuilderSkeleton<TBuilder>
    {
        foreach (var type in list)
        {
            var convert = PascalCaseNamingConvention.Instance.Apply(GetStandardGenericName(type));
            var url = "tag:yaml.org,2002:" + convert;
            builder.WithTagMapping(url, type);
        }
    }

    private static string GetStandardGenericName(Type type)
    {
        // e.g.: System.Collection.Generic.List`1[System.String] => System.Collection.Generic.List<System.String>

        if (!type.IsGenericType) return type.FullName!;

        var genericType = type.GetGenericTypeDefinition();
        string? fullName = genericType.FullName;

        Span<char> span = stackalloc char[128];
        using var vsb = new ValueStringBuilder(span);

        var index = fullName?.IndexOf('`');
        if (index >= 0)
        {
            vsb.Append(fullName!.Substring(0, index.Value));
        }
        else
        {
            vsb.Append(fullName);
        }

        vsb.Append('(');
        var args = type.GetGenericArguments();

        for (var i = 0; i < args.Length; i++)
        {
            var innerType = args[i];
            vsb.Append(GetStandardGenericName(innerType));
            if (i != args.Length - 1)
            {
                vsb.Append(',');
            }
        }

        vsb.Append(')');
        return vsb.ToString();

        //var result = fullName + "<" + string.Join(",", args.Select(GetStandardGenericName)) + ">";
        //return result;
    }
}