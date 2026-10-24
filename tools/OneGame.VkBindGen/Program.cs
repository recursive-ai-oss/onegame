// Generates the blittable Vulkan bindings used by OneGame.Gpu.Vulkan from the Khronos registry (vk.xml).
//
// Usage: VkBindGen <vk.xml> <api-list.txt> <output.cs>
//
// The api list names the commands and extra types the backend uses ("command vkFoo", "type VkBar"). Every struct, union,
// enum, handle and bitmask those names reach is emitted, and nothing else. Registry semantics followed here:
//  - enum values added by features and extensions (offset / bitpos / value) are merged into their enum
//    (Vulkan registry documentation, "Extension enumerant offset" rules: 1000000000 + (extnumber - 1) * 1000 + offset);
//  - a struct member named sType with a values= attribute gets that value from a parameterless constructor;
//  - array members become [InlineArray] helper structs so the generated code stays blittable and needs no fixed buffers.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

if (args.Length != 3)
{
    Console.Error.WriteLine("usage: VkBindGen <vk.xml> <api-list.txt> <output.cs>");
    return 2;
}

var gen = new Generator(XDocument.Load(args[0]).Root!);
foreach (var line in File.ReadAllLines(args[1]))
{
    var t = line.Split('#')[0].Trim();
    if (t.Length == 0)
    {
        continue;
    }

    var parts = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts[0] == "command")
    {
        gen.RequireCommand(parts[1]);
    }
    else if (parts[0] == "type")
    {
        gen.RequireType(parts[1]);
    }
    else
    {
        throw new InvalidOperationException("bad api list line: " + line);
    }
}

File.WriteAllText(args[2], gen.Emit());
return 0;

sealed class Generator
{
    static readonly Dictionary<string, string> Primitive = new()
    {
        ["void"] = "void", ["char"] = "byte", ["float"] = "float", ["double"] = "double",
        ["int"] = "int", ["uint8_t"] = "byte", ["int8_t"] = "sbyte", ["uint16_t"] = "ushort", ["int16_t"] = "short",
        ["uint32_t"] = "uint", ["int32_t"] = "int", ["uint64_t"] = "ulong", ["int64_t"] = "long",
        ["size_t"] = "nuint",
        // Platform types that only appear behind pointers or as opaque ids in the surface-creation structs.
        ["Display"] = "void", ["Window"] = "nuint", ["VisualID"] = "nuint", ["xcb_connection_t"] = "void",
        ["xcb_window_t"] = "uint", ["xcb_visualid_t"] = "uint",
        ["wl_display"] = "void", ["wl_surface"] = "void",
        ["HINSTANCE"] = "nint", ["HWND"] = "nint", ["HANDLE"] = "nint", ["HMONITOR"] = "nint", ["DWORD"] = "uint",
        ["LPCWSTR"] = "char*", ["SECURITY_ATTRIBUTES"] = "void",
    };

    static readonly HashSet<string> Keywords =
    [
        "object", "event", "base", "ref", "in", "out", "checked", "params", "string", "lock", "fixed", "operator", "namespace",
        "default", "abstract", "internal", "delegate", "is", "as", "new", "class", "struct", "this", "unchecked", "implicit",
    ];

    readonly XElement _root;
    readonly Dictionary<string, XElement> _structs = new();
    readonly Dictionary<string, XElement> _commands = new();
    readonly Dictionary<string, XElement> _enumDefs = new();
    readonly Dictionary<string, XElement> _handles = new();
    readonly Dictionary<string, XElement> _bitmasks = new();
    readonly Dictionary<string, string> _alias = new();
    readonly Dictionary<string, string> _basetypes = new();
    readonly Dictionary<string, long> _constants = new();
    readonly Dictionary<string, List<(string Name, long Value)>> _enumValues = new();
    readonly HashSet<string> _reqStructs = [];
    readonly HashSet<string> _reqEnums = [];
    readonly HashSet<string> _reqHandles = [];
    readonly List<string> _reqCommands = [];
    readonly SortedSet<(string Type, int Count)> _arrays = new();

