﻿using Adjutant.Blam.Common;
using Reclaimer.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Plugins
{
    public class BitmapViewer : Plugin
    {
        public override string Name => "Bitmap Viewer";

        public override bool CanOpenFile(object file, string key)
        {
            CacheType cacheType;
            if (!Enum.TryParse(key.Split('.').First(), out cacheType))
                return false;

            return file is IIndexItem && key.EndsWith(".bitm");
        }

        public override void OpenFile(object file, string key, IMultiPanelHost targetWindow)
        {
            var item = file as IIndexItem;
            var cacheType = (CacheType)Enum.Parse(typeof(CacheType), key.Split('.').First());
            var container = targetWindow.DocumentContainer;

            LogOutput($"Loading image: {item.FileName}");

            if (item.ClassCode == "bitm")
            {
                Adjutant.Utilities.IBitmap bitm;
                switch (cacheType)
                {
                    case CacheType.Halo1CE:
                    case CacheType.Halo1PC:
                        bitm = item.ReadMetadata<Adjutant.Blam.Halo1.bitmap>();
                        break;
                    case CacheType.Halo2Xbox:
                        bitm = item.ReadMetadata<Adjutant.Blam.Halo2.bitmap>();
                        break;
                    case CacheType.Halo3Beta:
                    case CacheType.Halo3Retail:
                    case CacheType.Halo3ODST:
                        bitm = item.ReadMetadata<Adjutant.Blam.Halo3.bitmap>();
                        break;
                    default: throw new NotSupportedException();
                }

                var viewer = new Controls.BitmapViewer();
                viewer.LoadImage(bitm, $"{item.FileName}.{item.ClassCode}");

                container.Items.Add(viewer);
                return;
            }

            LogOutput($"Loaded image: {item.FileName}");
        }
    }
}
