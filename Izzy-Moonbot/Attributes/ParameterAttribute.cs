using System;

namespace Izzy_Moonbot.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ParameterAttribute : Attribute
{
     public string Name { get; }
     public ParameterType Type { get; }
     public string Summary { get; }
     public bool Optional { get; }

     public ParameterAttribute(string name, ParameterType type, string summary)
     {
         Name = name;
         Type = type;
         Summary = summary;
         Optional = false;
     }
     
     public ParameterAttribute(string name, ParameterType type, string summary, bool optional)
     {
         Name = name;
         Type = type;
         Summary = summary;
         Optional = optional;
     }
}

public enum ParameterType
{
    Boolean,
    Character,
    String,
    Integer,
    Double,
    User,
    Role,
    Channel,
    Time
}