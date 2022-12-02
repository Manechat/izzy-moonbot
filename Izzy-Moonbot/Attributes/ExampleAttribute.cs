using System;

namespace Izzy_Moonbot.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ExampleAttribute : Attribute
{
    public string Text { get; }

    public ExampleAttribute(string text)
    {
        Text = text;
    }

    public override string ToString()
    {
        return Text;
    }
}
