# LightObjectMapper
*A lightweight object mapper for .NET*

NuGet package available at https://www.nuget.org/packages/LightObjectMapper/

## Intro

Maps from one object type to another, including collections, with optional override properties. This is convenient to avoid code duplication when for example converting business objects to DTOs. This is an extremely lightweight library that does one thing and does it well.

## Usage

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
