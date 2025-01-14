﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;

namespace BeebSpriter.Internal
{
    internal static class Extensions
    {
        /// <summary>
        /// Get the description field from the Enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToDescription<T>(this T source)
        {
            FieldInfo fieldInfo = source.GetType().GetField(source.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0) return attributes[0].Description;
            else return source.ToString();
        }

        /// <summary>
        /// Get the Enum from the Description field
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static T ToEnum<T>(string value)
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (DescriptionAttribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == value)
                    {
                        return (T)field.GetValue(null);
                    }
                }
            }

            throw new ArgumentException(string.Format("Not found '{0}' in Enum", nameof(value)));
        }

        /// <summary>
        /// Count to the number of different colours in a sprite
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="palette"></param>
        /// <param name="conv"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        public static int[] CountPalette(this Bitmap Image, BeebPalette palette)
        {
            int[] colourCount = new int[16];

            RectangleF cloneRect = new(0, 0, Image.Width, Image.Height);

            Bitmap cloneImage = Image.Clone(cloneRect, Image.PixelFormat);

            Bitmap newImage = new(cloneImage);

            Rectangle rect = new(0, 0, newImage.Width, newImage.Height);
            BitmapData bmpData = newImage.LockBits(rect, ImageLockMode.ReadWrite, newImage.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * newImage.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int counter = 0; counter < rgbValues.Length; counter += 4)
            {
                int b = rgbValues[counter];
                int g = rgbValues[counter + 1];
                int r = rgbValues[counter + 2];
                int a = rgbValues[counter + 3];

                Color origColour = Color.FromArgb(a, r, g, b);

                Color rgbColour = palette.FindClosestRGBColour(origColour);

                int index = palette.GetAcornColour(rgbColour);

                colourCount[index]++;
            }

            // Unlock the bits.
            newImage.UnlockBits(bmpData);
            newImage.Dispose();

            return colourCount;
        }

        /// <summary>
        /// Convert image to Acorn image
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="palette"></param>
        /// <param name="conv"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        public static Bitmap ToAcornFormat(this Bitmap Image, BeebPalette palette)
        {
            RectangleF cloneRect = new(0, 0, Image.Width, Image.Height);

            Bitmap cloneImage = Image.Clone(cloneRect, Image.PixelFormat);

            Bitmap newImage = new(cloneImage);

            Rectangle rect = new(0, 0, newImage.Width, newImage.Height);
            BitmapData bmpData = newImage.LockBits(rect, ImageLockMode.ReadWrite, newImage.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * newImage.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int counter = 0; counter < rgbValues.Length; counter += 4)
            {
                int b = rgbValues[counter];
                int g = rgbValues[counter + 1];
                int r = rgbValues[counter + 2];
                int a = rgbValues[counter + 3];

                Color originalColour = Color.FromArgb(a, r, g, b);

                Color rgbColour = palette.FindClosestRGBColour(originalColour);

                rgbValues[counter] = rgbColour.B;
                rgbValues[counter + 1] = rgbColour.G;
                rgbValues[counter + 2] = rgbColour.R;
                rgbValues[counter + 3] = rgbColour.A;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            newImage.UnlockBits(bmpData);

            return newImage;
        }

        /// <summary>
        /// Extract sprite from an image
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="palette"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static byte[] ExtractSprite(this Bitmap Image, BeebPalette palette, Rectangle rect)
        {
            byte[] data = new byte[rect.Width * rect.Height];

            BitmapData bmpData = Image.LockBits(rect, ImageLockMode.ReadWrite, Image.PixelFormat);

            // Get the address of the first line.
            IntPtr source = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * rect.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(source, rgbValues, 0, bytes);

            int counter = 0;
            for (int y = 0; y < rect.Height; y++)
            {
                for (int x = 0; x < rect.Width; x++)
                {
                    int ptr = (y * bmpData.Stride + (x * 4));

                    int pixel = 0;

                    if (ptr < rgbValues.Length)
                    {
                        int b = rgbValues[ptr];
                        int g = rgbValues[ptr + 1];
                        int r = rgbValues[ptr + 2];
                        int a = rgbValues[ptr + 3];

                        Color col = Color.FromArgb(a, r, g, b);

                        pixel = palette.GetAcornColour(col);
                    }

                    data[counter++] = (byte)pixel;
                }
            }

            // Unlock the bits.
            Image.UnlockBits(bmpData);

            return data;
        }
    }
};