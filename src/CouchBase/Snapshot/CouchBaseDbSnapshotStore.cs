using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Persistence.Snapshot;
using Akka.Actor;
using Couchbase;
using Couchbase.N1QL;

namespace Akka.Persistence.CouchBase.Snapshot
{
    class CouchBaseDbSnapshotStore:SnapshotStore
    {
        private CouchbaseBucket _CBBucket;


        public CouchBaseDbSnapshotStore()
        {
            _CBBucket = CouchBaseDBPersistence.Instance.Apply(Context.System).SnapShotStoreCBBucket;
        }

        protected override Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {

            // Create a Query with dynamic parameters
            string N1QLQueryString = "select from `" + _CBBucket.Name + "`.* where DocumentType = 'SnapshotEntry' AND PersistenceId = '$PersistenceId' ";

            long limit=0;

            if (criteria.MaxSequenceNr > 0 && criteria.MaxSequenceNr < long.MaxValue)
            {
                N1QLQueryString += "AND SequenceNr <= $limit ORDER BY SequenceNr DESC LIMIT 1";
                limit = criteria.MaxSequenceNr;
            }

            if (criteria.MaxTimeStamp != DateTime.MinValue && criteria.MaxTimeStamp != DateTime.MaxValue)
            {
                N1QLQueryString += "AND Timestamp <= $limit ORDER BY TimeStamp DESC LIMIT 1";
                limit = criteria.MaxTimeStamp.Ticks;

            }

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                    .Statement(N1QLQueryString)
                    .AddNamedParameter("PersistenceId", persistenceId)
                    .AddNamedParameter("limit", limit)
                    .AdHoc(false);

            return taskLoadAsync(N1QLQueryRequest);
                

        }

        private async Task<SelectedSnapshot> taskLoadAsync(IQueryRequest N1QLQueryRequest)
        {


            Task<IQueryResult<SnapshotEntry>> queryTask = _CBBucket.QueryAsync<SnapshotEntry>(N1QLQueryRequest);
            IQueryResult<SnapshotEntry> result = await queryTask;
            if (result.Rows.Count == 0)
                throw new Exception("No Snapshots Found");

            return ToSelectedSnapshot(result.Rows[0]);

        }

        private SelectedSnapshot ToSelectedSnapshot(SnapshotEntry entry)
        {
            return
                new SelectedSnapshot(
                    new SnapshotMetadata(entry.PersistenceId, entry.SequenceNr, new DateTime(entry.Timestamp)),
                    entry.Snapshot);
        }

        protected override Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            return SaveAsyncTask(snapshot, metadata);
        }

        async private Task SaveAsyncTask(object snapshot, SnapshotMetadata metadata)
        {
            await Task.Run(() =>
            {
                    Document<SnapshotEntry> SnapshotEntryDocument = ToSnapshotlEntryDocument(snapshot, metadata);
                    _CBBucket.InsertAsync<SnapshotEntry>(SnapshotEntryDocument);

            });
        }

        private Document<SnapshotEntry> ToSnapshotlEntryDocument(object snapshot, SnapshotMetadata metadata)
        {
            return new Document<SnapshotEntry>
            {
                Id = metadata.PersistenceId + "_" + metadata.SequenceNr.ToString(),
                Content = new SnapshotEntry
                {
                    Id = metadata.PersistenceId + "_" + metadata.SequenceNr,
                    PersistenceId = metadata.PersistenceId,
                    SequenceNr = metadata.SequenceNr,
                    Snapshot = snapshot,
                    Timestamp = metadata.Timestamp.Ticks
                }
            };
        }

        /// <summary>
        /// Called after snapshot is saved
        /// </summary>
        /// <param name="metadata"></param>
        protected override void Saved(SnapshotMetadata metadata) { 
            //Does not do anything; for now. 
            // Add loger here?
        }

        protected override Task DeleteAsync(SnapshotMetadata metadata)
        {
            return DeletePermanentlyMessages(metadata);
        }

        private Task DeletePermanentlyMessages(SnapshotMetadata metadata)
        {
            string N1QLQueryString = "delete from `" + _CBBucket.Name + "` where DocumentType = 'SnapshotEntry' AND PersistenceId = '$PersistenceId' ";
            long target=0;
            if (metadata.SequenceNr > 0 && metadata.SequenceNr < long.MaxValue)
            {
                N1QLQueryString += "AND SequenceNr = $target ORDER BY SequenceNr DESC LIMIT 1";
                target = metadata.SequenceNr;
            }

            if (metadata.Timestamp != DateTime.MinValue && metadata.Timestamp != DateTime.MaxValue)
            {
                N1QLQueryString += "AND Timestamp = $target ORDER BY TimeStamp DESC LIMIT 1";
                target = metadata.Timestamp.Ticks;

            }

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                    .Statement(N1QLQueryString)
                    .AddNamedParameter("PersistenceId", metadata.PersistenceId)
                    .AddNamedParameter("target", target)
                    .AdHoc(false);

            return _CBBucket.QueryAsync<dynamic>(N1QLQueryRequest);

            //Add logger here
        }

        protected override Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            string N1QLQueryString = "delete from `" + _CBBucket.Name + "` where DocumentType = 'SnapshotEntry'AND PersistenceId = '$PersistenceId' ";
            long target = 0;
            if (criteria.MaxSequenceNr > 0 && criteria.MaxSequenceNr < long.MaxValue)
            {
                N1QLQueryString += "AND SequenceNr <= $target ORDER BY SequenceNr DESC LIMIT 1";
                target = criteria.MaxSequenceNr;
            }

            if (criteria.MaxTimeStamp != DateTime.MinValue && criteria.MaxTimeStamp != DateTime.MaxValue)
            {
                N1QLQueryString += "AND Timestamp <= $target ORDER BY TimeStamp DESC LIMIT 1";
                target = criteria.MaxTimeStamp.Ticks;

            }

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                    .Statement(N1QLQueryString)
                    .AddNamedParameter("PersistenceId", persistenceId)
                    .AddNamedParameter("target", target)
                    .AdHoc(false);

            return _CBBucket.QueryAsync<dynamic>(N1QLQueryRequest);

            //Add logger here

        }
    }
}
