using System;
using System.Configuration;
using Akka.Persistence.TestKit.Snapshot;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core.Serialization;
using Couchbase.Management;
using Newtonsoft.Json;

namespace Akka.Persistence.MongoDb.Tests
{
    public class CouchBaseSnapshotStoreSpec : SnapshotStoreSpec
    {
        private static readonly string SpecConfig = @"
            akka.persistence{
	            journal{
		            couchdb:{
                        ServersURI:[
                        ""http://127.0.0.1:8091""
                        ],
                        AdminPassword = ""newuser"",
                        AdminUserName = ""Administrator"",
                        BucketName = ""testakka"",
                        BucketUseSsl = false,
                        Password = """",
                        DefaultOperationLifespan = 2000,
                        PoolConfiguration.MaxSize = 10,
                        PoolConfiguration.MinSize = 5,
                        SendTimeout = 12000
		            }	
	            }

	            snapshot-store{
		            couchdb:{
                        ServersURI:[
                        ""http://127.0.0.1:8091""
                        ],
                        AdminPassword = ""newuser"",
                        AdminUserName = ""Administrator"",
                        BucketName = ""testakka"",
                        BucketUseSsl = false,
                        Password = """",
                        DefaultOperationLifespan = 2000,
                        PoolConfiguration.MaxSize = 10,
                        PoolConfiguration.MinSize = 5,
                        SendTimeout = 12000		            
		            }	
                }
                	
            }
        ";


        public CouchBaseSnapshotStoreSpec() : base(CreateSpecConfig(), "MongoDbSnapshotStoreSpec")
        {
            Initialize();
        }

        private static string CreateSpecConfig()
        {
            return SpecConfig;
        }

        protected override void Dispose(bool disposing)
        {
            ClientConfiguration CBClientConfiguration;
            CBClientConfiguration = new ClientConfiguration();

            // Reset the serializers and deserializers so that JSON is stored as PascalCase instead of camelCase
            CBClientConfiguration.Serializer = () => new DefaultSerializer(new JsonSerializerSettings(), new JsonSerializerSettings());

            CBClientConfiguration.Servers.Add(new Uri("http://127.0.0.1:8091"));

            CBClientConfiguration.UseSsl = false;
            Cluster testCluster = new Cluster(CBClientConfiguration);

            testCluster.CreateManager("Administrator", "newuser").RemoveBucket("testakka");

            base.Dispose(disposing);
        }
    }
}
