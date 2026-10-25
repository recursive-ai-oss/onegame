using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

/// <summary>A Vulkan instance with the adapters it exposes; shared by the devices created from it.</summary>
internal sealed unsafe class VulkanInstance
{
    internal sealed record AdapterEntry(VkPhysicalDevice Physical, GpuAdapterInfo Info, uint QueueFamily, bool HasSwapchain);

    const string ValidationLayer = "VK_LAYER_KHRONOS_validation";

    public VkInstance Handle;
    public readonly VkGlobalFns Global = new();
    public readonly VkInstanceFns Fns = new();
    public ValidationSink? Sink;
    public GCHandle SinkHandle;
    public VkDebugUtilsMessengerEXT Messenger;
    public bool SurfaceKhr, XlibKhr, WaylandKhr, Win32Khr, AndroidKhr;
    public readonly List<AdapterEntry> Adapters = [];
    int _refs = 1;

    public static bool EnvFlag(string name) => Environment.GetEnvironmentVariable(name) is "1" or "true";

    public static VulkanInstance Create(GpuDeviceOptions options)
    {
        if (!VulkanLoader.IsAvailable())
        {
            throw new GpuUnsupportedException("the Vulkan loader library was not found on this machine");
        }

        var inst = new VulkanInstance();
        try
        {
            inst.Init(options);
            return inst;
        }
        catch
        {
            inst.Release();
            throw;
        }
    }

    static string Name(ReadOnlySpan<byte> utf8)
    {
        var n = utf8.IndexOf((byte)0);
        return Encoding.UTF8.GetString(n < 0 ? utf8 : utf8[..n]);
    }

    [UnmanagedCallersOnly]
    static nint GetProcThunk(nint instance, byte* name) => VulkanLoader.GetInstanceProcAddr(instance, name);

