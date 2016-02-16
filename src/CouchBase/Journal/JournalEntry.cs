
namespace Akka.Persistence.CouchBase.Journal
{
    /// <summary>
    /// Class used for storing intermediate result of the <see cref="IPersistentRepresentation"/>
    /// as POCO
    /// </summary>
    public class JournalEntry 
    {
        public JournalEntry ()
	    {
            DocumentType = "JournalEntry";
	    }
        public string Id { get; set; }

        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }

        public bool isDeleted { get; set; }

        public object Payload { get; set; }

        public string Manifest { get; set; }

        public string DocumentType {get;set;}
    }
}
