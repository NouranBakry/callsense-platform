namespace CallSense.Ingestion.Domain.Enums;

// An enum (not a raw string) so the compiler catches typos and all valid
// states are visible in one place.
// Stored as a string in the DB (see CallRecordConfiguration) so migration
// diffs stay readable ("Uploaded" instead of 0).
public enum CallStatus
{
    Uploaded,       // File received and saved to Blob Storage — waiting for transcription
    Transcribing,   // Transcription service picked up the job
    Analyzed,       // All downstream processing complete
    Failed          // Something went wrong in a downstream step
}
