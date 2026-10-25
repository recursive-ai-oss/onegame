using System.Runtime.InteropServices;
using OneGame.Gpu;

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

    private int InstanceTotal { get; set; }

    private int InstanceVisible { get; set; }

    private int BatchDrawCalls { get; set; }

    private void InitInstancing()
    {
        _instanceCapacity = 1024;
        _instanceBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_instanceCapacity * 64, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "3d instances" }));
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
            foreach (var n in model.DrawNodes)
            {
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

                Matrix4x4.Invert(world, out var inverse);
                _objects[slot] = new ObjectConstants { Model = world, NormalMat = Matrix4x4.Transpose(inverse) };
                var flipped = world.GetDeterminant() < 0;
                foreach (var p in prims)
                {
                    if (_batchItemCount == _batchItems.Length)
                    {
                        Array.Resize(ref _batchItems, _batchItems.Length * 2);
                    }

                    _batchItems[_batchItemCount++] = new BatchItem { Batch = bi, Primitive = p, Slot = slot, Flipped = flipped };
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
        for (var bi = 0; bi < _activeBatches.Count; bi++)
        {
            var b = _activeBatches[bi];
            var raw = b.Raw;
            var n = b.Count;
            for (var v = 0; v < views; v++)
            {
                if (v > 0 && !b.CastShadows)
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

                    _instStaging[_instCount++] = new InstanceGpu
                    {
                        Rx = new Vector4(m.M11, m.M21, m.M31, m.M41),
                        Ry = new Vector4(m.M12, m.M22, m.M32, m.M42),
                        Rz = new Vector4(m.M13, m.M23, m.M33, m.M43),
                        Color = it.Color,
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
                _instanceBuffer = Own(Device.CreateBuffer(new BufferDesc((ulong)_instanceCapacity * 64, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "3d instances" }));
            }

            Device.WriteBuffer(_instanceBuffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<InstanceGpu>(_instStaging, 0, _instCount)));
        }
    }

    private void DrawBatches(GpuRenderPass pass, PipeKind kind, int samples, int view, bool blend)
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
            if (range.Count == 0)
            {
                continue;
            }

            var wanted = ScenePipeline(kind, p.Material.Blend, p.Material.DoubleSided, it.Flipped, true, p.Material.Mask, samples);
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
            pass.SetVertexBuffer(1, _instanceBuffer, (ulong)range.First * 64);
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
            pass.SetVertexBuffer(1, _instanceBuffer, (ulong)range.First * 64);
            pass.SetIndexBuffer(p.Indices, p.IndexFormat);
            pass.DrawIndexed(p.IndexCount, (uint)range.Count);
        }
    }
}
