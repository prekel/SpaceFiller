module SpaceFiller.WPF.Services

open System.IO
open SpaceFiller.Services

type WpfFreeStorage() =
    interface IFreeStorage with
        member _.GetFreeStorage() =
            let info = DriveInfo.GetDrives()
            info.[0].TotalFreeSpace

[<assembly: Xamarin.Forms.Dependency(typeof<WpfFreeStorage>)>]
do ()
