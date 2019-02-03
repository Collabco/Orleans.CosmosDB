using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Orleans.Persistence.CosmosDB;
using Orleans.Persistence.CosmosDB.Models;
using Orleans.Runtime;
using Xunit;
using Xunit.Sdk;

namespace Orleans.CosmosDB.Tests
{
    public class StateSerializerTests
    {
        [Fact]
        public void MultiStateSerializer_Deserialise()
        {
            // 1. Arrange
            var options = new CosmosDBStorageOptions
            {
                JsonSerializerSettings = new JsonSerializerSettings()
            };
            var hasher = new HashFunction();
            var serializer = new MultiStateSerializer(options, hasher);

            var stateType = typeof(TestState);
            var stateValues = GetTestData();
            
            // 2. Act
            var result = serializer.Deserialize(stateType, null, stateValues);

            // 3. Assert
            var state = result as TestState;

            Assert.NotNull(state);
            Assert.Equal("TestString", state.Foo);
            Assert.Equal(42, state.Bar);
            Assert.Equal(2, state.Baz?.Count);
            Assert.True(state.Baz["One"].FooBar);
            Assert.False(state.Baz["Two"].FooBar);
        }

        [Fact]
        public void MultiStateSerializer_Serialise()
        {
            // 1. Arrange
            var options = new CosmosDBStorageOptions
            {
                JsonSerializerSettings = new JsonSerializerSettings()
            };
            var hasher = new HashFunction();
            var serializer = new MultiStateSerializer(options, hasher);

            var state = new TestState
            {
                Foo = "TestString",
                Bar = 42,
                Baz = new Dictionary<string, TestLookupState>
                {
                    {
                        "One", new TestLookupState
                        {
                            FooBar = true
                        }
                    },
                    {
                        "Two", new TestLookupState
                        {
                            FooBar = false
                        }
                    },
                }
            };

            // 2. Act
            var result = serializer.Serialize(state);

            // 3. Assert
            Assert.NotNull(result);
        }

        private GrainStateEntity[] GetTestData()
        {
            return new []
            {
                new GrainStateEntity
                {
                    Id = "Foo",
                    State = "\"TestString\""
                },
                new GrainStateEntity
                {
                    Id = "Bar",
                    State = 42
                },
                new GrainStateEntity
                {
                    Id = "Baz.One",
                    State = "{\"fooBar\": true}"
                },
                new GrainStateEntity
                {
                    Id = "Baz.Two",
                    State = "{\"fooBar\": false}"
                },
            };
        }
    }

    public class TestState
    {
        public string Foo { get; set; }

        public int Bar { get; set; }

        public Dictionary<string, TestLookupState> Baz { get; set; }
    }

    public class TestLookupState
    {
        public bool FooBar { get; set; }
    }
}
