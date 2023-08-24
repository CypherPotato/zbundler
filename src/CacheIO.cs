using System.Collections.Concurrent;

namespace zbundler.src;
public class CacheIO
{
    private static ConcurrentDictionary<string, string> Cache { get; set; } = new ConcurrentDictionary<string, string>();

    public static void Invalidate(string filename)
    {
        if (Cache.ContainsKey(filename))
        {
            Cache.Remove(filename, out _);
        }
    }

    public static string RetrieveFile(string filename)
    {
        if (Cache.TryGetValue(filename, out var contents))
        {
            return contents;
        }
        else
        {
            contents = File.ReadAllText(filename);
            Cache.TryAdd(filename, contents);
            return contents;
        }
    }

    public static string RetrieveURL(string extLink)
    {
        if (Cache.TryGetValue(extLink, out var contents))
        {
            return contents;
        }
        else
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, extLink);
                HttpResponseMessage res = client.Send(req);

                if (!res.IsSuccessStatusCode)
                {
                    Build.PrintBuildError($"Got HTTP {(int)res.StatusCode} when trying to fetch {extLink}.");
                    Build.SafeExit(1);
                    return "";
                }

                string result = res.Content.ReadAsStringAsync().Result;
                Cache.TryAdd(extLink, result);
                return result;
            }
        }
    }
}
