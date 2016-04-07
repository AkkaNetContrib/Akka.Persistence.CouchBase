using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence;

namespace Akka.Persistence.CouchBase
{
    static class CouchBaseDBUtility
    {

        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Action<T> processor)
        {
            await Task.Run(()=>{foreach (var item in source)  processor(item);});
        }
    }
}
