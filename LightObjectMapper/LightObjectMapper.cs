using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public static class LightObjectMapper
{
    private static readonly IDictionary<Type, Func<object, object>> typeConverters =
        new Dictionary<Type, Func<object, object>>();

    public static TDestination MapObject<TDestination>(
        object source, TDestination destination, object overrides = null, IEnumerable<string> ignoreProperties = null, bool ignoreSourceNullValues = false)
        where TDestination : class, new()
    {
        if (source == null)
            return null;

        var propertyDataMap = LightObjectMapper.GetPropertyDataMap(source.GetType(), typeof(TDestination));
        return LightObjectMapper.MapObject<TDestination>(source, propertyDataMap, overrides, destination, ignoreProperties, ignoreSourceNullValues);
    }
    public static TDestination MapObject<TDestination>(
        object source, object overrides = null, IEnumerable<string> ignoreProperties = null)
        where TDestination : class, new()
    {
        return LightObjectMapper.MapObject<TDestination>(source, null, overrides, ignoreProperties, true);
    }

    public static IReadOnlyCollection<TDestination> MapObjects<TSource, TDestination>(
        IEnumerable<TSource> sourceEnumerable, Func<TSource, object> overridesGetter = null, IEnumerable<string> ignoreProperties = null, bool ignoreSourceNullValues = true)
        where TDestination : class, new()
    {
        if (sourceEnumerable == null)
            return null;

        var destination = new List<TDestination>(sourceEnumerable.Count());

        var propertyDataMap = LightObjectMapper.GetPropertyDataMap(typeof(TSource), typeof(TDestination));
        foreach (var sourceElem in sourceEnumerable)
        {
            var overrides = overridesGetter?.Invoke(sourceElem);
            var destinationElem = LightObjectMapper.MapObject<TDestination>(sourceElem, propertyDataMap, overrides, null, ignoreProperties, ignoreSourceNullValues);
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

        return destinationPropertyInfos
            .ToDictionary(
                t => t.Name,
                t => new PropertyData
                {
                    destinationPropertyInfo = t,
                    sourcePropertyInfo = sourcePropertyInfos.FirstOrDefault(u => u.Name == t.Name)
                }
        );
    }

    private static TDestination MapObject<TDestination>(
        object source, IDictionary<string, PropertyData> propertyDataMap, object overrides = null,
        TDestination destination = null, IEnumerable<string> ignoreProperties = null, bool ignoreSourceNullValues = true)
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

            // get from overrides
            object sourcePropertyValue;
            if (!LightObjectMapper.GetFromOverrides(overrides, propertyName, out sourcePropertyValue))
            {
                // not in overrides
                if (propertyData.sourcePropertyInfo == null) // not in source property either, ignore
                    continue;

                sourcePropertyValue = propertyData.sourcePropertyInfo.GetValue(source); // fetch from source property
                if (sourcePropertyValue == null && ignoreSourceNullValues) // ignore if source null values are ignored
                    continue;
            }

            // apply type converter
            if (LightObjectMapper.typeConverters.Count > 0)
            {
                var sourceObjectType = sourcePropertyValue?.GetType() ?? propertyData.sourcePropertyInfo?.PropertyType;
                if (sourceObjectType != null)
                {
                    Func<object, object> typeConverter;
                    if (LightObjectMapper.typeConverters.TryGetValue(sourceObjectType, out typeConverter) && typeConverter != null)
                        sourcePropertyValue = typeConverter(sourcePropertyValue);
                }
            }

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
