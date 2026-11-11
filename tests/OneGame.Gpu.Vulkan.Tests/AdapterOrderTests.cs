using OneGame.Gpu.Vulkan;

namespace OneGame.Gpu.Vulkan.Tests;

public class AdapterOrderTests
{
    static VulkanInstance.AdapterEntry E(string name, AdapterKind kind, uint vendor, uint device, string uuid = "") =>
        new(default, new GpuAdapterInfo(name, kind, vendor, device, "1.4.0"), 0, true, uuid);

    static readonly VulkanInstance.AdapterEntry[] Machine =
    [
        E("llvmpipe", AdapterKind.Software, 0x10005, 0),
        E("NVIDIA GeForce RTX 4070", AdapterKind.Discrete, 0x10de, 0x2786),
        E("AMD Radeon Graphics (RADV)", AdapterKind.Integrated, 0x1002, 0x164e),
        E("NVIDIA GeForce RTX 3060", AdapterKind.Discrete, 0x10de, 0x2503),
        E("Twin", AdapterKind.Discrete, 0x1002, 0x1, "bb"),
        E("Twin", AdapterKind.Discrete, 0x1002, 0x1, "aa"),
    ];

    [Fact]
    public void EveryEnumerationOrderSortsToTheSameList()
    {
        var expected = Machine.OrderBy(x => x, Comparer<VulkanInstance.AdapterEntry>.Create(VulkanInstance.Compare)).Select(x => x.Info.Name + x.Uuid).ToArray();
        var rng = new Random(7);
        for (var i = 0; i < 200; i++)
        {
            var shuffled = Machine.OrderBy(_ => rng.Next()).ToList();
            shuffled.Sort(VulkanInstance.Compare);
            Assert.Equal(expected, shuffled.Select(x => x.Info.Name + x.Uuid));
        }

        Assert.Equal("Twinaa", expected[0]);
        Assert.Equal("llvmpipe", expected[^1]);
    }

    [Fact]
    public void TheDefaultIsFirstInTheOrderAndAPinIsHonoured()
    {
        var list = Machine.ToList();
        list.Sort(VulkanInstance.Compare);
        Assert.Equal("Twin", list[VulkanInstance.PickFrom(list, null)].Info.Name);
        Assert.Equal("llvmpipe", list[VulkanInstance.PickFrom(list, "software")].Info.Name);
        Assert.Equal("NVIDIA GeForce RTX 4070", list[VulkanInstance.PickFrom(list, "4070")].Info.Name);
        Assert.Equal("NVIDIA GeForce RTX 3060", list[VulkanInstance.PickFrom(list, "3060")].Info.Name);
        Assert.Throws<GpuUnsupportedException>(() => VulkanInstance.PickFrom(list, "nothing"));
    }
}
