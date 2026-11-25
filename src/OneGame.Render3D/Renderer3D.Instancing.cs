using System.Runtime.InteropServices;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D;

// Instance batches: many copies of one static model drawn with one instanced draw per primitive. The copies are culled on the CPU against
// the camera and each shadow tile, and the survivors of every view are packed into one vertex buffer that the draws read at an offset.
public sealed partial class Renderer3D
{
    private struct BatchItem
    {
        public int Batch;
        public Primitive3D Primitive;
        public int Slot;
        public bool Flipped;
    }

    private readonly List<InstanceBatch3D> _activeBatches = [];
    private BatchItem[] _batchItems = new BatchItem[16];
    private (int First, int Count)[] _batchRanges = new (int, int)[MaxViews * 4];
    private InstanceGpu[] _instStaging = new InstanceGpu[1024];
    private GpuBuffer _instanceBuffer = null!;
    private int _instanceCapacity;
    private int _batchItemCount;
    private int _instCount;

    private int[] _paletteMap = new int[64];

    private int InstanceTotal { get; set; }

    private int InstanceVisible { get; set; }

    private int BatchDrawCalls { get; set; }

    private void InitInstancing()
    {
        _instanceCapacity = 1024;
        _instanceBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_instanceCapacity * InstanceStride, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "3d instances" }));
    }

    private void CollectBatches(Scene3D scene, int samples)
    {
        _ = samples;
        _activeBatches.Clear();
        _batchItemCount = 0;
        InstanceTotal = 0;
        foreach (var b in scene.Batches)
        {
            if (!b.Visible || b.Count == 0)
            {
                continue;
            }

            var bi = _activeBatches.Count;
            _activeBatches.Add(b);
            InstanceTotal += b.Count;
            var (min, max) = b.WorldBounds();
            _sceneMin = Vector3.Min(_sceneMin, min);
            _sceneMax = Vector3.Max(_sceneMax, max);

            var model = b.Model;
            var nodes = model.Source.Nodes;
            for (var k = 0; k < model.DrawNodes.Length; k++)
            {
                var n = model.DrawNodes[k];
                var prims = model.Meshes[nodes[n].Mesh]!;
                if (prims.Length == 0)
                {
                    continue;
                }

                var world = b.RestWorlds[n];
                var slot = _objectCount++;
                if (slot == _objectCapacity)
                {
                    GrowObjects(slot + 1);
                }

                bool flipped;
                if (b.Posed && nodes[n].Skin >= 0 && nodes[n].Skin < model.Source.Skins.Length)
                {
                    // A skinned node's palette already holds joint world * inverse bind (model space), so the object matrix is the identity
                    // and the node's own transform is ignored, as glTF 2.0 section 3.7.3 says for skinned meshes. The vertex shader adds the
                    // instance's palette start to this node's offset (Info.X).
                    _objects[slot] = new ObjectConstants { Model = Matrix4x4.Identity, NormalMat = Matrix4x4.Identity, Info = new UInt4 { X = (uint)b.PaletteOffsetOf(k), Y = 1 } };
                    flipped = false;
                }
                else
                {
                    Matrix4x4.Invert(world, out var inverse);
                    _objects[slot] = new ObjectConstants { Model = world, NormalMat = Matrix4x4.Transpose(inverse) };
                    flipped = world.GetDeterminant() < 0;
                }

                foreach (var p in prims)
                {
                    if (_batchItemCount == _batchItems.Length)
                    {
                        Array.Resize(ref _batchItems, _batchItems.Length * 2);
                    }

                    _batchItems[_batchItemCount++] = new BatchItem { Batch = bi, Primitive = p, Slot = slot, Flipped = flipped };
                    if (p.Material.Reflective)
                    {
                        _reflectiveDraws++;
                    }
                }
            }
        }

        if (_activeBatches.Count * MaxViews > _batchRanges.Length)
        {
            _batchRanges = new (int, int)[_activeBatches.Count * MaxViews * 2];
        }
    }

    // Tests every copy against the camera and each shadow tile and appends the visible ones to the staging array, one run per view.
    private void CullBatches(in Matrix4x4 viewProj)
    {
        ExtractPlanes(viewProj, _planes.AsSpan(0, 6));
        _instCount = 0;
        InstanceVisible = 0;
        BatchDrawCalls = 0;
        var views = 1 + _tileCount;
        if (_reflActive)
        {
            ExtractPlanes(_reflViewProj, _planes.AsSpan(ReflSlot * 6, 6));
        }

        for (var bi = 0; bi < _activeBatches.Count; bi++)
        {
            var b = _activeBatches[bi];
            var raw = b.Raw;
            var n = b.Count;
            if (b.Posed)
            {
                if (_paletteMap.Length < n)
                {
                    _paletteMap = new int[Math.Max(n, _paletteMap.Length * 2)];
                }

                Array.Fill(_paletteMap, -1, 0, n);
            }

            // The views are the camera, the shadow tiles, and last the mirror image when it is drawn.
            for (var vi = 0; vi <= views; vi++)
            {
                var v = vi < views ? vi : ReflSlot;
                if (vi == views && !_reflActive)
                {
                    break;
                }

                if (v > 0 && v != ReflSlot && !b.CastShadows)
                {
                    _batchRanges[(bi * MaxViews) + v] = (0, 0);
                    continue;
                }

                var first = _instCount;
                if (_instCount + n > _instStaging.Length)
                {
                    Array.Resize(ref _instStaging, Math.Max(_instCount + n, _instStaging.Length * 2));
                }

                var planes = _planes.AsSpan(v * 6, 6);
                for (var i = 0; i < n; i++)
                {
                    ref var it = ref raw[i];
                    var m = it.Transform;
                    b.SphereOf(in m, out var c, out var r);
                    if (!Inside(planes, c, r))
                    {
                        continue;
                    }

                    var palette = b.Posed ? PaletteFor(b, i) : 0;
                    _instStaging[_instCount++] = new InstanceGpu
                    {
                        Rx = new Vector4(m.M11, m.M21, m.M31, m.M41),
                        Ry = new Vector4(m.M12, m.M22, m.M32, m.M42),
                        Rz = new Vector4(m.M13, m.M23, m.M33, m.M43),
                        Color = it.Color,
                        Skin = new UInt4 { X = (uint)palette },
                    };
                }

                _batchRanges[(bi * MaxViews) + v] = (first, _instCount - first);
                if (v == 0)
                {
                    InstanceVisible += _instCount - first;
                }
            }
        }

        for (var i = 0; i < _batchItemCount; i++)
        {
            var visible = _batchRanges[_batchItems[i].Batch * MaxViews].Count;
            if (visible > 0)
            {
                BatchDrawCalls++;
                _drawnTriangles += (long)(_batchItems[i].Primitive.IndexCount / 3) * visible;
            }
        }

        if (_instCount > 0)
        {
            if (_instCount > _instanceCapacity)
            {
                _instanceCapacity = Math.Max(_instCount, _instanceCapacity * 2);
                _owned.Remove(_instanceBuffer);
                _deferred.Add(_instanceBuffer);
                _instanceBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_instanceCapacity * InstanceStride, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "3d instances" }));
            }

            Device.WriteBuffer(_instanceBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<InstanceGpu>(_instStaging, 0, _instCount)));
        }
    }

    // The first joint of copy i's palette in the joint buffer. The palette is copied in the first time any view needs the copy this frame
    // and reused by the rest (the camera and every shadow tile that sees it), so each posed copy costs one upload however many views draw it.
    private int PaletteFor(InstanceBatch3D b, int i)
    {
        var at = _paletteMap[i];
        if (at >= 0)
        {
            return at;
        }

        var vectors = b.PaletteVectors;
        var needed = (_jointUsed * 4) + vectors;
        if (needed > _jointRows.Length)
        {
            Array.Resize(ref _jointRows, Math.Max(_jointRows.Length * 2, needed));
        }

        b.CopyPalette(i, _jointRows.AsSpan(_jointUsed * 4, vectors));
        at = _jointUsed;
        _jointUsed += vectors / 4;
        _paletteMap[i] = at;
        return at;
    }

    private void DrawBatches(GpuRenderPass pass, PipeKind kind, int samples, int view, bool blend, bool mirror = false)
    {
        GpuGraphicsPipeline? pipeline = null;
        Material3D? material = null;
        Span<uint> offset = stackalloc uint[1];
        for (var i = 0; i < _batchItemCount; i++)
        {
            ref var it = ref _batchItems[i];
            var p = it.Primitive;
            if (p.Material.Blend != blend)
            {
                continue;
            }

            var range = _batchRanges[(it.Batch * MaxViews) + view];
            if (range.Count == 0 || (mirror && p.Material.Reflective))
            {
                continue;
            }

            // A mirrored view turns every triangle inside out on screen, so the winding is read the other way round.
            var wanted = ScenePipeline(kind, p.Material.Blend, p.Material.DoubleSided, it.Flipped ^ mirror, true, p.Material.Mask, samples);
            if (!ReferenceEquals(wanted, pipeline))
            {
                pipeline = wanted;
                pass.SetPipeline(pipeline);
            }

            offset[0] = (uint)it.Slot * GpuDevice.DynamicOffsetAlignment;
            pass.SetBindGroup(1, _set1!, offset);
            if (!ReferenceEquals(p.Material, material))
            {
                material = p.Material;
                pass.SetBindGroup(2, material.Group);
            }

            pass.SetVertexBuffer(0, p.Vertices);
            pass.SetVertexBuffer(1, _instanceBuffer, (ulong)range.First * InstanceStride);
            pass.SetIndexBuffer(p.Indices, p.IndexFormat);
            pass.DrawIndexed(p.IndexCount, (uint)range.Count);
        }
    }

    private void DrawBatchOutlines(GpuRenderPass pass, int samples)
    {
        Span<uint> offset = stackalloc uint[1];
        for (var i = 0; i < _batchItemCount; i++)
        {
            ref var it = ref _batchItems[i];
            var p = it.Primitive;
            var range = _batchRanges[it.Batch * MaxViews];
            if (p.Material.OutlineWidth <= 0f || p.Material.Blend || range.Count == 0)
            {
                continue;
            }

            pass.SetPipeline(ScenePipeline(PipeKind.Outline, false, p.Material.DoubleSided, it.Flipped, true, p.Material.Mask, samples));
            offset[0] = (uint)it.Slot * GpuDevice.DynamicOffsetAlignment;
            pass.SetBindGroup(1, _set1!, offset);
            pass.SetBindGroup(2, p.Material.Group);
            pass.SetVertexBuffer(0, p.Vertices);
            pass.SetVertexBuffer(1, _instanceBuffer, (ulong)range.First * InstanceStride);
            pass.SetIndexBuffer(p.Indices, p.IndexFormat);
            pass.DrawIndexed(p.IndexCount, (uint)range.Count);
        }
    }
}
