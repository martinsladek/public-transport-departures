using System.Text;

namespace Departures;

static class GtfsCsv
{
    public static Dictionary<string, int> HeaderMap(string headerLine)
    {
        string[] cells = Split(headerLine);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < cells.Length; i++)
            map[cells[i].Trim()] = i;
        return map;
    }

    public static string Cell(string[] row, Dictionary<string, int> header, string name) =>
        header.TryGetValue(name, out int i) && i < row.Length ? row[i] : "";

    public static string[] Split(string line)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                cells.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        cells.Add(sb.ToString());
        return cells.ToArray();
    }
}
