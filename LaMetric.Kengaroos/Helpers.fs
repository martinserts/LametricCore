namespace LaMetric.Kengaroos

module Helpers =
    open NodaTime.Text
    
    let tryGetLocalDate (parseResult: ParseResult<_>) =
        match parseResult.Success with
        | true -> Some parseResult.Value
        | false -> Option.None

