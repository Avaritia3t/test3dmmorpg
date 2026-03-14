using System;
using System.Collections.Generic;

public class ServiceLocator
{
    private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public void Register<T>(T instance) where T : class
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        var key = typeof(T);
        if (_services.ContainsKey(key))
            _services[key] = instance;
        else
            _services.Add(key, instance);
    }

    public T Get<T>() where T : class
    {
        var key = typeof(T);
        if (_services.TryGetValue(key, out var instance))
            return (T)instance;

        return null;
    }

    public bool TryGet<T>(out T service) where T : class
    {
        service = Get<T>();
        return service != null;
    }
}
