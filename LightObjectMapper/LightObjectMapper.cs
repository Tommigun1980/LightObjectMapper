/**
 * Maps from one object type to another, with optional override properties.
 * 
 * This is convenient to avoid code duplication when for example converting
 * business objects to DTOs. This is an extremely lightweight library that
 * does one thing and does it well.
 * 
 * Example: Map from a business object to a DTO:
 * var someDTO = LightObjectMapper.MapObject<DTOType>(someBusinessObject);
 * 
 * Example: Map from a business object to a DTO, while patching in some override
 * values (that may or may not exist in source object):
 * var someDTO = LightObjectMapper.MapObject<DTOType>(someBusinessObject, new
 * {
 *      SomeProperty = 4,
 *      OtherProperty = "Hello"
 * });
 * 
 * Also works with collections:
 * DTOType[] someDTOs = LightObjectMapper.MapObjects<SourceType, DTOType>(someBusinessObjectEnumerable);
 * 
 * Collection with overrides:
 * var someDTOs = LightObjectMapper.MapObjects<SourceType, DTOType>(someBusinessObjectEnumerable, (businessObject) =>
 * new
 * {
 *      SomeProperty = businessObject.Something.Select(t => t.SomeValue);
 * });
 * 
 * Advanced, optional: Type converters can also be registered.
 * ObjectMap.RegisterTypeConverter(typeof(BusinessEnum), (enumValue) => mapToOtherEnum);
 * Whenever a BusinessEnum is encountered, it will be automatically converted to mapToOtherEnum's
 * value and type, before being assigned into the destination object. Overrides still apply.
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public static class LightObjectMapper
{
    private static readonly IDictionary<Type, Func<object, object>> typeConverters =
        new Dictionary<Type, Func<object, object>>();

    public static TDestination MapObject<TDestination>(
        object source, object propertyMap = null, TDestination destination = null, IEnumerable<string> ignoreProperties = null)
        where TDestination : class, new()
    {
        if (source == null)
            return null;

        var destinationPropertyNameToPropertyDataMap = LightObjectMapper.GetPropertyDataMap<TDestination>(source.GetType());
        return LightObjectMapper.MapObject<TDestination>(source, propertyMap, destinationPropertyNameToPropertyDataMap, destination, ignoreProperties);
    }

    public static IEnumerable<TDestination> MapObjects<TSource, TDestination>(
        IEnumerable<TSource> sourceEnumerable, Func<TSource, object> propertyMapGetter = null, IEnumerable<string> ignoreProperties = null)
        where TDestination : class, new()
    {
        if (sourceEnumerable == null)
            return null;

        var destination = new List<TDestination>(sourceEnumerable.Count());

        var destinationPropertyNameToPropertyDataMap = LightObjectMapper.GetPropertyDataMap<TDestination>(typeof(TSource));
        foreach (var sourceElem in sourceEnumerable)
        {
            var propertyMap = propertyMapGetter?.Invoke(sourceElem);
            var destinationElem = LightObjectMapper.MapObject<TDestination>(sourceElem, propertyMap, destinationPropertyNameToPropertyDataMap, null, ignoreProperties);
            destination.Add(destinationElem);
        }

        return destination;
    }

    public static void RegisterTypeConverter(Type fromType, Func<object, object> converter)
    {
        LightObjectMapper.typeConverters[fromType] = converter;
    }

    private class PropertyData
    {
        public PropertyInfo destinationPropertyInfo;
        public PropertyInfo sourcePropertyInfo;
    }

    private static IDictionary<string, PropertyData> GetPropertyDataMap<TDestination>(Type sourceType)
        where TDestination : class, new()
    {
        var sourcePropertyInfos = sourceType.GetProperties();
        return typeof(TDestination).GetProperties().ToDictionary(t => t.Name, t =>
            new PropertyData
            {
                destinationPropertyInfo = t,
                sourcePropertyInfo = sourcePropertyInfos.FirstOrDefault(u => u.Name == t.Name)
            }
        );
    }

    private static TDestination MapObject<TDestination>(
        object source, object propertyMap, IDictionary<string, PropertyData> destinationPropertyNameToPropertyDataMap,
        TDestination destination = null, IEnumerable<string> ignoreProperties = null)
        where TDestination : class, new()
    {
        if (source == null)
            return null;

        destination = destination ?? new TDestination();

        foreach (var propertyDataKvp in destinationPropertyNameToPropertyDataMap)
        {
            var destinationPropertyName = propertyDataKvp.Key;
            if (ignoreProperties != null && ignoreProperties.Contains(destinationPropertyName))
                continue;

            var propertyData = propertyDataKvp.Value;

            object sourceObject = null;
            PropertyInfo sourcePropertyInfo = null;

            PropertyInfo overridePropertyInfo = propertyMap?.GetType().GetProperty(destinationPropertyName);
            if (overridePropertyInfo != null)
            {
                sourceObject = propertyMap;
                sourcePropertyInfo = overridePropertyInfo;
            }
            else
            {
                sourceObject = source;
                sourcePropertyInfo = propertyData.sourcePropertyInfo;
            }

            if (sourcePropertyInfo != null)
            {
                object sourcePropertyValue;
                try
                {
                    sourcePropertyValue = sourcePropertyInfo.GetValue(sourceObject);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Object mapper error when reading value from source property '{destinationPropertyName}'", e);
                }

                Func<object, object> typeConverter;
                if (LightObjectMapper.typeConverters.TryGetValue(sourcePropertyInfo.PropertyType, out typeConverter) && typeConverter != null)
                    sourcePropertyValue = typeConverter(sourcePropertyValue);

                try
                {
                    propertyData.destinationPropertyInfo.SetValue(destination, sourcePropertyValue);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Object mapper error when setting value to destination property '{destinationPropertyName}'", e);
                }
            }
        }

        return destination;
    }
}
