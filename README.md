# Akka.Persistence.CouchBase
Is a plugin that provides the ability to use Couchbase as a back-end store for the Akka.Persistence module.  For more information on Akka.Persistence please visit:
http://getakka.net/docs/Persistence

This plugin allows the Akka.Persistence layer to store journal and snapshot store entries into Couchbase.

This file has been written with those who are new in mind. 

## Couchbase plugin specific considerations
Unlike other document based databases which have a database which houses multiple collection of documents, Couchbase uses a bucket(s) to store documents.  Although the plugin was written to use distinct buckets for the storage of journal and snapshot entries it is best employed with a single bucket.  Journal and Snapshot entries are stored with tags that allow them to co-exist within the same bucket.  Couchbase, inherently, distributes bucket data in a cluster of physical machines which in turn creates resiliency and performance so storing both journal and snapshot entries within a bucket is just fine.

## Development Requirements
This plugin was developed using the following:
- Visual Studio 2013
- Couchbase SDK V2.2 - Nuget https://www.nuget.org/packages/CouchbaseNetClient/
- Couchbase V4.1
- .NET Framework V4.5
- XUnit - Unit testing
- Akka - Nuget pkg.
- Akka.Persistence - Nuget pkg.
- Akka.Persistence.TestKit - Nuget pkg.
- FAKE build system See http://fsharp.github.io/FAKE/ and a good guide here: http://fsharp.github.io/FAKE/gettingstarted.html


### Background Knowledge Prerequisites
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

## Operational Requirements and Considerations

To be able to use this plugin you will need:
- Couchbase 4.1
- Create Global Secondary Indexes (To increase lookup performance).
- Abide by the document limitations (more on this below).

We selected Couchbase 4.1 primarily because of N1QL.  Performing all CRUD operations is simple and familiar to many programmers due to its similarity to SQL.  That does not mean that this plug-in can also be developed using the clasical Couchbase operators to perform CRUD operations.  Adding this functionality can easily be done and it will allow for legacy Couchbase users (Couchbase < V4.1) to take advantage of the Akka.Persistence layer.



