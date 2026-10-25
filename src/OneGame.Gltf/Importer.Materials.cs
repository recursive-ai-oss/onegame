using System.Globalization;

namespace OneGame.Gltf;

internal sealed partial class Importer
{
    private GltfMaterial[] ReadMaterials()
    {
        var count = _root.ArrayLength("materials", _limits.MaxMaterials);
        var result = new GltfMaterial[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = ReadMaterial(_root.Item("materials", i));
        }

        return result;
    }

    private static JObj? Ext(JObj o, string name) => o.Obj("extensions")?.Obj(name);

    private GltfMaterial ReadMaterial(JObj m)
    {
        var mat = new GltfMaterial
        {
            Name = m.Str("name"),
            EmissiveFactor = Vec3(m.Floats("emissiveFactor", 3), Vector3.Zero),
            NormalTexture = TexRef(m.Obj("normalTexture"), false),
            OcclusionTexture = TexRef(m.Obj("occlusionTexture"), false),
            EmissiveTexture = TexRef(m.Obj("emissiveTexture"), true),
            AlphaCutoff = m.Float("alphaCutoff", 0.5f),
            DoubleSided = m.Bool("doubleSided", false),
        };
        mat.NormalScale = m.Obj("normalTexture")?.Float("scale", 1) ?? 1;
        mat.OcclusionStrength = m.Obj("occlusionTexture")?.Float("strength", 1) ?? 1;
        mat.AlphaMode = m.Str("alphaMode", "OPAQUE") switch
        {
            "OPAQUE" => GltfAlphaMode.Opaque,
            "MASK" => GltfAlphaMode.Mask,
            "BLEND" => GltfAlphaMode.Blend,
            _ => throw m.Error("schema", "alphaMode", "must be OPAQUE, MASK or BLEND."),
        };
        if (m.Obj("pbrMetallicRoughness") is { } pbr)
        {
            var bc = pbr.Floats("baseColorFactor", 4);
            mat.BaseColorFactor = bc is null ? Vector4.One : new Vector4(bc[0], bc[1], bc[2], bc[3]);
            mat.BaseColorTexture = TexRef(pbr.Obj("baseColorTexture"), true);
            mat.MetallicFactor = pbr.Float("metallicFactor", 1);
            mat.RoughnessFactor = pbr.Float("roughnessFactor", 1);
            mat.MetallicRoughnessTexture = TexRef(pbr.Obj("metallicRoughnessTexture"), false);
        }

        if (Ext(m, "KHR_materials_emissive_strength") is { } es)
        {
            mat.EmissiveStrength = es.Float("emissiveStrength", 1);
        }

        mat.Unlit = Ext(m, "KHR_materials_unlit") is not null;
        if (Ext(m, "KHR_materials_ior") is { } ior)
        {
            mat.Ior = ior.Float("ior", 1.5f);
        }

        if (Ext(m, "KHR_materials_clearcoat") is { } cc)
        {
            var nt = cc.Obj("clearcoatNormalTexture");
            mat.Clearcoat = new ClearcoatInfo
            {
                Factor = cc.Float("clearcoatFactor", 0),
                Texture = TexRef(cc.Obj("clearcoatTexture"), false),
                RoughnessFactor = cc.Float("clearcoatRoughnessFactor", 0),
                RoughnessTexture = TexRef(cc.Obj("clearcoatRoughnessTexture"), false),
                NormalTexture = TexRef(nt, false),
                NormalScale = nt?.Float("scale", 1) ?? 1,
            };
        }

        if (Ext(m, "KHR_materials_transmission") is { } tr)
        {
            mat.Transmission = new TransmissionInfo { Factor = tr.Float("transmissionFactor", 0), Texture = TexRef(tr.Obj("transmissionTexture"), false) };
        }

        if (Ext(m, "KHR_materials_specular") is { } sp)
        {
            mat.Specular = new SpecularInfo
            {
                Factor = sp.Float("specularFactor", 1),
                Texture = TexRef(sp.Obj("specularTexture"), false),
                ColorFactor = Vec3(sp.Floats("specularColorFactor", 3), Vector3.One),
                ColorTexture = TexRef(sp.Obj("specularColorTexture"), true),
            };
        }

        if (Ext(m, "KHR_materials_sheen") is { } sh)
        {
            mat.Sheen = new SheenInfo
            {
                ColorFactor = Vec3(sh.Floats("sheenColorFactor", 3), Vector3.Zero),
                ColorTexture = TexRef(sh.Obj("sheenColorTexture"), true),
                RoughnessFactor = sh.Float("sheenRoughnessFactor", 0),
                RoughnessTexture = TexRef(sh.Obj("sheenRoughnessTexture"), false),
            };
        }

        if (Ext(m, "KHR_materials_volume") is { } vo)
        {
            mat.Volume = new VolumeInfo
            {
                ThicknessFactor = vo.Float("thicknessFactor", 0),
                ThicknessTexture = TexRef(vo.Obj("thicknessTexture"), false),
                AttenuationDistance = vo.Float("attenuationDistance", float.PositiveInfinity),
                AttenuationColor = Vec3(vo.Floats("attenuationColor", 3), Vector3.One),
            };
        }

        return mat;
    }

    private static Vector3 Vec3(float[]? v, Vector3 fallback) => v is null ? fallback : new Vector3(v[0], v[1], v[2]);

    /// <summary>Reads a texture reference and marks its image as colour (sRGB) or data (linear).</summary>
    private TextureRef? TexRef(JObj? info, bool color)
    {
        if (info is not { } t)
        {
            return null;
        }

        var index = t.ReqRef("index", _model.Textures.Length);
        var r = new TextureRef { Texture = index, TexCoord = t.Int("texCoord", 0, 0, 1000) };
        if (Ext(t, "KHR_texture_transform") is { } tt)
        {
            var offset = tt.Floats("offset", 2);
            var scale = tt.Floats("scale", 2);
            r.Transform = new TextureTransform(
                offset is null ? Vector2.Zero : new Vector2(offset[0], offset[1]),
                tt.Float("rotation", 0),
                scale is null ? Vector2.One : new Vector2(scale[0], scale[1]));
            // The extension may replace the UV set.
            if (tt.Has("texCoord"))
            {
                r.TexCoord = tt.Int("texCoord", r.TexCoord, 0, 1000);
            }
        }

        var source = _model.Textures[index].Source;
        if (source >= 0)
        {
            if (color)
            {
                _model.Images[source].UsedAsColor = true;
            }
            else
            {
                _model.Images[source].UsedAsData = true;
            }
        }

        return r;
    }
}
