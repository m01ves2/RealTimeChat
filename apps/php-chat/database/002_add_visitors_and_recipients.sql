BEGIN;

ALTER TABLE messages ADD COLUMN recipient VARCHAR(30);

CREATE TABLE room_visitors (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    room_id integer NOT NULL REFERENCES rooms (id) ON DELETE CASCADE,
    nickname VARCHAR(30) NOT NULL,
    last_seen TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX room_visitors_room_id_nickname_idx ON room_visitors (room_id, LOWER(nickname));

CREATE INDEX messages_room_id_created_at_idx ON messages (room_id, created_at DESC);
CREATE INDEX room_visitors_room_id_last_seen_idx ON room_visitors (room_id, last_seen DESC);

INSERT INTO rooms(title) VALUES ('Programming'), ('Gaming'), ('Off Topic');

COMMIT;