    void Init(GpuDeviceOptions options)
    {
        Global.Load(0, &GetProcThunk);

        uint apiVersion = 1u << 22;
        if (Global.vkEnumerateInstanceVersion != null)
        {
            uint v;
            Global.vkEnumerateInstanceVersion(&v);
            apiVersion = v;
        }

        if (apiVersion < (1u << 22 | 3u << 12))
        {
            throw new GpuUnsupportedException("the Vulkan loader is older than 1.3");
        }

        var available = new HashSet<string>();
        uint count = 0;
        Global.vkEnumerateInstanceExtensionProperties(null, &count, null);
        var extProps = new VkExtensionProperties[count];
        fixed (VkExtensionProperties* ep = extProps)
        {
            Global.vkEnumerateInstanceExtensionProperties(null, &count, ep);
        }

        foreach (var e in extProps)
        {
            available.Add(Name(e.extensionName));
        }

        var wantValidation = options.Validation || EnvFlag("ONEGAME_GPU_VALIDATION");
        var layerPresent = false;
        if (wantValidation)
        {
            uint lc = 0;
            Global.vkEnumerateInstanceLayerProperties(&lc, null);
            var layers = new VkLayerProperties[lc];
            fixed (VkLayerProperties* lp = layers)
            {
                Global.vkEnumerateInstanceLayerProperties(&lc, lp);
            }

            foreach (var l in layers)
            {
                layerPresent |= Name(l.layerName) == ValidationLayer;
            }

            if (layerPresent)
            {
                // The layer adds its own instance extensions (validation features among them).
                uint lec = 0;
                var lname = Encoding.UTF8.GetBytes(ValidationLayer + "\0");
                fixed (byte* ln = lname)
                {
                    Global.vkEnumerateInstanceExtensionProperties(ln, &lec, null);
                    var lext = new VkExtensionProperties[lec];
                    fixed (VkExtensionProperties* lp = lext)
                    {
                        Global.vkEnumerateInstanceExtensionProperties(ln, &lec, lp);
                    }

                    foreach (var e in lext)
                    {
                        available.Add(Name(e.extensionName));
                    }
                }
            }

            if (!layerPresent && options.RequireValidation)
            {
                throw new GpuUnsupportedException("the Vulkan validation layer (VK_LAYER_KHRONOS_validation) is not installed; set VK_ADD_LAYER_PATH to its location");
            }
        }

        var useValidation = wantValidation && layerPresent;
        var exts = new List<string>();
        void Want(string name, bool required = false)
        {
            if (available.Contains(name))
            {
                exts.Add(name);
            }
            else if (required)
            {
                throw new GpuUnsupportedException($"instance extension {name} is missing");
            }
        }

        if (available.Contains("VK_KHR_surface"))
        {
            Want("VK_KHR_surface");
            SurfaceKhr = true;
            if (available.Contains("VK_KHR_xlib_surface"))
            {
                Want("VK_KHR_xlib_surface");
                XlibKhr = true;
            }

            if (available.Contains("VK_KHR_wayland_surface"))
            {
                Want("VK_KHR_wayland_surface");
                WaylandKhr = true;
            }

            if (available.Contains("VK_KHR_win32_surface"))
            {
                Want("VK_KHR_win32_surface");
                Win32Khr = true;
            }

            if (available.Contains("VK_KHR_android_surface"))
            {
                Want("VK_KHR_android_surface");
                AndroidKhr = true;
            }
        }

        var debugUtils = useValidation && available.Contains("VK_EXT_debug_utils");
        if (debugUtils)
        {
            exts.Add("VK_EXT_debug_utils");
        }

        var validationFeatures = useValidation && available.Contains("VK_EXT_validation_features");
        if (validationFeatures)
        {
            exts.Add("VK_EXT_validation_features");
        }

        using var arena = new Arena();
        var extPtrs = arena.Alloc<nint>(exts.Count);
        for (var i = 0; i < exts.Count; i++)
        {
            extPtrs[i] = (nint)arena.Utf8(exts[i]);
        }

        var layerName = arena.Utf8(ValidationLayer);
        var app = new VkApplicationInfo
        {
            pApplicationName = arena.Utf8(options.ApplicationName),
            applicationVersion = 1,
            pEngineName = arena.Utf8("onegame"),
            engineVersion = 1,
            apiVersion = 1u << 22 | 3u << 12,
        };

        // Synchronization validation catches missing barriers that plain validation cannot see.
        var syncFeature = VkValidationFeatureEnableEXT.SynchronizationValidation;
        var vf = new VkValidationFeaturesEXT { enabledValidationFeatureCount = 1, pEnabledValidationFeatures = &syncFeature };
        var ici = new VkInstanceCreateInfo
        {
            pNext = validationFeatures ? &vf : null,
            pApplicationInfo = &app,
            enabledLayerCount = useValidation ? 1u : 0,
            ppEnabledLayerNames = useValidation ? &layerName : null,
            enabledExtensionCount = (uint)exts.Count,
            ppEnabledExtensionNames = (byte**)extPtrs,
        };
        VkInstance instance;
        var r = Global.vkCreateInstance(&ici, null, &instance);
        if (r < 0)
        {
            throw new GpuUnsupportedException($"vkCreateInstance failed: {r}");
        }

        Handle = instance;
        Fns.Load(instance.Value, &GetProcThunk);

        if (debugUtils)
        {
            Sink = new ValidationSink();
            SinkHandle = GCHandle.Alloc(Sink);
            var mci = new VkDebugUtilsMessengerCreateInfoEXT
            {
                messageSeverity = VkDebugUtilsMessageSeverityFlagBitsEXT.Warning | VkDebugUtilsMessageSeverityFlagBitsEXT.Error,
                messageType = VkDebugUtilsMessageTypeFlagBitsEXT.General | VkDebugUtilsMessageTypeFlagBitsEXT.Validation | VkDebugUtilsMessageTypeFlagBitsEXT.Performance,
                pfnUserCallback = (nint)(delegate* unmanaged<VkDebugUtilsMessageSeverityFlagBitsEXT, VkDebugUtilsMessageTypeFlagBitsEXT, VkDebugUtilsMessengerCallbackDataEXT*, void*, uint>)&DebugCallback,
                pUserData = (void*)GCHandle.ToIntPtr(SinkHandle),
            };
            VkDebugUtilsMessengerEXT m;
            if (Fns.vkCreateDebugUtilsMessengerEXT(Handle, &mci, null, &m) == VkResult.Success)
            {
                Messenger = m;
            }
        }

        EnumerateAdapters();
    }

