namespace Shared.Models.Memory;

public enum States
{
    UNKNOWN = 0,
    UNMODIFIED = 1,
    GENERATING = 2,
    STOPPED = 3,
    EDITED = 4,
    DELETED = 5,
    FAILED = 6,
    EMPTY = 7,
}