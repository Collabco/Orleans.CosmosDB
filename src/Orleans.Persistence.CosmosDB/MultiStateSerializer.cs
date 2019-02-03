using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Orleans.Persistence.CosmosDB.Models;
using Orleans.Runtime;

namespace Orleans.Persistence.CosmosDB
{
    internal class MultiStateSerializer
    {
        private readonly CosmosDBStorageOptions _options;

        private readonly IHashFunction _hasher;

        public MultiStateSerializer(CosmosDBStorageOptions options, IHashFunction hasher)
        {
            _options = options ?? throw new NullReferenceException(nameof(options));
            _hasher = hasher ?? throw new NullReferenceException(nameof(hasher)); ;
        }

        // TODO: Will need lots of validation in the actual implementation
        public object Deserialize(Type stateType, GrainReference grainReference, GrainStateEntity[] stateValues)
        {
            var result = Activator.CreateInstance(stateType);

            // 2. Loop through all GrainStatEntities and assign them as properties
            foreach (var entity in stateValues)
            {
                if (entity.Id.Contains("."))
                {
                    // 2.1 If the property name contains a dot, this is a key-value collection
                    var nameParts = entity.Id.Split('.');

                    var property = stateType.GetProperty(nameParts[0]);

                    var collection = property.GetValue(result);

                    // If the property is null, create a new instance
                    if (collection == null)
                    {
                        collection = Activator.CreateInstance(property.PropertyType);
                        property.SetValue(result, collection);
                    }

                    // Deserialise the state
                    var typeofT = property.PropertyType.GetGenericArguments()[1];
                    var value = JsonConvert.DeserializeObject(entity.State.ToString(), typeofT, _options.JsonSerializerSettings);

                    // Determine the dictionary Key
                    var key = string.Join(".", nameParts.Skip(1));

                    // Add the value to the collection
                    var dictionary = (IDictionary)collection;
                    dictionary.Add(key, value);
                }
                else
                {
                    // 2.2 If the property name doesn't contain a dot, it's a top-level value
                    var property = stateType.GetProperty(entity.Id);

                    if (property != null /* TODO: Ensure property should be read */)
                    {
                        var value = JsonConvert.DeserializeObject(entity.State.ToString(), property.PropertyType, _options.JsonSerializerSettings);
                        property.SetValue(result, value);
                    }
                }

                // Store the hash of the value in the StateCache
                var cacheKey = GetCacheKey(grainReference, entity.Id);
                var hash = _hasher.CalculateHash(entity.State.ToString());

                StateCache[cacheKey] = hash;
            }

            // 3. Return the result
            return result;
        }

        private static ConcurrentDictionary<string, ulong> StateCache = new ConcurrentDictionary<string, ulong>();

        private string GetCacheKey(GrainReference grainReference, string propertyKey) => grainReference?.ToKeyString() + "__" + propertyKey;

        // This will need to handle previously null values being set as null
        public GrainStateEntity[] Serialize(object state)
        {
            // 1. Get a list of all top-level properties
            var properties = state.GetType().GetProperties();

            // 2. Loop through each Property and serialise it to a GrainStateEntity
            var result = new List<GrainStateEntity>();

            foreach (var property in properties)
            {
                var value = property.GetValue(state);
                var type = property.PropertyType;

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    // 2.1 If this is a Dictionary, serialize each key individually
                    var dictionary = (IDictionary) value;

                    foreach (var k in dictionary.Keys)
                    {
                        // The key is the parent property name and the key
                        result.Add(new GrainStateEntity
                        {
                            Id = property.Name + "." + k,
                            State = JsonConvert.SerializeObject(dictionary[k], _options.JsonSerializerSettings)
                        });
                    }

                }
                else
                {
                    // 2.2 If this is a standard object, serialise it to the result
                    result.Add(new GrainStateEntity
                    {
                        Id = property.Name,
                        State = JsonConvert.SerializeObject(value, _options.JsonSerializerSettings)
                    });
                }
            }

            // 3. Return the result
            return result.ToArray();
        }
    }
}