    public Generator(XElement root)
    {
        _root = root;
        foreach (var t in root.Elements("types").Elements("type"))
        {
            var name = (string?)t.Attribute("name") ?? (string?)t.Element("name");
            if (name is null)
            {
                continue;
            }

            var alias = (string?)t.Attribute("alias");
            if (alias is not null)
            {
                _alias[name] = alias;
                continue;
            }

            switch ((string?)t.Attribute("category"))
            {
                case "struct":
                case "union":
                    _structs[name] = t;
                    break;
                case "handle":
                    _handles[name] = t;
                    break;
                case "bitmask":
                    _bitmasks[name] = t;
                    break;
                case "basetype":
                    var under = (string?)t.Element("type");
                    if (under is not null)
                    {
                        _basetypes[name] = under;
                    }

                    break;
            }
        }

        foreach (var c in root.Elements("commands").Elements("command"))
        {
            var alias = (string?)c.Attribute("alias");
            if (alias is not null)
            {
                _alias[(string)c.Attribute("name")!] = alias;
            }
            else
            {
                _commands[(string)c.Element("proto")!.Element("name")!] = c;
            }
        }

        foreach (var e in root.Elements("enums"))
        {
            var name = (string)e.Attribute("name")!;
            if ((string?)e.Attribute("type") == "constants")
            {
                foreach (var v in e.Elements("enum").Where(x => x.Attribute("value") is not null && x.Attribute("alias") is null))
                {
                    var text = (string)v.Attribute("value")!;
                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    {
                        _constants[(string)v.Attribute("name")!] = n;
                    }
                }

                continue;
            }

            _enumDefs[name] = e;
            var list = new List<(string, long)>();
            foreach (var v in e.Elements("enum").Where(x => x.Attribute("alias") is null))
            {
                list.Add(((string)v.Attribute("name")!, ParseValue(v, null)));
            }

            _enumValues[name] = list;
        }

        // Features and extensions add values to existing enums.
        foreach (var scope in root.Elements("feature").Concat(root.Elements("extensions").Elements("extension")))
        {
            if ((string?)scope.Attribute("supported") == "disabled")
            {
                continue;
            }

            var extNumber = (string?)scope.Attribute("number");
            foreach (var v in scope.Elements("require").Elements("enum"))
            {
                var extends = (string?)v.Attribute("extends");
                if (extends is null || v.Attribute("alias") is not null || !_enumValues.TryGetValue(extends, out var list))
                {
                    continue;
                }

                var name = (string)v.Attribute("name")!;
                if (list.Any(x => x.Name == name))
                {
                    continue;
                }

                list.Add((name, ParseValue(v, extNumber)));
            }
        }
    }

