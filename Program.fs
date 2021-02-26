open System
open System.IO
open FsToolkit.ErrorHandling
open Argu
open Paket
open Paket.Domain
open Thoth.Json.Net

type CliArguments =
  | [<MainCommand; ExactlyOnce; Last>] LockFilePath of string
  | [<Unique>] Group of string
  | Dry_Run
  interface IArgParserTemplate with
    member s.Usage =
      match s with
      | LockFilePath _ -> "specify a path to the paket.lock"
      | Group _ -> "specify a package group"
      | Dry_Run -> "skips writing the nuget2config.json"

let parser = ArgumentParser.Create<CliArguments>(programName = "pakettobazel")

module LockFile =

  let tryLoadFile path =
    try
      LockFile.LoadFrom path
      |> Ok
    with exn ->
      Error exn

  let tryParse lines =
    try
      LockFile.Parse ("name", lines)
      |> Ok
    with exn ->
      Error exn

type DomainError =
  | InvalidCliArguments of Exception
  | ErrorLoadingLock of string * Exception
  | MissingGroup of GroupName * LockFile
  | FileWriteError of string * Exception

[<EntryPoint>]
let main argv =
  let result =
    asyncResult {
      let! results =
        try
          let results = parser.Parse argv

          let all = results.GetAllResults ()

          Ok all
        with exn ->
          Error (InvalidCliArguments exn)

      let lockFilePath =
        results
        |> Seq.tryPick (
          function
          | LockFilePath p -> Some p
          | _ -> None)
        |> Option.defaultValue "paket.lock"

      printfn "Loading lock-file from %s" lockFilePath

      let! lockFile =
        LockFile.tryLoadFile lockFilePath
        |> Result.mapError (fun exn -> ErrorLoadingLock (lockFilePath, exn))

      let groupName =
        results
        |> Seq.tryPick (
          function
          | Group n -> Some (GroupName n)
          | _ -> None)
        |> Option.defaultValue (GroupName "Main")

      printfn "Using dependencies of group \"%s\". " groupName.Name

      let! lockFileGroup =
        lockFile.Groups
        |> Map.tryFind groupName
        |> Result.requireSome (MissingGroup (groupName, lockFile))

      let json =
        [
          "externals", Encode.object []
          (
            "dependencies",
            lockFileGroup.Resolution
            |> Seq.map (fun (KeyValue (k, v)) ->
              k.Name, Encode.string (v.Version.Normalize ()))
            |> Seq.toList
            |> Encode.object
          )
        ]
        |> Encode.object

      let encoded = Encode.toString 2 json

      let isDryRun =
        results
        |> Seq.exists (
          function
          | Dry_Run -> true
          | _ -> false)

      if isDryRun
      then
        printfn "\n\n%A" encoded
      else
        do!
          async {
            let path = "nuget2config.json"

            try
              printfn "Updating %s ..." path
              do!
                File.WriteAllTextAsync (path, encoded)
                |> Async.AwaitTask

              return Ok ()
            with exn ->
              return Error (FileWriteError (path, exn))
          }

      printfn "Done. "

      ()
    }
    |> Async.RunSynchronously

  match result with
  | Ok () -> 0
  | Error e ->
    match e with
    | InvalidCliArguments _ ->
      printfn "%s" (parser.PrintUsage ())
    | _ ->
      printfn "%A" e
    1
