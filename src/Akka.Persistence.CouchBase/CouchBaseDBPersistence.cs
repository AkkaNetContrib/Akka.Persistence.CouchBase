using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;

namespace Akka.Persistence.CouchBase
{
    /// <summary>
    /// Extension Id provider for the CouchBaseDB persistence extension.
    /// </summary>
    class CouchBaseDBPersistence : ExtensionIdProvider<CouchBaseDBExtension>
    {
        public static readonly CouchBaseDBPersistence Instance = new CouchBaseDBPersistence();

        private CouchBaseDBPersistence() { }


        /// <summary>
        /// Retrieves embedded default configuration 
        /// </summary>
        /// <returns>Akka Configuration</returns>
        public static Config DefaultConfiguration(){
            return ConfigurationFactory.FromResource<CouchBaseDBPersistence>("Akka.Persistance.CouchBase.reference.conf");
        }

        /// <summary>
        /// Instantiates the CouchDBExtension object.
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public override CouchBaseDBExtension CreateExtension(ExtendedActorSystem system)
        {
            return new CouchBaseDBExtension(system);
        }


    }
}
