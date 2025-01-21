using System.Collections.Generic;
using System.IO;
using System.Xml;
using Newtonsoft.Json;

public class PathfindingJsonWriter
{
    public void AppendPathfindingResultToJson(string filePath, List<PathfindResult> results)
    {

        if (results == null || results.Count == 0) return;

        // Convert the list to JSON
        string json = JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);

        // Write to a file
        File.AppendAllText(filePath, json);
    }
}
