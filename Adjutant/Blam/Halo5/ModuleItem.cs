﻿using Adjutant.Utilities;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Halo5
{
    [FixedSize(88)]
    public class ModuleItem
    {
        private readonly Module module;

        [Offset(0)]
        public int NameOffset { get; set; }

        [Offset(4)]
        public int ParentIndex { get; set; }

        [Offset(8)]
        public int ResourceCount { get; set; }

        [Offset(12)]
        public int ResourceIndex { get; set; }

        [Offset(16)]
        public int BlockCount { get; set; }

        [Offset(20)]
        public int BlockIndex { get; set; }

        [Offset(24)]
        public long DataOffset { get; set; }

        [Offset(32)]
        [StoreType(typeof(uint))]
        public long TotalCompressedSize { get; set; }

        [Offset(36)]
        [StoreType(typeof(uint))]
        public long TotalUncompressedSize { get; set; }

        [Offset(43)]
        public FileEntryFlags Flags { get; set; }

        [Offset(44)]
        public int GlobalTagId { get; set; }

        [Offset(48)]
        public long AssetId { get; set; }

        [Offset(56)]
        public long AssetChecksum { get; set; }

        [Offset(64)]
        [ByteOrder(ByteOrder.BigEndian)]
        public int ClassId { get; set; }

        [Offset(68)]
        [StoreType(typeof(uint))]
        public long UncompressedHeaderSize { get; set; }

        [Offset(72)]
        [StoreType(typeof(uint))]
        public long UncompressedTagDataSize { get; set; }

        [Offset(76)]
        [StoreType(typeof(uint))]
        public long UncompressedResourceDataSize { get; set; }

        [Offset(80)]
        public short HeaderBlockCount { get; set; }

        [Offset(82)]
        public short TagDataBlockCount { get; set; }

        [Offset(84)]
        public short ResourceBlockCount { get; set; }

        public string ClassCode => (ClassId == -1) ? null : Encoding.UTF8.GetString(BitConverter.GetBytes(ClassId));

        public string FullPath => module.Strings[NameOffset];

        public ModuleItem(Module module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            this.module = module;
        }

        private Block GetImpliedBlock()
        {
            return new Block
            {
                CompressedOffset = 0,
                CompressedSize = TotalCompressedSize,
                UncompressedOffset = 0,
                UncompressedSize = TotalUncompressedSize,
                Compressed = TotalUncompressedSize > TotalCompressedSize ? 1 : 0
            };
        }

        public DependencyReader CreateReader()
        {
            using (var reader = module.CreateReader())
            {
                IEnumerable<Block> blocks;
                if (BlockCount > 0)
                    blocks = module.Blocks.Skip(BlockIndex).Take(BlockCount);
                else
                {
                    if (TotalUncompressedSize == TotalCompressedSize)
                        return (DependencyReader)reader.CreateVirtualReader(module.DataAddress + DataOffset);
                    else blocks = Enumerable.Repeat(GetImpliedBlock(), 1);
                }

                var decompressed = new MemoryStream((int)blocks.Sum(b => b.UncompressedSize));
                foreach (var block in blocks)
                {
                    reader.Seek(module.DataAddress + DataOffset + block.CompressedOffset, SeekOrigin.Begin);

                    if (block.Compressed == 0)
                        decompressed.Write(reader.ReadBytes((int)block.UncompressedSize), 0, (int)block.UncompressedSize);
                    else
                    {
                        using (var zstream = new ZlibStream(decompressed, CompressionMode.Decompress, true))
                            zstream.Write(reader.ReadBytes((int)block.CompressedSize), 0, (int)block.CompressedSize);
                    }
                }

                decompressed.Position = 0;
                return module.CreateReader(decompressed);
            }
        }

        public override string ToString() => FullPath;
    }
}
