using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

/// <summary>How the GPU uses a resource in a command. Each use maps to a pipeline stage, an access type and an image layout.</summary>
internal enum Use
{
    CopySource,
    CopyDestination,
    Sampled,
    StorageRead,
    StorageWrite,
    ColorAttachment,
    DepthAttachment,
    Present,
    VertexBuffer,
    IndexBuffer,
    UniformBuffer,
    IndirectBuffer,
}

/// <summary>The last known GPU access to a resource, used to insert the barriers the portable API leaves implicit.</summary>
internal struct SyncState
{
    public VkImageLayout Layout;
    public VkPipelineStageFlagBits2 WriteStage;
    public VkAccessFlagBits2 WriteAccess;
    public VkPipelineStageFlagBits2 ReadStages;

    const VkPipelineStageFlagBits2 AllShaders = VkPipelineStageFlagBits2.VertexShader | VkPipelineStageFlagBits2.FragmentShader | VkPipelineStageFlagBits2.ComputeShader;

    public static void Describe(Use use, out VkPipelineStageFlagBits2 stage, out VkAccessFlagBits2 access, out VkImageLayout layout, out bool write)
    {
        write = false;
        switch (use)
        {
            case Use.CopySource:
                stage = VkPipelineStageFlagBits2.AllTransfer;
                access = VkAccessFlagBits2.TransferRead;
                layout = VkImageLayout.TransferSrcOptimal;
                break;
            case Use.CopyDestination:
                stage = VkPipelineStageFlagBits2.AllTransfer;
                access = VkAccessFlagBits2.TransferWrite;
                layout = VkImageLayout.TransferDstOptimal;
                write = true;
                break;
            case Use.Sampled:
                stage = AllShaders;
                access = VkAccessFlagBits2.ShaderSampledRead;
                layout = VkImageLayout.ShaderReadOnlyOptimal;
                break;
            case Use.StorageRead:
                stage = AllShaders;
                access = VkAccessFlagBits2.ShaderStorageRead;
                layout = VkImageLayout.General;
                break;
            case Use.StorageWrite:
                stage = AllShaders;
                access = VkAccessFlagBits2.ShaderStorageRead | VkAccessFlagBits2.ShaderStorageWrite;
                layout = VkImageLayout.General;
                write = true;
                break;
            case Use.ColorAttachment:
                stage = VkPipelineStageFlagBits2.ColorAttachmentOutput;
                access = VkAccessFlagBits2.ColorAttachmentRead | VkAccessFlagBits2.ColorAttachmentWrite;
                layout = VkImageLayout.ColorAttachmentOptimal;
                write = true;
                break;
            case Use.DepthAttachment:
                stage = VkPipelineStageFlagBits2.EarlyFragmentTests | VkPipelineStageFlagBits2.LateFragmentTests;
                access = VkAccessFlagBits2.DepthStencilAttachmentRead | VkAccessFlagBits2.DepthStencilAttachmentWrite;
                layout = VkImageLayout.DepthStencilAttachmentOptimal;
                write = true;
                break;
            case Use.Present:
                stage = VkPipelineStageFlagBits2.None;
                access = VkAccessFlagBits2.None;
                layout = VkImageLayout.PresentSrc;
                break;
            case Use.VertexBuffer:
                stage = VkPipelineStageFlagBits2.VertexAttributeInput;
                access = VkAccessFlagBits2.VertexAttributeRead;
                layout = VkImageLayout.Undefined;
                break;
            case Use.IndexBuffer:
                stage = VkPipelineStageFlagBits2.IndexInput;
                access = VkAccessFlagBits2.IndexRead;
                layout = VkImageLayout.Undefined;
                break;
            case Use.IndirectBuffer:
                stage = VkPipelineStageFlagBits2.DrawIndirect;
                access = VkAccessFlagBits2.IndirectCommandRead;
                layout = VkImageLayout.Undefined;
                break;
            default:
                stage = AllShaders;
                access = VkAccessFlagBits2.UniformRead;
                layout = VkImageLayout.Undefined;
                break;
        }
    }

    /// <summary>
    /// Decides whether moving to <paramref name="use"/> needs a barrier and updates the state. Returns the source scope of the barrier.
    /// Reads after reads in the same layout need nothing, except that a stage not yet covered by an earlier barrier needs to see the last write.
    /// </summary>
    public bool Transition(Use use, out VkPipelineStageFlagBits2 srcStage, out VkAccessFlagBits2 srcAccess, out VkPipelineStageFlagBits2 dstStage, out VkAccessFlagBits2 dstAccess, out VkImageLayout oldLayout, out VkImageLayout newLayout)
    {
        Describe(use, out dstStage, out dstAccess, out newLayout, out var write);
        oldLayout = Layout;
        var isImage = newLayout != VkImageLayout.Undefined;
        if (!isImage)
        {
            newLayout = Layout;
        }

        var layoutChange = isImage && Layout != newLayout;
        var hadWrite = WriteAccess != VkAccessFlagBits2.None || WriteStage != VkPipelineStageFlagBits2.None;

        if (!write && !layoutChange && (ReadStages & dstStage) == dstStage && (hadWrite || ReadStages != 0))
        {
            srcStage = 0;
            srcAccess = 0;
            return false;
        }

        if (!write && !layoutChange && !hadWrite && Layout == VkImageLayout.Undefined && !isImage)
        {
            // Buffer never written on the GPU: contents came from the host or earlier transfers that are ordered by submission.
            ReadStages |= dstStage;
            srcStage = 0;
            srcAccess = 0;
            return false;
        }

        srcStage = WriteStage | ReadStages;
        srcAccess = WriteAccess;
        if (srcStage == VkPipelineStageFlagBits2.None)
        {
            srcStage = VkPipelineStageFlagBits2.None;
        }

        if (write || layoutChange)
        {
            WriteStage = dstStage;
            WriteAccess = write ? dstAccess : VkAccessFlagBits2.None;
            ReadStages = write ? VkPipelineStageFlagBits2.None : dstStage;
        }
        else
        {
            ReadStages |= dstStage;
        }

        if (isImage)
        {
            Layout = newLayout;
        }

        return true;
    }
}
