module SpaceFiller.Tables

open SqlHydra.Query
open SpaceFiller.Db

let table_name = table<main.table_name>
let fill_record = table<main.fill_record>
