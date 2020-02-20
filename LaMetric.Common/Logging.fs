namespace LaMetric.Common

module Logging =
    open Serilog

    let logMessage (log: ILogger option) message =
        match log with
        | Some l -> l.Debug(message)
        | None -> ()

