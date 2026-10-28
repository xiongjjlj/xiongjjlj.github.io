using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
namespace System.Text.Json {
    public sealed class JsonSerializerOptions { public bool WriteIndented { get; set; } }
    // Small fixture serializer for the original Mono runtime. Input consists
    // only of anonymous records, arrays, strings, booleans and finite numbers.
    public static class JsonSerializer {
        public static string Serialize(object value, JsonSerializerOptions options = null) {
            var text = new StringBuilder(); Append(text, value, 0); return text.ToString();
        }
        static void Quote(StringBuilder b, string value) {
            b.Append('"');
            foreach (char c in value) {
                if (c == '"' || c == '\\') b.Append('\\').Append(c);
                else if (c < 32) b.Append("\\u").Append(((int)c).ToString("x4"));
                else b.Append(c);
            }
            b.Append('"');
        }
        static void Append(StringBuilder b, object value, int depth) {
            if (depth > 20) throw new InvalidOperationException("Fixture exceeds nesting limit");
            if (value == null) { b.Append("null"); return; }
            if (value is string s) { Quote(b,s); return; }
            if (value is bool yes) { b.Append(yes ? "true" : "false"); return; }
            if (value is float f) { if(float.IsNaN(f)||float.IsInfinity(f))throw new InvalidOperationException("Nonfinite fixture value"); b.Append(f.ToString("R",CultureInfo.InvariantCulture)); return; }
            if (value is double d) { if(double.IsNaN(d)||double.IsInfinity(d))throw new InvalidOperationException("Nonfinite fixture value"); b.Append(d.ToString("R",CultureInfo.InvariantCulture)); return; }
            if (value is int || value is long || value is uint || value is short) { b.Append(Convert.ToString(value,CultureInfo.InvariantCulture)); return; }
            if (value is IDictionary dictionary) {
                b.Append('{'); bool firstEntry=true;
                foreach(DictionaryEntry entry in dictionary) {
                    if(!firstEntry)b.Append(','); firstEntry=false; Quote(b,(string)entry.Key); b.Append(':'); Append(b,entry.Value,depth+1);
                }
                b.Append('}'); return;
            }
            if (value is IEnumerable sequence) {
                b.Append('['); bool first=true;
                foreach(object item in sequence) { if(!first)b.Append(','); first=false; Append(b,item,depth+1); }
                b.Append(']'); return;
            }
            b.Append('{'); bool initial=true;
            foreach(var property in value.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance)) {
                if(!property.CanRead || property.GetIndexParameters().Length!=0)continue;
                if(!initial)b.Append(','); initial=false; Quote(b,property.Name); b.Append(':'); Append(b,property.GetValue(value),depth+1);
            }
            b.Append('}');
        }
    }
}
