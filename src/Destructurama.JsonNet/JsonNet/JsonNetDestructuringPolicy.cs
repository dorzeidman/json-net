﻿// Copyright 2015 Destructurama Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Destructurama.JsonNet
{
    internal class JsonNetDestructuringPolicy : IDestructuringPolicy
    {
        private readonly HashSet<string> _ignoreExtactNames;
        private readonly string[] _ignoreLikeNames;

        public JsonNetDestructuringPolicy()
        {

        }

        public JsonNetDestructuringPolicy(params string[] ignoreNames)
        {
            var igNames = (ignoreNames ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x));

            _ignoreExtactNames = new HashSet<string>(igNames.Where(x => !x.StartsWith("*") && !x.EndsWith("*")),
                StringComparer.CurrentCultureIgnoreCase);

            _ignoreLikeNames = igNames.Where(x => x.StartsWith("*") || x.EndsWith("*"))
                .Select(x => x.ToLower())
                .ToArray();
        }

        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            switch (value)
            {
                case JObject jo:
                    result = Destructure(jo, propertyValueFactory);
                    return true;
                case JArray ja:
                    result = Destructure(ja, propertyValueFactory);
                    return true;
                case JValue jv:
                    result = Destructure(jv, propertyValueFactory);
                    return true;
            }

            result = null;
            return false;
        }

        LogEventPropertyValue Destructure(JValue jv, ILogEventPropertyValueFactory propertyValueFactory)
        {
            return propertyValueFactory.CreatePropertyValue(jv.Value, true);
        }

        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        LogEventPropertyValue Destructure(JArray ja, ILogEventPropertyValueFactory propertyValueFactory)
        {
            var elems = ja.Select(t => propertyValueFactory.CreatePropertyValue(t, true));
            return new SequenceValue(elems);
        }

        LogEventPropertyValue Destructure(JObject jo, ILogEventPropertyValueFactory propertyValueFactory)
        {
            string typeTag = null;
            var props = new List<LogEventProperty>(jo.Count);
            foreach (var prop in jo.Properties())
            {
                if (!CheckJsonName(prop))
                    continue;

                if (prop.Name == "$type")
                {
                    if (prop.Value is JValue typeVal && typeVal.Value is string)
                    {
                        typeTag = (string)typeVal.Value;
                        continue;
                    }
                }

                props.Add(new LogEventProperty(prop.Name, propertyValueFactory.CreatePropertyValue(prop.Value, true)));
            }

            return new StructureValue(props, typeTag);
        }

        private bool CheckJsonName(JProperty jProperty)
        {
            if (_ignoreExtactNames != null && _ignoreExtactNames.Contains(jProperty.Name))
                return false;
            if(_ignoreLikeNames != null)
            {
                foreach (var item in _ignoreLikeNames)
                {
                    //Start with *
                    if (!item.StartsWith("*") && item.EndsWith("*"))
                        if (jProperty.Name.ToLower().StartsWith(item.Substring(1, item.Length - 1)))
                            return false;

                    //Ends with *
                    if (item.StartsWith("*") && !item.EndsWith("*"))
                        if (jProperty.Name.ToLower().EndsWith(item.Substring(0, item.Length - 1)))
                            return false;

                    //Both Start *
                    if (jProperty.Name.ToLower().Contains(item.Substring(1, item.Length - 2)))
                        return false;
                }
            }

            return true;
        }
    }
}
