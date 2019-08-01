using System;
using System.Collections.Generic;

namespace LightestNight.System.Caching.Redis.Tests.TagCache
{
    [Serializable]
    public class TestObject
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
        public int Property3 { get; set; }
        
        public List<string> List { get; set; }
    }
}