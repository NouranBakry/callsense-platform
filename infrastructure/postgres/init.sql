-- Creates one database per service on first container startup.
-- Add new databases here as new services are added.

CREATE DATABASE callsense_ingestion;
CREATE DATABASE callsense_transcription;
CREATE DATABASE callsense_analysis;
CREATE DATABASE callsense_scoring;
CREATE DATABASE callsense_reporting;
CREATE DATABASE callsense_notifications;
CREATE DATABASE callsense_identity;
