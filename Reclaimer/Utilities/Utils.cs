﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Utilities
{
    public static class Utils
    {
        /// <summary>
        /// Like <see cref="System.IO.Path.GetFileName"/> but allows invalid characters.
        /// </summary>
        public static string GetFileName(string path)
        {
            return path?.Split('\\').Last();
        }

        /// <summary>
        /// Like <see cref="System.IO.Path.GetFileNameWithoutExtension"/> but allows invalid characters.
        /// </summary>
        public static string GetFileNameWithoutExtension(string path)
        {
            if (path == null)
                return null;

            var fileName = path.Split('\\').Last();
            var index = fileName.LastIndexOf('.');

            if (index < 0)
                return fileName;
            else return fileName.Substring(0, index);
        }

        /// <summary>
        /// Replaces any characters in <paramref name="fileName"/> that are not valid file name characters.
        /// </summary>
        /// <param name="fileName">The file name to convert.</param>
        public static string GetSafeFileName(string fileName)
        {
            return string.Join("_", fileName.Split(Path.GetInvalidPathChars()));
        }

        /// <summary>
        /// Converts radian values to degree values.
        /// </summary>
        /// <param name="radians">The value in radians.</param>
        public static double RadToDeg(double radians)
        {
            return radians * (180d / Math.PI);
        }

        /// <summary>
        /// Converts degree values to radian values.
        /// </summary>
        /// <param name="degrees">The value in degrees.</param>
        public static double DegToRad(double degrees)
        {
            return degrees * (Math.PI / 180d);
        }
    }
}