    static long ParseValue(XElement v, string? parentExtension)
    {
        var bitpos = (string?)v.Attribute("bitpos");
        if (bitpos is not null)
        {
            return 1L << int.Parse(bitpos, CultureInfo.InvariantCulture);
        }

        var offset = (string?)v.Attribute("offset");
        if (offset is not null)
        {
            var ext = int.Parse((string?)v.Attribute("extnumber") ?? parentExtension!, CultureInfo.InvariantCulture);
            long value = 1000000000L + (ext - 1) * 1000L + int.Parse(offset, CultureInfo.InvariantCulture);
            return (string?)v.Attribute("dir") == "-" ? -value : value;
        }

        var text = (string?)v.Attribute("value") ?? "0";
        if (text.StartsWith("0x", StringComparison.Ordinal))
        {
            return long.Parse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return long.Parse(text, CultureInfo.InvariantCulture);
    }

    string Resolve(string name) => _alias.TryGetValue(name, out var a) ? Resolve(a) : name;

    public void RequireCommand(string name)
    {
        name = Resolve(name);
        if (!_commands.TryGetValue(name, out var c))
        {
            throw new InvalidOperationException("unknown command " + name);
        }

        if (_reqCommands.Contains(name))
        {
            return;
        }

        _reqCommands.Add(name);
        foreach (var p in c.Elements("param"))
        {
            RequireType((string)p.Element("type")!);
        }

        RequireType((string)c.Element("proto")!.Element("type")!);
    }

    public void RequireType(string name)
    {
        name = Resolve(name);
        if (Primitive.ContainsKey(name) || name.StartsWith("PFN_", StringComparison.Ordinal))
        {
            return;
        }

        if (_basetypes.TryGetValue(name, out var under))
        {
            RequireType(under);
            return;
        }

        if (_handles.ContainsKey(name))
        {
            _reqHandles.Add(name);
        }
        else if (_bitmasks.TryGetValue(name, out var bm))
        {
            var bits = (string?)bm.Attribute("requires") ?? (string?)bm.Attribute("bitvalues");
            if (bits is not null)
            {
                RequireType(bits);
            }
        }
        else if (_enumDefs.ContainsKey(name))
        {
            _reqEnums.Add(name);
        }
        else if (_structs.TryGetValue(name, out var s))
        {
            if (_reqStructs.Add(name))
            {
                foreach (var m in s.Elements("member"))
                {
                    RequireType((string)m.Element("type")!);
                }
            }
        }
        else
        {
            throw new InvalidOperationException("unknown type " + name);
        }
    }

    // Maps a registry type name to the C# type used for it (not counting pointer levels).
    string CsType(string name)
    {
        name = Resolve(name);
        if (Primitive.TryGetValue(name, out var p))
        {
            return p;
        }

        if (name.StartsWith("PFN_", StringComparison.Ordinal))
        {
            return "nint";
        }

        if (_basetypes.TryGetValue(name, out var under))
        {
            return CsType(under);
        }

        if (_bitmasks.TryGetValue(name, out var bm))
        {
            var bits = (string?)bm.Attribute("requires") ?? (string?)bm.Attribute("bitvalues");
            if (bits is not null)
            {
                return Resolve(bits);
            }

            return (string?)bm.Element("type") == "VkFlags64" ? "ulong" : "uint";
        }

        return name;
    }

    sealed record Field(string Type, string Name, int Array);

    Field ParseField(XElement m, bool inStruct)
    {
        var type = CsType((string)m.Element("type")!);
        var nameEl = m.Element("name")!;
        var name = (string)nameEl;

        // Text between </type> and <name> carries the pointer stars ("const void* ").
        var between = new StringBuilder();
        var node = m.Element("type")!.NextNode;
        while (node is not null && node != nameEl)
        {
            if (node is XText tx)
            {
                between.Append(tx.Value);
            }

            node = node.NextNode;
        }

        var stars = between.ToString().Count(c => c == '*');
        var after = new StringBuilder();
        for (var n = nameEl.NextNode; n is not null; n = n.NextNode)
        {
            if (n is XText tx)
            {
                after.Append(tx.Value);
            }
            else if (n is XElement e && e.Name == "enum")
            {
                after.Append((string)e);
            }
        }

        var arrayText = after.ToString().Trim();
        var array = 0;
        if (arrayText.StartsWith('['))
        {
            var inner = arrayText.Trim('[', ']');
            array = int.TryParse(inner, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n1) ? n1 : (int)_constants[inner];
        }

        if (array > 0 && !inStruct)
        {
            stars++;
            array = 0;
        }

        if (array > 0)
        {
            _arrays.Add((type, array));
            type = ArrayName(type, array);
        }

        type += new string('*', stars);
        if (Keywords.Contains(name))
        {
            name = "@" + name;
        }

        return new Field(type, name, array);
    }

    static string ArrayName(string type, int count) => "Array" + count + "_" + type.Replace("*", "Ptr", StringComparison.Ordinal);

    static string Pascal(string token)
    {
        var split = Regex.Match(token, "^([A-Z]{2,})([0-9]+)$");
        if (split.Success)
        {
            return Pascal(split.Groups[1].Value) + split.Groups[2].Value;
        }

        if (token.Any(char.IsDigit))
        {
            return token;
        }

        return token.Length == 0 ? token : char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant();
    }

    static string EnumPrefix(string typeName, bool flagBits)
    {
        var n = typeName;
        if (n.StartsWith("Vk", StringComparison.Ordinal))
        {
            n = n[2..];
        }

        var tag = "";
        var m = Regex.Match(n, "(KHR|EXT)$");
        if (m.Success)
        {
            tag = m.Value;
            n = n[..^tag.Length];
        }

        var digits = "";
        if (flagBits)
        {
            var fm = Regex.Match(n, @"FlagBits(\d*)$");
            if (fm.Success)
            {
                digits = fm.Groups[1].Value;
                n = n[..fm.Index];
            }
        }

        var snake = Regex.Replace(n, "(?<=[a-z0-9])(?=[A-Z])", "_").ToUpperInvariant();
        return "VK_" + snake + "_" + (digits.Length > 0 ? digits + "_" : "");
    }

    static string MemberName(string value, string prefix, bool flagBits)
    {
        var v = value;
        v = Regex.Replace(v, "_(KHR|EXT)$", "");
        if (flagBits)
        {
            v = Regex.Replace(v, "_BIT$", "");
        }

        if (v.StartsWith(prefix, StringComparison.Ordinal) && v.Length > prefix.Length)
        {
            v = v[prefix.Length..];
        }
        else if (v.StartsWith("VK_", StringComparison.Ordinal))
        {
            v = v[3..];
        }

        var name = string.Concat(v.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Pascal));
        return char.IsDigit(name[0]) ? "_" + name : name;
    }

