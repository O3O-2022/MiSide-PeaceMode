using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Immutable;

// usage: dump <dll> [typeFilterRegex]
var path = args[0];
var filter = args.Length > 1 ? new System.Text.RegularExpressions.Regex(args[1], System.Text.RegularExpressions.RegexOptions.IgnoreCase) : null;
using var fs = File.OpenRead(path);
using var pe = new PEReader(fs);
var md = pe.GetMetadataReader();
var prov = new SigProvider(md);
foreach (var th in md.TypeDefinitions)
{
    var t = md.GetTypeDefinition(th);
    var ns = md.GetString(t.Namespace);
    var name = md.GetString(t.Name);
    var full = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    if (!t.GetDeclaringType().IsNil) full = TypeName(md, t.GetDeclaringType()) + "+" + name;
    if (filter != null && !filter.IsMatch(full)) continue;
    string baseT = t.BaseType.IsNil ? "" : " : " + prov.Name(t.BaseType);
    Console.WriteLine($"class {full}{baseT}");
    foreach (var fh in t.GetFields())
    {
        var f = md.GetFieldDefinition(fh);
        var fname = md.GetString(f.Name);
        if (fname.StartsWith("NativeFieldInfoPtr_")) continue;
        var ft = f.DecodeSignature(prov, null);
        Console.WriteLine($"   F {ft} {fname}");
    }
    foreach (var ph in t.GetProperties())
    {
        var p = md.GetPropertyDefinition(ph);
        var sig = p.DecodeSignature(prov, null);
        Console.WriteLine($"   P {sig.ReturnType} {md.GetString(p.Name)}");
    }
    foreach (var mh in t.GetMethods())
    {
        var m = md.GetMethodDefinition(mh);
        var mname = md.GetString(m.Name);
        if (mname.StartsWith("get_") || mname.StartsWith("set_") || mname == ".cctor") continue;
        var sig = m.DecodeSignature(prov, null);
        var pnames = new List<string>();
        foreach (var parh in m.GetParameters()) { var par = md.GetParameter(parh); if (par.SequenceNumber > 0) pnames.Add(md.GetString(par.Name)); }
        var ps = new List<string>();
        for (int i = 0; i < sig.ParameterTypes.Length; i++) ps.Add(sig.ParameterTypes[i] + " " + (i < pnames.Count ? pnames[i] : "p" + i));
        var st = (m.Attributes & System.Reflection.MethodAttributes.Static) != 0 ? "static " : "";
        Console.WriteLine($"   M {st}{sig.ReturnType} {mname}({string.Join(", ", ps)})");
    }
}

static string TypeName(MetadataReader md, TypeDefinitionHandle h)
{
    var t = md.GetTypeDefinition(h);
    var n = md.GetString(t.Name);
    var ns = md.GetString(t.Namespace);
    if (!t.GetDeclaringType().IsNil) return TypeName(md, t.GetDeclaringType()) + "+" + n;
    return string.IsNullOrEmpty(ns) ? n : ns + "." + n;
}

class SigProvider : ISignatureTypeProvider<string, object>
{
    MetadataReader md; public SigProvider(MetadataReader m) { md = m; }
    public string Name(EntityHandle h) => h.Kind switch
    {
        HandleKind.TypeDefinition => GetTypeFromDefinition(md, (TypeDefinitionHandle)h, 0),
        HandleKind.TypeReference => GetTypeFromReference(md, (TypeReferenceHandle)h, 0),
        HandleKind.TypeSpecification => GetTypeFromSpecification(md, null, (TypeSpecificationHandle)h, 0),
        _ => "?"
    };
    public string GetArrayType(string e, ArrayShape s) => e + "[,]";
    public string GetByReferenceType(string e) => "ref " + e;
    public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr";
    public string GetGenericInstantiation(string g, ImmutableArray<string> a) => g + "<" + string.Join(",", a) + ">";
    public string GetGenericMethodParameter(object c, int i) => "!!" + i;
    public string GetGenericTypeParameter(object c, int i) => "!" + i;
    public string GetModifiedType(string m, string u, bool r) => u;
    public string GetPinnedType(string e) => e;
    public string GetPointerType(string e) => e + "*";
    public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString().ToLower();
    public string GetSZArrayType(string e) => e + "[]";
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) { var t = r.GetTypeDefinition(h); return r.GetString(t.Name); }
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) { var t = r.GetTypeReference(h); return r.GetString(t.Name); }
    public string GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k) => r.GetTypeSpecification(h).DecodeSignature(this, c);
}
