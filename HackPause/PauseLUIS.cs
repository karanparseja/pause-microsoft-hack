using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot
{

    public class Rootobject
    {
        public object alteredText { get; set; }
        public Entities entities { get; set; }
        public Intents intents { get; set; }
        public string text { get; set; }
    }

    public class Entities
    {
        public Instance instance { get; set; }
    }

    public class Instance
    {
    }

    public class Intents
    {
        public Book_Flight Book_flight { get; set; }
    }

    public class Book_Flight
    {
        public float score { get; set; }
    }

}