    public string Emit()
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Produced by tools/OneGame.VkBindGen from the Khronos Vulkan registry (vk.xml). Do not edit.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("namespace OneGame.Gpu.Vulkan.Interop;");
        sb.AppendLine();

        foreach (var h in _reqHandles.Order(StringComparer.Ordinal))
        {
            var dispatchable = ((string)_handles[h].Element("type")!) == "VK_DEFINE_HANDLE";
            var under = dispatchable ? "nint" : "ulong";
            sb.AppendLine($"internal readonly struct {h}({under} value) {{ public readonly {under} Value = value; public bool IsNull => Value == 0; }}");
        }

        sb.AppendLine();
        foreach (var e in _reqEnums.Order(StringComparer.Ordinal))
        {
            EmitEnum(sb, e);
        }

        // Bitmask types with no flag-bits enum (reserved for future use) are plain uint/ulong and need no declaration.
        var structOrder = new List<string>();
        foreach (var s in _reqStructs.Order(StringComparer.Ordinal))
        {
            structOrder.Add(s);
        }

        var structText = new StringBuilder();
        foreach (var s in structOrder)
        {
            EmitStruct(structText, s);
        }

        foreach (var (type, count) in _arrays)
        {
            sb.AppendLine($"[InlineArray({count})] internal struct {ArrayName(type, count)} {{ public {type} Element0; }}");
        }

