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
        object source, object overrides = null, TDestination destination = null, IEnumerable<string> ignoreProperties = null)
        where TDestination : class, new()
    {
        if (source == null)
            return null;

        var propertyDataMap = LightObjectMapper.GetPropertyDataMap(source.GetType(), typeof(TDestination));
        return LightObjectMapper.MapObject<TDestination>(source, propertyDataMap, overrides, destination, ignoreProperties);
    }

    public static IEnumerable<TDestination> MapObjects<TSource, TDestination>(
        IEnumerable<TSource> sourceEnumerable, Func<TSource, object> overridesGetter = null, IEnumerable<string> ignoreProperties = null)
        where TDestination : class, new()
    {
        if (sourceEnumerable == null)
            return null;

        var destination = new List<TDestination>(sourceEnumerable.Count());

        var propertyDataMap = LightObjectMapper.GetPropertyDataMap(typeof(TSource), typeof(TDestination));
        foreach (var sourceElem in sourceEnumerable)
        {
            var overrides = overridesGetter?.Invoke(sourceElem);
            var destinationElem = LightObjectMapper.MapObject<TDestination>(sourceElem, propertyDataMap, overrides, null, ignoreProperties);
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

    // returns property name to property data map
    private static IDictionary<string, PropertyData> GetPropertyDataMap(Type sourceType, Type destinationType)
    {
        var sourcePropertyInfos = sourceType.GetProperties();
        var destinationPropertyInfos = destinationType.GetProperties();

        return destinationPropertyInfos.ToDictionary(t => t.Name, t =>
            new PropertyData
            {
                destinationPropertyInfo = t,
                sourcePropertyInfo = sourcePropertyInfos.FirstOrDefault(u => u.Name == t.Name)
            }
        );
    }

    private static TDestination MapObject<TDestination>(
        object source, IDictionary<string, PropertyData> propertyDataMap, object overrides = null,
        TDestination destination = null, IEnumerable<string> ignoreProperties = null)
        where TDestination : class, new()
    {
        if (source == null)
            return null;

        destination = destination ?? new TDestination();

        foreach (var propertyDataKvp in propertyDataMap)
        {
            var propertyName = propertyDataKvp.Key;
            var propertyData = propertyDataKvp.Value;

            // property ignores
            if (ignoreProperties != null && ignoreProperties.Contains(propertyName))
                continue;

            // get from override or source object
            object sourcePropertyValue;
            if (!LightObjectMapper.GetFromOverrides(overrides, propertyName, out sourcePropertyValue))
                sourcePropertyValue = propertyData.sourcePropertyInfo?.GetValue(source);

            if (sourcePropertyValue == null)
                continue;

            // apply type converter
            Func<object, object> typeConverter;
            if (LightObjectMapper.typeConverters.TryGetValue(sourcePropertyValue.GetType(), out typeConverter) && typeConverter != null)
                sourcePropertyValue = typeConverter(sourcePropertyValue);

            // set in destination object
            propertyData.destinationPropertyInfo.SetValue(destination, sourcePropertyValue);
        }

        return destination;
    }
    private static bool GetFromOverrides(object overrides, string propertyName, out object out_propertyValue)
    {
        out_propertyValue = null;

        if (overrides == null)
            return false;

        if (overrides is IDictionary<string, object> propertyMapDict)
        {
            return propertyMapDict.TryGetValue(propertyName, out out_propertyValue);
        }
        else
        {
            var overridePropertyInfo = overrides.GetType().GetProperty(propertyName);
            if (overridePropertyInfo == null)
                return false;

            out_propertyValue = overridePropertyInfo.GetValue(overrides);
            return true;
        }
    }
}
