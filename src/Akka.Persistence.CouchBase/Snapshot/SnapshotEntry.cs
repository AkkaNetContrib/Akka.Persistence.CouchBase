
namespace Akka.Persistence.CouchBase.Snapshot
{
    class SnapshotEntry
    {
        public SnapshotEntry()
        {
            DocumentType = "SnapshotEntry";
        }
        public string Id { get; set; }

        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }

        public string Timestamp { get; set; }

        public object Snapshot { get; set; }

        public string DocumentType{ get; set; }

    }
}
