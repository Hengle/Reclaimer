﻿using Adjutant.Blam.Common;
using Adjutant.Spatial;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Dds;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Halo2
{
    public class bitmap : IBitmap
    {
        private readonly ICacheFile cache;
        private readonly IIndexItem item;

        public bitmap(ICacheFile cache, IIndexItem item)
        {
            this.cache = cache;
            this.item = item;
        }

        [Offset(60)]
        [MinVersion((int)CacheType.Halo2Xbox)] //ignore h2b
        public BlockCollection<SequenceBlock> Sequences { get; set; }

        [Offset(96, MaxVersion = (int)CacheType.Halo2Xbox)]
        [Offset(68, MinVersion = (int)CacheType.Halo2Xbox)]
        public BlockCollection<BitmapDataBlock> Bitmaps { get; set; }

        #region IBitmap

        private static readonly CubemapLayout Halo2CubeLayout = new CubemapLayout
        {
            Face1 = CubemapFace.Right,
            Face2 = CubemapFace.Left,
            Face3 = CubemapFace.Back,
            Face4 = CubemapFace.Front,
            Face5 = CubemapFace.Top,
            Face6 = CubemapFace.Bottom,
            Orientation1 = RotateFlipType.Rotate270FlipNone,
            Orientation2 = RotateFlipType.Rotate90FlipNone,
            Orientation3 = RotateFlipType.Rotate180FlipNone,
            Orientation6 = RotateFlipType.Rotate180FlipNone
        };

        string IBitmap.SourceFile => item.CacheFile.FileName;

        int IBitmap.Id => item.Id;

        string IBitmap.Name => item.FullPath;

        string IBitmap.Class => item.ClassName;

        int IBitmap.SubmapCount => Bitmaps.Count;

        CubemapLayout IBitmap.CubeLayout => Halo2CubeLayout;

        public DdsImage ToDds(int index)
        {
            if (index < 0 || index >= Bitmaps.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var submap = Bitmaps[index];
            var data = submap.Lod0Pointer.ReadData(submap.Lod0Size);
            var frameCount = ((IBitmapData)submap).FrameCount;

            //Halo2 has all the lod mips in the same resource data as the main lod.
            //this means that for cubemaps each face will be separated by mips, so we
            //need to make sure the main lods are contiguous and discard additional data.
            var mip0Size = submap.Width * submap.Height * submap.BitmapFormat.Bpp() / 8;
            if (frameCount > 1)
            {
                var mipsSize = submap.Lod0Size / frameCount;
                for (int i = 1; i < frameCount; i++)
                    Array.Copy(data, i * mipsSize, data, i * mip0Size, mip0Size);
            }

            //get rid of additional mipmap data
            Array.Resize(ref data, mip0Size * frameCount);

            int virtualWidth, virtualHeight;
            virtualWidth = submap.Width;
            virtualHeight = submap.Height * frameCount;

            if (submap.Flags.HasFlag(BitmapFlags.Swizzled))
            {
                var unitSize = submap.BitmapFormat.LinearUnitSize();
                data = TextureUtils.Swizzle(data, virtualWidth, virtualHeight, 1, unitSize);
            }

            var format = submap.BitmapFormat;
            if (format == TextureFormat.P8_bump)
            {
                var indices = data;
                data = new byte[indices.Length * 4];
                format = TextureFormat.A8R8G8B8;

                for (int i = 0; i < indices.Length; i++)
                    Array.Copy(Properties.Resources.Halo2BumpPalette, indices[i] * 4, data, i * 4, 4);
            }

            return TextureUtils.GetDds(submap.Height, submap.Width, format, submap.BitmapType == TextureType.CubeMap, data);
        }

        #endregion
    }

    [FixedSize(60)]
    public class SequenceBlock
    {
        [Offset(0)]
        [NullTerminated(Length = 32)]
        public string Name { get; set; }

        [Offset(32)]
        public short FirstSubmapIndex { get; set; }

        [Offset(34)]
        public short BitmapCount { get; set; }

        [Offset(52)]
        public BlockCollection<SpriteBlock> Sprites { get; set; }
    }

    [FixedSize(32)]
    public class SpriteBlock
    {
        [Offset(0)]
        public short SubmapIndex { get; set; }

        [Offset(8)]
        public float Left { get; set; }

        [Offset(12)]
        public float Right { get; set; }

        [Offset(16)]
        public float Top { get; set; }

        [Offset(20)]
        public float Bottom { get; set; }

        [Offset(24)]
        public RealVector2D RegPoint { get; set; }
    }

    [FixedSize(116)]
    public class BitmapDataBlock : IBitmapData
    {
        private readonly ICacheFile cache;

        public BitmapDataBlock(ICacheFile cache)
        {
            this.cache = cache;
        }

        [Offset(0)]
        [FixedLength(4)]
        public string Class { get; set; }

        [Offset(4)]
        public short Width { get; set; }

        [Offset(6)]
        public short Height { get; set; }

        [Offset(8)]
        public short Depth { get; set; }

        [Offset(10)]
        public TextureType BitmapType { get; set; }

        [Offset(12)]
        public TextureFormat BitmapFormat { get; set; }

        [Offset(14)]
        public BitmapFlags Flags { get; set; }

        [Offset(16)]
        public short RegX { get; set; }

        [Offset(18)]
        public short RegY { get; set; }

        [Offset(20)]
        public short MipmapCount { get; set; }

        [Offset(28)]
        public DataPointer Lod0Pointer { get; set; }

        [Offset(32)]
        public DataPointer Lod1Pointer { get; set; }

        [Offset(36)]
        public DataPointer Lod2Pointer { get; set; }

        [Offset(52)]
        public int Lod0Size { get; set; }

        [Offset(56)]
        public int Lod1Size { get; set; }

        [Offset(60)]
        public int Lod2Size { get; set; }

        #region IBitmapData

        ByteOrder IBitmapData.ByteOrder => cache.ByteOrder;
        bool IBitmapData.UsesPadding => false;
        MipmapLayout IBitmapData.CubeMipLayout => MipmapLayout.Fragmented;
        MipmapLayout IBitmapData.ArrayMipLayout => MipmapLayout.None;

        int IBitmapData.Width => Width;
        int IBitmapData.Height => Height;
        int IBitmapData.Depth => BitmapType == TextureType.Texture3D ? Depth : 1;
        int IBitmapData.MipmapCount => MipmapCount;
        int IBitmapData.FrameCount => BitmapType == TextureType.CubeMap ? 6 : Depth;

        object IBitmapData.BitmapFormat => BitmapFormat;
        object IBitmapData.BitmapType => BitmapType;

        bool IBitmapData.Swizzled => Flags.HasFlag(BitmapFlags.Swizzled);

        #endregion
    }

    [Flags]
    public enum BitmapFlags : short
    {
        Swizzled = 8
    }

    public enum TextureType : short
    {
        Texture2D = 0,
        Texture3D = 1,
        CubeMap = 2
    }

    public enum TextureFormat : short
    {
        A8 = 0,
        Y8 = 1,
        AY8 = 2,
        A8Y8 = 3,
        Unused4 = 4,
        Unused5 = 5,
        R5G6B5 = 6,
        Unused7 = 7,
        A1R5G5B5 = 8,
        A4R4G4B4 = 9,
        X8R8G8B8 = 10,
        A8R8G8B8 = 11,
        Unused12 = 12,
        Unused13 = 13,
        DXT1 = 14,
        DXT3 = 15,
        DXT5 = 16,
        P8_bump = 17,
        P8 = 18,
        ARGBFP32 = 19,
        RGBFP32 = 20,
        RGBFP16 = 21,
        U8V8 = 22
    }
}
