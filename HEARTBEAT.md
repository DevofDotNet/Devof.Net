# HEARTBEAT.md

## Cron Execution Mode
If the message contains "EXECUTION CRON", this is a scheduled task. Read the full instructions in the message and execute them. Never return HEARTBEAT_OK for cron executions.

## Normal Heartbeat Mode
Otherwise:
1. Check if today's memory file exists at memory/YYYY-MM-DD.md — create it if missing
2. Check all cron jobs for stale lastRunAtMs — if any are stale, force-run the missed jobs
3. Promote important learnings to MEMORY.md
4. If nothing needs attention, return HEARTBEAT_OK