        sb.Append(structText);
        EmitCommands(sb);
        return sb.ToString();
    }

    void EmitEnum(StringBuilder sb, string name)
    {
        var def = _enumDefs[name];
        var bitmask = (string?)def.Attribute("type") == "bitmask";
        var wide = (string?)def.Attribute("bitwidth") == "64";
        var prefix = EnumPrefix(name, bitmask);
        var under = wide ? "ulong" : bitmask ? "uint" : "int";
        if (bitmask)
        {
            sb.AppendLine("[Flags]");
        }

        sb.AppendLine($"internal enum {name} : {under}");
        sb.AppendLine("{");
        var seen = new HashSet<string>();
        foreach (var (vname, value) in _enumValues[name])
        {
            if (vname.EndsWith("_MAX_ENUM", StringComparison.Ordinal))
            {
                continue;
            }

            var member = MemberName(vname, prefix, bitmask);
            if (!seen.Add(member))
            {
                continue;
            }

            var lit = bitmask && !wide ? ((uint)value).ToString(CultureInfo.InvariantCulture)
                : wide ? ((ulong)value).ToString(CultureInfo.InvariantCulture) : ((int)value).ToString(CultureInfo.InvariantCulture);
            sb.AppendLine($"    {member} = {lit},");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    void EmitStruct(StringBuilder sb, string name)
    {
        var s = _structs[name];
        var union = (string?)s.Attribute("category") == "union";
        sb.AppendLine(union ? "[StructLayout(LayoutKind.Explicit)]" : "[StructLayout(LayoutKind.Sequential)]");
        sb.AppendLine($"internal unsafe struct {name}");
        sb.AppendLine("{");
        string? stype = null;
        foreach (var m in s.Elements("member"))
        {
            if ((string?)m.Attribute("api") == "vulkansc")
            {
                continue;
            }

            var f = ParseField(m, true);
            if (union)
            {
                sb.AppendLine("    [FieldOffset(0)]");
            }

            sb.AppendLine($"    public {f.Type} {f.Name};");
            if (f.Name == "sType" && m.Attribute("values") is { } v)
            {
                stype = MemberName((string)v, "VK_STRUCTURE_TYPE_", false);
            }
        }

        if (stype is not null)
        {
            sb.AppendLine($"    public {name}() {{ sType = VkStructureType.{stype}; }}");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    void EmitCommands(StringBuilder sb)
    {
        string Kind(string cmd)
        {
            var first = (string)_commands[cmd].Elements("param").First().Element("type")!;
            return first switch
            {
                "VkInstance" or "VkPhysicalDevice" => "Instance",
                "VkDevice" or "VkQueue" or "VkCommandBuffer" => "Device",
                _ => "Global",
            };
        }

        foreach (var kind in new[] { "Global", "Instance", "Device" })
        {
            var cmds = _reqCommands.Where(c => Kind(c) == kind && c != "vkGetInstanceProcAddr" && c != "vkGetDeviceProcAddr").Order(StringComparer.Ordinal).ToList();
            sb.AppendLine($"/// <summary>{kind}-level entry points, loaded through the loader's proc-address functions.</summary>");
            sb.AppendLine($"internal sealed unsafe class Vk{kind}Fns");
            sb.AppendLine("{");
            foreach (var c in cmds)
            {
                sb.AppendLine($"    public {Signature(c)} {c};");
            }

            var loader = kind == "Device" ? "delegate* unmanaged<nint, byte*, nint> get" : "delegate* unmanaged<nint, byte*, nint> get";
            sb.AppendLine($"    public void Load(nint handle, {loader})");
            sb.AppendLine("    {");
            foreach (var c in cmds)
            {
                sb.AppendLine($"        {c} = ({Signature(c)})get(handle, \"{c}\\0\"u8.GetPointer());");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        sb.AppendLine("internal static unsafe class Utf8Literal");
        sb.AppendLine("{");
        sb.AppendLine("    public static byte* GetPointer(this ReadOnlySpan<byte> literal) => (byte*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(literal));");
        sb.AppendLine("}");
    }

    string Signature(string cmd)
    {
        var c = _commands[cmd];
        var types = c.Elements("param").Where(p => (string?)p.Attribute("api") != "vulkansc").Select(p => ParseField(p, false).Type).ToList();
        types.Add(CsType((string)c.Element("proto")!.Element("type")!));
        return "delegate* unmanaged<" + string.Join(", ", types) + ">";
    }
}
