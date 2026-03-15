#!/usr/bin/env python3
"""
Idempotent DB seeding script for the game persistence schema.
Uses SQLite. If the database file and tables already exist, does nothing.
Otherwise creates: accounts, playerstats, playerinventory.
Run from project root: python Scripts/seed_db.py
"""

import os
import sqlite3
import sys

# Default: Data/game.db relative to project root (parent of Scripts)
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
DEFAULT_DB_PATH = os.path.join(PROJECT_ROOT, "Data", "game.db")

REQUIRED_TABLES = ("accounts", "playerstats", "playerinventory")


def schema_exists(conn):
    """Return True if all required tables exist."""
    cur = conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name IN (?, ?, ?)",
        REQUIRED_TABLES,
    )
    found = {row[0] for row in cur.fetchall()}
    return found == set(REQUIRED_TABLES)


def create_schema(conn):
    """Create accounts, playerstats, and playerinventory tables."""
    conn.executescript("""
        CREATE TABLE accounts (
            id TEXT PRIMARY KEY,
            displayname TEXT,
            authprovider TEXT,
            createdat TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE TABLE playerstats (
            accountid TEXT PRIMARY KEY,
            statsjson TEXT NOT NULL,
            updatedat TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (accountid) REFERENCES accounts(id)
        );

        CREATE TABLE playerinventory (
            accountid TEXT PRIMARY KEY,
            inventoryjson TEXT NOT NULL,
            updatedat TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (accountid) REFERENCES accounts(id)
        );
    """)


def main():
    db_path = os.environ.get("GAME_DB_PATH", DEFAULT_DB_PATH)
    db_dir = os.path.dirname(db_path)
    if db_dir and not os.path.isdir(db_dir):
        os.makedirs(db_dir, exist_ok=True)

    if os.path.isfile(db_path):
        conn = sqlite3.connect(db_path)
        try:
            if schema_exists(conn):
                print(f"Database already exists with schema: {db_path}")
                return 0
        finally:
            conn.close()

    conn = sqlite3.connect(db_path)
    try:
        create_schema(conn)
        conn.commit()
        print(f"Created schema at {db_path}")
    finally:
        conn.close()

    return 0


if __name__ == "__main__":
    sys.exit(main())
