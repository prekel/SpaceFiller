namespace SpaceFiller

[<RequireQualifiedAccess>]
module Global =
    open System

    let appName = "SpaceFiller"

    let appDataPath =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        ^/ appName

    let dbName = $"%s{appName}.sqlite"

    let dbPath = appDataPath ^/ dbName

    let connectionString = $"Data Source=%s{dbPath}"


module Tables =
    open SqlHydra.Query
    open SpaceFiller.Db

    let fill_record = table<main.fill_record>