    [UnmanagedCallersOnly]
    static uint DebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT severity, VkDebugUtilsMessageTypeFlagBitsEXT types, VkDebugUtilsMessengerCallbackDataEXT* data, void* user)
    {
        var sink = (ValidationSink?)GCHandle.FromIntPtr((nint)user).Target;
        if (sink is not null && data != null && data->pMessage != null)
        {
            var text = Marshal.PtrToStringUTF8((nint)data->pMessage) ?? "";
            sink.Add($"[{severity}] {text}");
        }

        return 0;
    }

    void EnumerateAdapters()
    {
        uint n = 0;
        Fns.vkEnumeratePhysicalDevices(Handle, &n, null);
        var devices = new VkPhysicalDevice[n];
        fixed (VkPhysicalDevice* p = devices)
        {
            Fns.vkEnumeratePhysicalDevices(Handle, &n, p);
        }

        foreach (var pd in devices)
        {
            VkPhysicalDeviceProperties props;
            Fns.vkGetPhysicalDeviceProperties(pd, &props);
            if (props.apiVersion < (1u << 22 | 3u << 12))
            {
                continue;
            }

            uint qn = 0;
            Fns.vkGetPhysicalDeviceQueueFamilyProperties(pd, &qn, null);
            var qprops = new VkQueueFamilyProperties[qn];
            fixed (VkQueueFamilyProperties* qp = qprops)
            {
                Fns.vkGetPhysicalDeviceQueueFamilyProperties(pd, &qn, qp);
            }

            var family = -1;
            var want = VkQueueFlagBits.Graphics | VkQueueFlagBits.Compute;
            for (var i = 0; i < qprops.Length; i++)
            {
                if ((qprops[i].queueFlags & want) == want)
                {
                    family = i;
                    break;
                }
            }

            if (family < 0)
            {
                continue;
            }

            uint en = 0;
            Fns.vkEnumerateDeviceExtensionProperties(pd, null, &en, null);
            var eprops = new VkExtensionProperties[en];
            var swapchain = false;
            fixed (VkExtensionProperties* ep = eprops)
            {
                Fns.vkEnumerateDeviceExtensionProperties(pd, null, &en, ep);
            }

            foreach (var e in eprops)
            {
                swapchain |= Name(e.extensionName) == "VK_KHR_swapchain";
            }

            var kind = props.deviceType switch
            {
                VkPhysicalDeviceType.DiscreteGpu => AdapterKind.Discrete,
                VkPhysicalDeviceType.IntegratedGpu => AdapterKind.Integrated,
                VkPhysicalDeviceType.VirtualGpu => AdapterKind.Virtual,
                VkPhysicalDeviceType.Cpu => AdapterKind.Software,
                _ => AdapterKind.Other,
            };
            var v = props.apiVersion;
            var info = new GpuAdapterInfo(Name(props.deviceName), kind, props.vendorID, props.deviceID, $"{v >> 22}.{(v >> 12) & 0x3ff}.{v & 0xfff}");
            Adapters.Add(new AdapterEntry(pd, info, (uint)family, swapchain));
        }
    }

    /// <summary>Chooses an adapter: by name or kind text when given, otherwise discrete, integrated, virtual, then software.</summary>
    public int Pick(string? spec)
    {
        spec = Environment.GetEnvironmentVariable("ONEGAME_GPU_ADAPTER") is { Length: > 0 } env ? env : spec;
        if (Adapters.Count == 0)
        {
            throw new GpuUnsupportedException("no Vulkan 1.3 adapter with graphics and compute queues was found");
        }

        if (!string.IsNullOrEmpty(spec))
        {
            for (var i = 0; i < Adapters.Count; i++)
            {
                var a = Adapters[i].Info;
                if (a.Name.Contains(spec, StringComparison.OrdinalIgnoreCase) || string.Equals(a.Kind.ToString(), spec, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            throw new GpuUnsupportedException($"no adapter matches '{spec}'");
        }

        AdapterKind[] order = [AdapterKind.Discrete, AdapterKind.Integrated, AdapterKind.Virtual, AdapterKind.Software, AdapterKind.Other];
        foreach (var k in order)
        {
            for (var i = 0; i < Adapters.Count; i++)
            {
                if (Adapters[i].Info.Kind == k)
                {
                    return i;
                }
            }
        }

        return 0;
    }

    public void AddRef() => _refs++;

    public void Release()
    {
        if (--_refs > 0)
        {
            return;
        }

        if (Handle.Value != 0)
        {
            if (Messenger.Value != 0)
            {
                Fns.vkDestroyDebugUtilsMessengerEXT(Handle, Messenger, null);
            }

            Fns.vkDestroyInstance(Handle, null);
            Handle = default;
        }

        if (SinkHandle.IsAllocated)
        {
            SinkHandle.Free();
        }
    }
}
