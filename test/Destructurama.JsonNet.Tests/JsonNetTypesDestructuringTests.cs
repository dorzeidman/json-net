using System.Collections.Generic;
using System.Linq;
using Destructurama.JsonNet.Tests.Support;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Destructurama.JsonNet.Tests
{
    class HasName
    {
        public string Name { get; set; }
    }

    public class JsonNetTypesDestructuringTests
    {
        [Fact]
        public void AttributesAreConsultedWhenDestructuring()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.JsonNetTypes()
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var test = new
            {
                HN = new HasName { Name = "Some name" },
                Arr = new[] { 1, 2, 3 },
                S = "Some string",
                D = new Dictionary<int, string> { { 1, "One" }, { 2, "Two" } },
                E = (object)null
            };

            var ser = JsonConvert.SerializeObject(test, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            var dyn = JsonConvert.DeserializeObject<dynamic>(ser);

            log.Information("Here is {@Dyn}", dyn);

            var sv = (StructureValue)evt.Properties["Dyn"];
            var props = sv.Properties.ToDictionary(p => p.Name, p => p.Value);

            Assert.IsType<StructureValue>(props["HN"]);
            Assert.IsType<SequenceValue>(props["Arr"]);
            Assert.IsType<string>(props["S"].LiteralValue());
            Assert.Null(props["E"].LiteralValue());

            // Not currently handled correctly - will serialize as a structure
            // Assert.IsInstanceOf<DictionaryValue>(props["D"]);
        }

        [Fact]
        public void CheckIgnoreExactMatch_JObject()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.JsonNetTypes("Password")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var test = new
            {
                password = "PW",
                Name = "Dor"
            };

            var ser = JsonConvert.SerializeObject(test, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            var dyn = JsonConvert.DeserializeObject<dynamic>(ser);

            log.Information("Here is {@Dyn}", dyn);

            var sv = (StructureValue)evt.Properties["Dyn"];
            var props = sv.Properties.ToDictionary(p => p.Name, p => p.Value);

            Assert.False(props.ContainsKey("password"));
            Assert.True(props.ContainsKey("Name"));
        }

        [Fact]
        public void CheckIgnoreExactMatch_JArray()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.JsonNetTypes("Password")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var test = new object[]
            {
                new {
                    password = "PW",
                    Name = "Dor"
                },
                new {
                    password = "PW2",
                    Name = "Dor2"
                }
            };

            var ser = JsonConvert.SerializeObject(test, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            var dyn = JsonConvert.DeserializeObject<dynamic>(ser);

            log.Information("Here is {@Dyn}", dyn);

            var sv = (SequenceValue)evt.Properties["Dyn"];
            foreach (StructureValue item in sv.Elements)
            {
                Assert.DoesNotContain(item.Properties, x => x.Name == "password");
            }
        }

        [Fact]
        public void CheckIgnoreLikeMatch_JObject()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.JsonNetTypes("*password*")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var test = new
            {
                password = "PW",
                OldPassword = "old",
                Name = "Dor"
            };

            var ser = JsonConvert.SerializeObject(test, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            var dyn = JsonConvert.DeserializeObject<dynamic>(ser);

            log.Information("Here is {@Dyn}", dyn);

            var sv = (StructureValue)evt.Properties["Dyn"];
            var props = sv.Properties.ToDictionary(p => p.Name, p => p.Value);

            Assert.False(props.ContainsKey("password"));
            Assert.False(props.ContainsKey("OldPassword"));
            Assert.True(props.ContainsKey("Name"));
        }

        [Fact]
        public void CheckIgnoreLikeMatch_JArray()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.JsonNetTypes("pass*")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var test = new object[]
            {
                new {
                    password = "PW",
                    pass = "pass",
                    Name = "Dor"
                },
                new {
                    password = "PW2",
                    pass = "pass",
                    Name = "Dor2"
                }
            };

            var ser = JsonConvert.SerializeObject(test, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            var dyn = JsonConvert.DeserializeObject<dynamic>(ser);

            log.Information("Here is {@Dyn}", dyn);

            var sv = (SequenceValue)evt.Properties["Dyn"];
            foreach (StructureValue item in sv.Elements)
            {
                Assert.DoesNotContain(item.Properties, x => x.Name == "password");
            }
        }
    }
}
