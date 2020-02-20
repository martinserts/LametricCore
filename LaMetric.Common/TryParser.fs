namespace LaMetric.Common

module TryParser =
    open System
    open System.Globalization

    // convenient, functional TryParse wrappers returning option<'a>
    let tryParseWith tryParseFunc = tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None

    let parseDate : (string -> DateTime option) = tryParseWith (fun s -> DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None))
    let parseDateExact (format : string) = tryParseWith <| (fun s -> DateTime.TryParseExact(s,
                                                                                            format,
                                                                                            CultureInfo.InvariantCulture,
                                                                                            DateTimeStyles.None))
    let parseInt : (string -> Int32 option) = tryParseWith (fun s -> Int32.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture))
    let parseInt64 : (string -> Int64 option) = tryParseWith (fun s -> Int64.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture))
    let parseSingle : (string -> Single option) = tryParseWith (fun s -> Single.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture))
    let parseDouble : (string -> Double option) = tryParseWith (fun s -> Double.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture))
    let parseDecimal : (string -> Decimal option) = tryParseWith (fun s -> Decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture))
    let parseString = (fun s -> if String.IsNullOrEmpty(s) then None else Some s)
    // etc.

    // active patterns for try-parsing strings
    let (|Date|_|)   = parseDate
    let (|Int|_|)    = parseInt
    let (|Single|_|) = parseSingle
    let (|Double|_|) = parseDouble
