namespace Milki.Extensions.Configuration;

public class EncodingAttribute : Attribute
{
    public string EncodingString { get; }

    public EncodingAttribute(string encodingString)
    {
        EncodingString = encodingString;
    }
}