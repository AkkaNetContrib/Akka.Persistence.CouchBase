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
        private Couchbase.Core.IBucket _CBBucket;


        public CouchBaseDbSnapshotStore()
        {
            _CBBucket = CouchBaseDBPersistence.Instance.Apply(Context.System).SnapShotStoreCBBucket;
        }

        protected override Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {

            // Create a Query with dynamic parameters
            string N1QLQueryString = "select `" + _CBBucket.Name + "`.* from `" + _CBBucket.Name + "` where DocumentType = 'SnapshotEntry' AND PersistenceId = $PersistenceId ";

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                    .AddNamedParameter("PersistenceId", persistenceId);

            string N1QLQueryOrderByClauseString = "ORDER BY SequenceNr DESC";
            
            if (criteria.MaxSequenceNr > 0 && criteria.MaxSequenceNr < long.MaxValue)
            {
                N1QLQueryString += "AND SequenceNr <= $limit ";
                N1QLQueryOrderByClauseString = "ORDER BY SequenceNr DESC,";
                N1QLQueryRequest.AddNamedParameter("limit",criteria.MaxSequenceNr);
            }

            if (criteria.MaxTimeStamp != DateTime.MinValue && criteria.MaxTimeStamp != DateTime.MaxValue)
            {
                N1QLQueryString += " AND Timestamp <= $timelimit ";
                N1QLQueryOrderByClauseString = "ORDER BY Timestamp DESC,";
                N1QLQueryRequest.AddNamedParameter("timelimit", criteria.MaxTimeStamp.Ticks.ToString());
            }

            N1QLQueryString += N1QLQueryOrderByClauseString.TrimEnd(',') + " LIMIT 1"; 

            N1QLQueryRequest.Statement(N1QLQueryString).AdHoc(false);

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
                    new SnapshotMetadata(entry.PersistenceId, entry.SequenceNr, new DateTime(long.Parse(entry.Timestamp))),
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
                    Document<SnapshotEntry> SnapshotEntryDocument = ToSnapshotEntryDocument(snapshot, metadata);
                    _CBBucket.UpsertAsync<SnapshotEntry>(SnapshotEntryDocument);

            });
        }

        private Document<SnapshotEntry> ToSnapshotEntryDocument(object snapshot, SnapshotMetadata metadata)
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
                    Timestamp = metadata.Timestamp.Ticks.ToString()
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
            string N1QLQueryString = "delete from `" + _CBBucket.Name + "` where DocumentType = 'SnapshotEntry' AND PersistenceId = $PersistenceId ";

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                .AddNamedParameter("PersistenceId", metadata.PersistenceId)
                .AdHoc(false);



            if (metadata.SequenceNr > 0 && metadata.SequenceNr < long.MaxValue)
            {
                N1QLQueryString += " AND SequenceNr = $TargetSequenceId";
                N1QLQueryRequest.AddNamedParameter("TargetSequenceId", metadata.SequenceNr);

            }

            if (metadata.Timestamp != DateTime.MinValue && metadata.Timestamp != DateTime.MaxValue)
            {
                N1QLQueryString += " AND Timestamp = $TargetTimeStamp";
                N1QLQueryRequest.AddNamedParameter("TargetTimeStamp", metadata.Timestamp.Ticks.ToString());
            }

            N1QLQueryRequest.Statement(N1QLQueryString).AdHoc(false);

            return _CBBucket.QueryAsync<dynamic>(N1QLQueryRequest);
        }

        protected override Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            string N1QLQueryString = "delete from `" + _CBBucket.Name + "` where DocumentType = 'SnapshotEntry' AND PersistenceId = $PersistenceId ";

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                .AddNamedParameter("PersistenceId", persistenceId);


            if (criteria.MaxSequenceNr > 0 && criteria.MaxSequenceNr < long.MaxValue)
            {
                N1QLQueryString += " AND SequenceNr <= $limit ";
                N1QLQueryRequest.AddNamedParameter("limit", criteria.MaxSequenceNr);
            }

            if (criteria.MaxTimeStamp != DateTime.MinValue && criteria.MaxTimeStamp != DateTime.MaxValue)
            {
                N1QLQueryString += " AND Timestamp <= $timelimit ";
                N1QLQueryRequest.AddNamedParameter("timelimit", criteria.MaxTimeStamp.Ticks.ToString());
            }

            N1QLQueryRequest.Statement(N1QLQueryString).AdHoc(false);

            return _CBBucket.QueryAsync<dynamic>(N1QLQueryRequest);
        }
    }
}
