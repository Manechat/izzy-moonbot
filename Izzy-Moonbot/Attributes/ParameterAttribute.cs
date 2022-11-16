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

     public override string ToString()
     {
         var typeName = Type switch
         {
             ParameterType.Boolean => "Boolean",
             ParameterType.Character => "Character",
             ParameterType.String => "String",
             ParameterType.Integer => "Integer",
             ParameterType.Double => "Decimal Number",
             ParameterType.User => "User",
             ParameterType.Role => "Role",
             ParameterType.Channel => "Channel",
             ParameterType.DateTime => "Date/Time",
             _ => "Unknown"
         };
         return $"{Name} [{typeName}]{(Optional ? " {OPTIONAL}" : "")} - {Summary}";
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
    DateTime
}