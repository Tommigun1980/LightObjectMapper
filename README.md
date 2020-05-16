# LightObjectMapper
*A lightweight object mapper for .NET*

NuGet package available at https://www.nuget.org/packages/LightObjectMapper/

## Intro

Maps from one object type to another, including collections, with optional override properties. This is convenient to avoid code duplication when for example converting business objects to DTOs. This is an extremely lightweight library that does one thing and does it well.

## Examples

LightObjectMapper copies all public properties from source object to destination object. The property types must match (see 'Optional automatic property type converters' for an exception to this rule).

Map from a business object to a DTO:

```c#
var someDTO = LightObjectMapper.MapObject<DTOType>(someBusinessObject);
```

Map from a business object to a DTO, while patching in some override values (that may or may not exist in source object):

```c#
var someDTO = LightObjectMapper.MapObject<DTOType>(someBusinessObject, new
{
     SomeProperty = 4,
     OtherProperty = "Hello"
});
```

Also works with collections:

```c#
DTOType[] someDTOs = LightObjectMapper.MapObjects<SourceType, DTOType>(someBusinessObjectEnumerable);
```

Collection with overrides:

```c#
var someDTOs = LightObjectMapper.MapObjects<SourceType, DTOType>(someBusinessObjectEnumerable, (businessObject) =>
new
{
     SomeProperty = businessObject.Something.Select(t => t.SomeValue);
});
```

### Optional automatic property type converters

Optionally, property type converters can also be registered. This can be useful if the some properties are conversible but of different types, such as different types of enumerations that are conversible.

To use this feature, register the type converter once:

```c#
LightObjectMapper.RegisterTypeConverter(typeof(BusinessEnum), (enumValue) => mapToOtherEnum);
```

Now, whenever a BusinessEnum is encountered in the mapping process, it will be automatically converted to the value specified by your converter. Overrides will still apply.
