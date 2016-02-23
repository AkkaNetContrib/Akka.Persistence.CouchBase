# Akka.Persistence.CouchBase
Is a plugin that provides the ability to use Couchbase as a back-end store for the Akka.Persistence module.  For more information on Akka.Persistence please visit:
http://getakka.net/docs/Persistence

This plugin allows the Akka.Persistence layer to store journal and snapshot store entries into Couchbase.

This file has been written with those who are new in mind. 

## Acknowledgements
Many thanks to:
- Aaron Stannard and Bartosz Sypytkowski your guidance (via https://gitter.im/akkadotnet/akka.net) and articles where indispensible for us to be able to create this plugin.
- James Ring-Howell and the www.CoralFire.com team - your critical eye(s) and ideas helped this plug-in become better.
- The folks at Couchbase https://forums.couchbase.com/ - answering quickly to all our questions helped us complete this project.
- For the team who developed https://github.com/akkadotnet/Akka.Persistence.MongoDB without your work this would have been considerably harder.

## Couchbase plugin specific considerations
Unlike other document based databases which have a database which houses multiple collection of documents, Couchbase uses a bucket(s) to store documents.  Although the plugin was written to use distinct buckets for the storage of journal and snapshot entries it is best employed with a single bucket.  Journal and Snapshot entries are stored with tags that allow them to co-exist within the same bucket.  Couchbase, inherently, distributes bucket data in a cluster of physical machines which in turn creates resiliency and performance so storing both journal and snapshot entries within a bucket is just fine.


## Operational Requirements and Considerations
In this section I will cover in general terms what needs to be done to use the Akka.Persistence.Couchbase plugin in your code.  I will assume that you have some experience setting projects in Visual Studio and referencing dll's. At a high level To be able to use this plugin you will need:
- Download this code and compile plugin using Visual Studio 2013 or greater.
- Reference the plugin dll from the Akka.Persistence project which you wish to use Couchbase as a store.
- Instal Couchbase 4.1 and create bucket(s).
- Create Global Secondary Indexes (To increase lookup performance).
- Create either [HOCON](http://getakka.net/docs/concepts/configuration) or in your app.config file the configuration to connect to Couchbase (details below).
- Abide by the document limitations (more on this below).
- Start using the plugin as described in [this article]( https://petabridge.com/blog/intro-to-persistent-actors/).

We, at CoralFire, selected Couchbase 4.1 primarily because of N1QL.  Performing all CRUD operations is simple and familiar to many programmers due to its similarity to SQL.  That does not mean that this plug-in can also be developed using the clasical Couchbase operators to perform CRUD operations.  Adding this functionality can easily be done and it will allow for legacy Couchbase users (Couchbase < V4.1) to take advantage of the Akka.Persistence layer. To inquire please reach us at www.CoralFire.com

To use the plugin you need to take the following general steps:
1. Download Couchbase and CBQ from here: [Couchbase Downloads](http://www.couchbase.com/nosql-databases/downloads)
2. Create a new bucket using the Couchbase administrative console.
3. Create Global Secondary Indexes.

The first two items are beyond the scope of this file, however, I will explain how to create the Global Secondary Indexes.

Global secondary indexes speed up the performance of journal and snapshot entry retrieval.  When you create the bucket(s) which will house the journal and snapshot entries.  To do that open an instance of CBQ which you can get at [Couchbase Downloads](http://www.couchbase.com/nosql-databases/downloads).

Select the bucket which you created to store the journal and snapshot entries and create a primary index - if you have not done this yet - like this:
> CREATE PRIMARY INDEX ON `yourbucket`;

Now create the Gloal Secondary indexes with the following commands:

> CREATE INDEX idxDocumentType_PersistenceId_SequenceNr on `yourbucket` (PersistenceId,SequenceNr,DocumentType) USING GSI
CREATE INDEX idxDocumentType_PersistenceId_Timestamp on `yourbucket` (PersistenceId,Timestamp,DocumentType) USING GSI
CREATE INDEX idxDocumentType_PersistenceId on `yourbucket` (DocumentType,PersistenceId) USING GSI

Once the indexes are created now you need to configure your application.  There are two possible ways you can pass the Couchbase configuration to the plugin.  The first way is via Akka's HOCON configuration exclusively.  The other way is using a combination of HOCON and a Couchbase app.config section.  Let's start looking at the exclusive HOCON configuration first.

### Exclusive HOCON Configuration
To configure Akka.Persistence to access to use the Akka.Persistence.Couchbase plugin you may specify the following persistence section in your HOCON declarations:
> akka.persistence{
				journal{
					plugin = "akka.persistence.journal.couchbase"
					couchbase:{
                    	UseClusterHelper=false,
						class = "Akka.Persistence.CouchBase.Journal.CouchBaseDbJournal, Akka.Persistence.CouchBase"
						ServersURI:[
						"http://127.0.0.1:8091"
						],
						BucketName = "testakka",
						BucketUseSsl = false,
						Password = "",
						DefaultOperationLifespan = 2000,
						PoolConfiguration.MaxSize = 10,
						PoolConfiguration.MinSize = 5,
						SendTimeout = 12000
					}	
				}

				snapshot-store{
					plugin = "akka.persistence.snapshot-store.couchbase"
					couchbase:{
                    	UseClusterHelper=false,
						class = "Akka.Persistence.CouchBase.Snapshot.CouchBaseDbSnapshotStore, Akka.Persistence.CouchBase"
						ServersURI:[
						"http://127.0.0.1:8091"
						],
						BucketName = "testakka",
						BucketUseSsl = false,
						Password = """",
						DefaultOperationLifespan = 2000,
						PoolConfiguration.MaxSize = 10,
						PoolConfiguration.MinSize = 5,
						SendTimeout = 12000		            
					}	
				}
					
			}

Notice that there are two main sub-sections to the Akka.Persistence section - journal and snapshot-store.  Both of these sub-sections are required for the plugin to work properly.  They are needed to declare the back-end store which will be used for the journal entries and snapshot entries.  If the 'BucketName' and 'ServerURI' parameters in both the journal and snapshot sub-sections are the  same the plugin will use the same bucket in the cluster.  This is a basic explanation of the parameters:
- ServersURI - this is a list of servers in the Couchbase cluster.
- BucketName - Bucket which is going to store journal and/or snapshot entries.
- BucketUseSsl - Use SSL encryption for the bucket connection.
- Password - Password to be able to interact with the bucket.
- DefaultOperationalLifespan - Gets or sets the default maximum time an operation is allowed to take (including processing and in-flight time on the wire)
- PoolConfiguration.MaxSize - The maximum number of TCP connection to use
- PoolConfiguration.MinSize - 	The minimum or starting number of TCP connections to use
- SendTimeout - The amount of time to allow between an operation being written on the socket and being acknowledged.
- UseClusterHelper - Ignores HOCON configuration and relies on the ClusterHelper to be declared and the Couchbase section of the app.config to be defined for configuration parameters.  More on this on the next section.

### Using Couchbase app.config and The Cluster Helper
The Couchbase SDK(V2.2 as of the redaction of this document) includes a "ClusterHelper" which makes the Couchbase cluster and buckets globally available - see http://developer.couchbase.com/documentation/server/4.1/sdks/dotnet-2.2/cluster-helper.html for details.  If you decide to use this in your application your HOCON configuration file must let the plugin know that it needs to rely of the globally available ClusterHelper instance.  To do that declare your HOCON as this:

> akka.persistence{
				journal{
					plugin = "akka.persistence.journal.couchbase"
					couchbase:{
                    	UseClusterHelper=false
					}	
				}

				snapshot-store{
					plugin = "akka.persistence.snapshot-store.couchbase"
					couchbase:{
                    	UseClusterHelper=false	            
					}	
				}
					
			}

If the plugin does not find the ClusterHelper it will error out and fail.

### Document Size Limitations
Couchbase allows you to store documents up-to 20MB.  Documents larger than this limit will generate a failure.  This particularly important when deciding how much state data to store in your snapshots. You have been warned!

### Sample Persistence Project
I have included a preconfigured sample persistence project(See under src folder [PersistenceExample](https://github.com/ilhadad/Akka.Persistence.CouchBase/tree/master/src/PersistenceExample)) so that users can see how to use the Couchbase persistence layer.  This project was developed by the folks in Akka.net and we are grateful for providing this sample project.

## Development Findings
I am writing a few notes on a few things to consider if you choose to contribute or decide to create a plugin such as this.

### Development Tools, Packages, and Testing
To create the plugin I found that is was extremely useful to clone the Akka.Net repo and pull the Akka.Persistence, Akka.Persistence.TestKit, projects into your solution while you are testing.  Specifically, the Akka.Persistence.TestKit project was helpful to find out which parts of the plugin were not meeting the expected functionality.  

To develop the project you will need to create a class library project that will help the Akka.Persistence.TestKit find the configuration for testing the Couchbase plugin.  That project has been added to this repository and is called Akka.Persistence.Couchbase.Tests.

This plugin was developed using the following:
- Visual Studio 2013
- Couchbase SDK V2.2 - Nuget https://www.nuget.org/packages/CouchbaseNetClient/
- Couchbase V4.1
- .NET Framework V4.5
- XUnit.Net Runner - Unit testing runner pkg (if you want to run the tests within Visual Studio).
- Akka - Nuget pkg.
- Akka.Persistence - Nuget pkg.
- Akka.Persistence.TestKit - Nuget pkg.
- FAKE build system See http://fsharp.github.io/FAKE/ and a good guide here: http://fsharp.github.io/FAKE/gettingstarted.html
- CBQ - The CouchBase Query tool will help you during the setup of things http://developer.couchbase.com/documentation/server/4.1/cli/cbq-tool.html

### Required Knowledge and Where To Get It
Couchbase:
- N1QL - Couchbase Structure Query Language
- Indexes - How to create.
- Administration - How to create a bucket.
.NET C#:
- Tasks
- How to use Test Explorer
Akka.Net:
- Akka System - See http://getakka.net/
- Akka.Persistence - See http://getakka.net/docs/Persistence 
- For a conceptual understanding how does this pluging work see http://bartoszsypytkowski.com/how-akka-net-persistence-works/ 
- For a highlevel tour of the code see https://petabridge.com/blog/intro-to-persistent-actors/
- To get a basic understanding on how to put a plugin together see http://bartoszsypytkowski.com/create-your-own-akka-net-persistence-plugin/
GIT:
- This is how we work: https://petabridge.com/blog/github-workflow/
