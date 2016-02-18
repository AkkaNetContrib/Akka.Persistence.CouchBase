using System;
using System.Collections.Generic;
using Akka.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Core.Serialization;
using Newtonsoft.Json;

namespace Akka.Persistence.CouchBase
{
    public abstract class CouchDBSettings
    {

        public ClientConfiguration CBClientConfiguration { get; private set; }
        public string BucketName { get; private set; }

        // Not needed since we are not generating a bucket on the fly.
        //public string AdminUserName { get; private set; }

        //public string AdminPassword { get; private set; }




        public CouchDBSettings()
        {
        }

        protected void CreateCouchBaseDBClientConfiguration(Config config)
        {
            CBClientConfiguration = new ClientConfiguration();

            // Reset the serializers and deserializers so that JSON is stored as PascalCase instead of camelCase
            CBClientConfiguration.Serializer = () => new DefaultSerializer(new JsonSerializerSettings(), new JsonSerializerSettings());
            CBClientConfiguration.Servers.RemoveAt(0);

            //Get the URI's from the HOCON config
            try
            {
                if (config.GetStringList("ServersURI").Count > 0)
                {
                    List<Uri> uris = new List<Uri>();
                    foreach (string s in config.GetStringList("ServersURI"))
                    {
                        CBClientConfiguration.Servers.Add(new Uri(s));
                    }
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Invalid URI specified in HOCON configuration", ex);
            }

            // Use SSL?
            CBClientConfiguration.UseSsl = config.GetBoolean("UseSSL");

            // This will not be needed since we are not creating a bucket on the fly.
            //AdminPassword = config.GetString("AdminPassword");
            //AdminUserName = config.GetString("AdminUserName");


            // Get the bucket(s) configuration
            Dictionary<string, BucketConfiguration> BucketConfigs = new Dictionary<string, BucketConfiguration>();
            BucketConfiguration newBC = new BucketConfiguration();


            newBC.UseSsl = config.GetBoolean("BucketUseSSL");
            newBC.Password = config.GetString("Password");
            newBC.DefaultOperationLifespan = (uint)config.GetInt("DefaultOperationLifespan");
            BucketName = config.GetString("BucketName");
            newBC.BucketName = BucketName;
            newBC.PoolConfiguration.MinSize = config.GetInt("PoolConfiguration.MinSize");
            newBC.PoolConfiguration.MaxSize = config.GetInt("PoolConfiguration.MaxSize");

            // Create the bucket config specified in the HOCON
            BucketConfigs.Add(newBC.BucketName, newBC);
            CBClientConfiguration.BucketConfigs = BucketConfigs;



        }


    }

    public class CouchBaseJournalSettings : CouchDBSettings
    {
        public CouchBaseJournalSettings(Config config)
        {
            if (config == null)
                throw new ArgumentNullException("config",
                    "CoucBase journal settings cannot be initialized, because required HOCON section couldn't been found");
            CreateCouchBaseDBClientConfiguration(config);

        }

    }
    public class CouchbaseSnapshotSettings : CouchDBSettings
    {
        public CouchbaseSnapshotSettings(Config config)
        {
            if (config == null)
                throw new ArgumentNullException("config",
                    "CoucBase snapshot settings cannot be initialized, because required HOCON section couldn't been found");
            CreateCouchBaseDBClientConfiguration(config);

        }

    }
}
