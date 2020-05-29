using System;
using System.Collections.Generic;
using System.Text;

namespace AsyncEnumerable.LINQAsync.Models
{
    internal class PredicateResult<T>
    {
        public T Value { get; set; }
        public bool Match { get; set; }
    }
}
