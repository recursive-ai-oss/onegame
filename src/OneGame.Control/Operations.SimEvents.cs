namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] SimEventOps =>
    [
        new("sim_events",
            "The one-shot events the simulation emitted (the world's event log), as JSON: world (authority or replica), tick, next (the sequence the next event will get), first (the oldest sequence still held), dropped (events lost to the per-tick limit), hash (a hash of the whole stream, for comparing two runs) and events [{sequence, tick, entity, type, data}] oldest first. type is the event's name and data its payload fields. since=N returns only events with sequence N or more (pass the last call's next to read what is new); max caps the list at the newest N (at most 512, the size of the log). On a replica the list holds what the authority sent this audience. Events are output, not state: they are not saved or hashed with the world, and a snapshot restore empties the log.",
            """{"type":"object","properties":{"since":{"type":"integer","minimum":0},"max":{"type":"integer","minimum":1,"maximum":512}}}""", false),
    ];
}
