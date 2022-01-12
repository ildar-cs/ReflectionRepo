using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Task1.DoNotChange;

namespace Task1
{
    public class Container
    {
        private Dictionary<Type, Type> keyValuePairs;

        public Container()
        {
            this.keyValuePairs = new Dictionary<Type, Type>();
        }

        public void AddAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass)
                {
                    var customAttributes = type.GetCustomAttributes(true);

                    if (customAttributes.Any(c => c is ImportConstructorAttribute) ||
                        customAttributes.Any(c => c is ExportAttribute) ||
                        type.GetProperties().Any(c => c.GetCustomAttributes().Any(a => a is ImportAttribute)))
                        AddType(type);
                }

            }
        }

        public void AddType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (this.keyValuePairs.Keys.Any(t => t.FullName.Equals(type.FullName)))
                throw new ArgumentException("The type has already been added.");

            var customAttributes = type.GetCustomAttributes(true);
            bool added = false;

            foreach (var attr in customAttributes)
            {
                if (attr is ExportAttribute attrObj)
                {
                    if (attrObj.Contract != null)
                    {
                        AddType(type, attrObj.Contract);
                        added = true;
                    }
                    else
                    {
                        AddType(type, type);
                        added = true;
                    }
                }
            }

            if (!added)
                AddType(type, type);
        }

        public void AddType(Type type, Type baseType)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            if (this.keyValuePairs.Keys.Any(t => t.FullName.Equals(type.FullName)))
                throw new ArgumentException($"Type {type.Name} has already been added.");

            if (!baseType.IsAssignableFrom(type) && !type.Equals(baseType))
                throw new ArgumentException($"Type {type.Name} does not implement {nameof(baseType)}.");

            this.keyValuePairs.Add(type, baseType);
        }

        public T Get<T>() where T : class
        {
            object obj = null;

            if (!this.keyValuePairs.Keys.Any(t => t.FullName.Equals(typeof(T).FullName)) &&
                !this.keyValuePairs.Values.Any(t => t.FullName.Equals(typeof(T).FullName)))
                throw new ArgumentException($"Type {typeof(T).Name} has not been added yet.");

            var mainType = typeof(T);
            var flags = BindingFlags.Public | BindingFlags.Instance;

            if (mainType.IsInterface)
                mainType = this.keyValuePairs.FirstOrDefault(t => t.Value.FullName.Equals(mainType.FullName)).Key;

            var constructors = mainType.GetConstructors(flags);

            if (constructors.Count() != 1)
                throw new TypeInitializationException(mainType.FullName, new Exception("Not found proper constructor."));

            var attributes = mainType.GetCustomAttributes(true);
            if (attributes.Any(a => a is ImportConstructorAttribute))
                obj = CreateInstanceInConstructor(mainType, constructors[0]);
            else
                obj = CreateInstance(mainType, flags);

            return (T)obj;
        }

        private object CreateInstance(Type mainType, BindingFlags flags)
        {
            object obj = Activator.CreateInstance(mainType);
            var properties = mainType.GetProperties(flags);
            foreach (var prop in properties)
            {
                object propObj = null;

                foreach (var propAttr in prop.CustomAttributes)
                {
                    if (propAttr.AttributeType.Equals(typeof(ImportAttribute)))
                    {
                        propObj = GetTypeInstance(prop.PropertyType);
                        break;
                    }
                }
                if (propObj != null)
                    prop.SetValue(obj, propObj, null);
            }

            return obj;
        }

        private object CreateInstanceInConstructor(Type mainType, ConstructorInfo constructor)
        {
            object instance;

            var parameters = constructor.GetParameters();
            var paramsForActivator = new object[parameters.Count()];
            for (int i = 0; i < parameters.Count(); i++)
            {
                var paramObj = GetTypeInstance(parameters[i].ParameterType);
                if (paramObj == null)
                    throw new TypeInitializationException(parameters[i].ParameterType.FullName,
                                        new Exception($"Type {parameters[i].ParameterType} has not been added yet to the container."));

                paramsForActivator[i] = paramObj;
            }
            instance = Activator.CreateInstance(mainType, paramsForActivator);
            return instance;
        }

        private object GetTypeInstance(Type type)
        {
            object instance = null;

            var keyValue = this.keyValuePairs.FirstOrDefault(t => t.Key.FullName.Equals(type.FullName) ||
                                                                  t.Value.FullName.Equals(type.FullName));

            instance = Activator.CreateInstance(keyValue.Key);

            return instance;
        }
    }
}