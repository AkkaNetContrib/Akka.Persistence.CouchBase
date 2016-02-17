using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Journal;
using Couchbase;
using Couchbase.N1QL;

namespace Akka.Persistence.CouchBase.Journal
{
    /// <summary>
    /// An Akka.NET journal implementation that writes events asynchronously to CouchBase DB.
    /// </summary>
    public class CouchBaseDbJournal : AsyncWriteJournal
    {
        private CouchbaseBucket _CBBucket;
        

        public CouchBaseDbJournal()
        {
            _CBBucket = CouchBaseDBPersistence.Instance.Apply(Context.System).JournalCBBucket;
        }

        public override Task ReplayMessagesAsync(string persistenceId, long fromSequenceNr, long toSequenceNr, long max, Action<IPersistentRepresentation> replayCallback)
        {
            // Limit(0) doesn't work...
            if (max == 0)
                return Task.Run(() => {});

            // Limit allows only integer
            var maxValue = max >= int.MaxValue ? int.MaxValue : (int)max;
            var sender = Context.Sender;

            String N1QLQueryString = "SELECT `" + _CBBucket.Name + "`.* FROM `" + _CBBucket.Name + "` WHERE PersistenceId = $PersistenceId AND SequenceNr >= $FromSequenceNr  AND SequenceNr <= $ToSequenceNr ORDER BY SequenceNr ASC";

            IQueryRequest N1QLQueryRequest = new QueryRequest()
            .Statement(N1QLQueryString)
            .AddNamedParameter("PersistenceId", persistenceId)
            .AddNamedParameter("FromSequenceNr", fromSequenceNr)
            .AddNamedParameter("ToSequenceNr", toSequenceNr)
            .AdHoc(false);

            Couchbase.N1QL.IQueryResult<JournalEntry> result = _CBBucket.Query<JournalEntry>(N1QLQueryRequest);


            return result.Rows.ForEachAsync<JournalEntry>(je => replayCallback(ToPersistanceRepresentation(je, sender)));
        }

        private Persistent ToPersistanceRepresentation(JournalEntry entry, IActorRef sender)
        {
            return new Persistent(entry.Payload, entry.SequenceNr, entry.Manifest, entry.PersistenceId, entry.isDeleted, sender);
        }



        public override Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            return taskReadHighestSequenceNrAsync(persistenceId, fromSequenceNr);
        }

        private async Task<long> taskReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            string resultField = "MaxSequenceNr";
            string N1QLQueryString = "SELECT MAX(`" + _CBBucket.Name + "`.SequenceNr) AS " + resultField + " FROM `" + _CBBucket.Name + "` WHERE DocumentType='JournalEntry' AND PersistenceId =$PersistenceId";

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                .Statement(N1QLQueryString)
                .AddNamedParameter("PersistenceId", persistenceId)
                .AdHoc(false);

            var queryTask = _CBBucket.QueryAsync<dynamic>(N1QLQueryRequest);
            long highestSeqNum = 0;
            var result = await queryTask;
            long.TryParse(result.Rows[0][resultField].ToString(), out highestSeqNum);
            return highestSeqNum;
        }


        protected override Task WriteMessagesAsync(IEnumerable<IPersistentRepresentation> messages)
        {
            return WriteMessagesTask(messages);
        }

        async private Task WriteMessagesTask(IEnumerable<IPersistentRepresentation> messages)
        {
            await Task.Run(async () =>
            {
                foreach (IPersistentRepresentation m in messages)
                {
                    Document<JournalEntry> jED = ToJournalEntry(m);
                    Task<IDocumentResult<JournalEntry>> ia = _CBBucket.InsertAsync<JournalEntry>(jED);
                    IDocumentResult<JournalEntry> dr = await ia;
                    string test = "Yo: " + dr.ToString();

                }
            });
        }

        protected override Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr, bool isPermanent)
        {

            if (toSequenceNr != long.MaxValue)
            {


                if (isPermanent)// Wipe out the records
                {
                    return DeletePermanentlyMessages(persistenceId, toSequenceNr);

                }
                else// Mark them for deletion
                {
                    return DeleteByMarkingMessages(persistenceId,toSequenceNr);

                }
            }
            else
            {
                throw new OverflowException("Sequence number larger than long's maximum allowed value.");
            }
        }

        private async Task DeletePermanentlyMessages(string persistenceId, long toSequenceNr)
        {
            string N1QLQueryString = "DELETE FROM `"+_CBBucket.Name+"` WHERE DocumentType = 'JournalEntry' AND PersistenceId = $PersistenceId AND SequenceNr <= $ToSequenceNr";

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                .Statement(N1QLQueryString)
                .AddNamedParameter("PersistenceId", persistenceId)
                .AddNamedParameter("ToSequenceNr", toSequenceNr)
                .AdHoc(false);

            var queryTask = _CBBucket.QueryAsync<JournalEntry>(N1QLQueryRequest);
            var result = await queryTask;
            //Add logger here

        }

        async private Task DeleteByMarkingMessages(string persistenceId, long toSequenceNr)
        {
            string N1QLQueryString = "UPDATE `" + _CBBucket.Name + "` SET isDeleted = true WHERE DocumentType = 'JournalEntry' AND PersistenceId = $PersistenceId AND SequenceNr <= $ToSequenceNr";

            IQueryRequest N1QLQueryRequest = new QueryRequest()
                .Statement(N1QLQueryString)
                .AddNamedParameter("PersistenceId", persistenceId)
                .AddNamedParameter("ToSequenceNr", toSequenceNr)
                .AdHoc(false);

            var queryTask = _CBBucket.QueryAsync<JournalEntry>(N1QLQueryRequest);
            var result = await queryTask;
            //Add logger here
        }



        private Document<JournalEntry> ToJournalEntry(IPersistentRepresentation message)
        {
            return new Document<JournalEntry>
            {
                Id = message.PersistenceId + "_" + message.SequenceNr.ToString(),
                Content = new JournalEntry
                {
                    Id = message.PersistenceId + "_" + message.SequenceNr,
                    isDeleted = message.IsDeleted,
                    Payload = message.Payload,
                    PersistenceId = message.PersistenceId,
                    SequenceNr = message.SequenceNr,
                    Manifest = message.Manifest
                }
            };
        }

    }
}
