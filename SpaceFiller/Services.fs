module SpaceFiller.Services

type IFreeStorage =
    abstract member GetFreeStorage : unit -> int64
