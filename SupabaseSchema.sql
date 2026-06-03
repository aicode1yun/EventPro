-- EventPro Supabase Schema
-- Run this SQL in your Supabase project's SQL Editor (https://supabase.com/dashboard/project/_/sql/new)

-- 1. Attendees table
CREATE TABLE attendees (
    id BIGSERIAL PRIMARY KEY,
    full_name TEXT NOT NULL,
    phone_number TEXT,
    ticket_type TEXT NOT NULL DEFAULT 'General',
    ticket_code TEXT UNIQUE NOT NULL,
    qr_token TEXT NOT NULL DEFAULT '',
    is_checked_in BOOLEAN NOT NULL DEFAULT FALSE,
    checked_in_at TIMESTAMPTZ,
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    notes TEXT,
    payment_status TEXT NOT NULL DEFAULT 'Pending',
    photo_url TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 2. Events table (single event per organizer)
CREATE TABLE events (
    id BIGSERIAL PRIMARY KEY,
    event_name TEXT NOT NULL DEFAULT 'EventPro Conference',
    event_date DATE,
    venue TEXT NOT NULL DEFAULT 'Main Hall',
    description TEXT,
    logo_path TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes for fast lookups
CREATE INDEX idx_attendees_ticket_code ON attendees(ticket_code);
CREATE INDEX idx_attendees_phone_number ON attendees(phone_number);

-- Enable Row Level Security
ALTER TABLE attendees ENABLE ROW LEVEL SECURITY;
ALTER TABLE events ENABLE ROW LEVEL SECURITY;

-- RLS policies: all authenticated users share the same data (multi-device access)
CREATE POLICY "Authenticated users can read attendees"
    ON attendees FOR SELECT TO authenticated USING (true);

CREATE POLICY "Authenticated users can insert attendees"
    ON attendees FOR INSERT TO authenticated WITH CHECK (true);

CREATE POLICY "Authenticated users can update attendees"
    ON attendees FOR UPDATE TO authenticated USING (true);

CREATE POLICY "Authenticated users can delete attendees"
    ON attendees FOR DELETE TO authenticated USING (true);

CREATE POLICY "Authenticated users can read events"
    ON events FOR SELECT TO authenticated USING (true);

CREATE POLICY "Authenticated users can insert events"
    ON events FOR INSERT TO authenticated WITH CHECK (true);

CREATE POLICY "Authenticated users can update events"
    ON events FOR UPDATE TO authenticated USING (true);

CREATE POLICY "Authenticated users can delete events"
    ON events FOR DELETE TO authenticated USING (true);

-- Seed default event
INSERT INTO events (event_name, event_date, venue, description)
VALUES ('EventPro Conference', NOW() + INTERVAL '30 days', 'Main Hall', 'Annual tech conference');
