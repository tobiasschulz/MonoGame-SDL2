#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.IO;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;
using SDL2;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class Texture2D : Texture
	{
		#region Public Properties

		public int Width
		{
			get;
			private set;
		}

		public int Height
		{
			get;
			private set;
		}

		public Rectangle Bounds
		{
			get
			{
				return new Rectangle(0, 0, Width, Height);
			}
		}

		#endregion

		#region Public Constructors

		public Texture2D(
			GraphicsDevice graphicsDevice,
			int width,
			int height
		) : this(
			graphicsDevice,
			width,
			height,
			false,
			SurfaceFormat.Color
		) {
		}

		public Texture2D(
			GraphicsDevice graphicsDevice,
			int width,
			int height,
			bool mipmap,
			SurfaceFormat format
		) {
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException("graphicsDevice");
			}

			GraphicsDevice = graphicsDevice;
			Width = width;
			Height = height;
			LevelCount = mipmap ? CalculateMipLevels(width, height) : 1;

			Format = format;
			GetGLSurfaceFormat();

			Threading.ForceToMainThread(() =>
			{
				texture = new OpenGLDevice.OpenGLTexture(
					TextureTarget.Texture2D,
					Format,
					LevelCount > 1
				);
				if (((Width & (Width - 1)) != 0) || ((Height & (Height - 1)) != 0))
				{
					texture.WrapS.Set(TextureAddressMode.Clamp);
					texture.WrapT.Set(TextureAddressMode.Clamp);
				}
				OpenGLDevice.Instance.BindTexture(texture);

				if (	Format == SurfaceFormat.Dxt1 ||
					Format == SurfaceFormat.Dxt3 ||
					Format == SurfaceFormat.Dxt5	)
				{
					GL.CompressedTexImage2D(
						TextureTarget.Texture2D,
						0,
						glInternalFormat,
						Width,
						Height,
						0,
						((Width + 3) / 4) * ((Height + 3) / 4) * GetFormatSize(),
						IntPtr.Zero
					);
				}
				else
				{
					GL.TexImage2D(
						TextureTarget.Texture2D,
						0,
						glInternalFormat,
						Width,
						Height,
						0,
						glFormat,
						glType,
						IntPtr.Zero
					);
				}
				texture.Flush(true);
			});
		}

		#endregion

		#region Public SetData Methods

		public void SetData<T>(T[] data) where T : struct
		{
			SetData(
				0,
				null,
				data,
				0,
				data.Length
			);
		}

		public void SetData<T>(
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			SetData(
				0,
				null,
				data,
				startIndex,
				elementCount
			);
		}

		public void SetData<T>(
			int level,
			Rectangle? rect,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}

			int x, y, w, h;
			if (rect.HasValue)
			{
				x = rect.Value.X;
				y = rect.Value.Y;
				w = rect.Value.Width;
				h = rect.Value.Height;
			}
			else
			{
				x = 0;
				y = 0;
				w = Math.Max(Width >> level, 1);
				h = Math.Max(Height >> level, 1);
			}

			Threading.ForceToMainThread(() =>
			{
				GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

				try
				{
					int elementSizeInBytes = Marshal.SizeOf(typeof(T));
					int startByte = startIndex * elementSizeInBytes;
					IntPtr dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startByte);

					OpenGLDevice.Instance.BindTexture(texture);
					if (glFormat == (PixelFormat) All.CompressedTextureFormats)
					{
						int dataLength;
						if (elementCount > 0)
						{
							dataLength = elementCount * elementSizeInBytes;
						}
						else
						{
							dataLength = data.Length - startByte;
						}
						if (rect.HasValue)
						{
							GL.CompressedTexSubImage2D(
								TextureTarget.Texture2D,
								level,
								x,
								y,
								w,
								h,
								glFormat,
								dataLength,
								dataPtr
							);
						}
						else
						{
							GL.CompressedTexImage2D(
								TextureTarget.Texture2D,
								level,
								glInternalFormat,
								w,
								h,
								0,
								dataLength,
								dataPtr
							);
						}
					}
					else
					{
						// Set pixel alignment to match texel size in bytes
						GL.PixelStore(
							PixelStoreParameter.UnpackAlignment,
							GetFormatSize()
						);

						if (rect.HasValue)
						{
							GL.TexSubImage2D(
								TextureTarget.Texture2D,
								level,
								x,
								y,
								w,
								h,
								glFormat,
								glType,
								dataPtr
							);
						}
						else
						{
							GL.TexImage2D(
								TextureTarget.Texture2D,
								level,
								glInternalFormat,
								w,
								h,
								0,
								glFormat,
								glType,
								dataPtr
							);
						}

						// Return to default pixel alignment
						GL.PixelStore(
							PixelStoreParameter.UnpackAlignment,
							4
						);
					}

					GL.Finish();
				}
				finally
				{
					dataHandle.Free();
				}
			});
		}

		#endregion

		#region Public GetData Methods

		public void GetData<T>(T[] data) where T : struct
		{
			GetData(
				0,
				null,
				data,
				0,
				data.Length
			);
		}

		public void GetData<T>(
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			GetData(
				0,
				null,
				data,
				startIndex,
				elementCount
			);
		}

		public void GetData<T>(
			int level,
			Rectangle? rect,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			if (data == null || data.Length == 0)
			{
				throw new ArgumentException("data cannot be null");
			}
			if (data.Length < startIndex + elementCount)
			{
				throw new ArgumentException(
					"The data passed has a length of " + data.Length.ToString() +
					" but " + elementCount.ToString() + " pixels have been requested."
				);
			}

			OpenGLDevice.Instance.BindTexture(texture);

			if (glFormat == (PixelFormat) All.CompressedTextureFormats)
			{
				throw new NotImplementedException("GetData, CompressedTexture");
			}
			else if (rect == null)
			{
				// Just throw the whole texture into the user array.
				GL.GetTexImage(
					TextureTarget.Texture2D,
					0,
					glFormat,
					glType,
					data
				);
			}
			else
			{
				// Get the whole texture...
				T[] texData = new T[Width * Height];
				GL.GetTexImage(
					TextureTarget.Texture2D,
					0,
					glFormat,
					glType,
					texData
				);

				// Now, blit the rect region into the user array.
				Rectangle region = rect.Value;
				int curPixel = -1;
				for (int row = region.Y; row < region.Y + region.Height; row += 1)
				{
					for (int col = region.X; col < region.X + region.Width; col += 1)
					{
						curPixel += 1;
						if (curPixel < startIndex)
						{
							// If we're not at the start yet, just keep going...
							continue;
						}
						if (curPixel > elementCount)
						{
							// If we're past the end, we're done!
							return;
						}
						data[curPixel - startIndex] = texData[(row * Width) + col];
					}
				}
			}
		}

		#endregion

		#region Public Texture2D Save Methods

		public void SaveAsJpeg(Stream stream, int width, int height)
		{
			// dealwithit.png -flibit
			throw new NotSupportedException("It's 2014. Time to move on.");
		}

		public void SaveAsPng(Stream stream, int width, int height)
		{
			// Get the Texture2D pixels
			byte[] data = new byte[Width * Height * 4];
			GetData(data);

			// Create an SDL_Surface*, write the pixel data
			IntPtr surface = SDL.SDL_CreateRGBSurface(
				0,
				Width,
				Height,
				32,
				0x000000FF,
				0x0000FF00,
				0x00FF0000,
				0xFF000000
			);
			SDL.SDL_LockSurface(surface);
			Marshal.Copy(
				data,
				0,
				INTERNAL_getSurfacePixels(surface),
				data.Length
			);
			SDL.SDL_UnlockSurface(surface);
			data = null; // We're done with the original pixel data.

			// Blit to a scaled surface of the size we want, if needed.
			if (width != Width || height != Height)
			{
				IntPtr scaledSurface = SDL.SDL_CreateRGBSurface(
					0,
					width,
					height,
					32,
					0x000000FF,
					0x0000FF00,
					0x00FF0000,
					0xFF000000
				);
				SDL.SDL_BlitScaled(
					surface,
					IntPtr.Zero,
					scaledSurface,
					IntPtr.Zero
				);
				SDL.SDL_FreeSurface(surface);
				surface = scaledSurface;
			}

			// Create an SDL_RWops*, save PNG to RWops
			byte[] pngOut = new byte[width * height * 4]; // Max image size
			IntPtr dst = SDL.SDL_RWFromMem(pngOut, pngOut.Length);
			SDL_image.IMG_SavePNG_RW(surface, dst, 1);
			SDL.SDL_FreeSurface(surface); // We're done with the surface.

			// Get PNG size, write to Stream
			int size = (
				(pngOut[33] << 24) |
				(pngOut[34] << 16) |
				(pngOut[35] << 8) |
				(pngOut[36])
			) + 41 + 57; // 41 - header, 57 - footer
			stream.Write(pngOut, 0, size);
		}

		#endregion

		#region Public Static Texture2D Load Method

		public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
		{
			// Read the image data from the stream
			int width, height;
			byte[] pixels;
			TextureDataFromStreamEXT(stream, out width, out height, out pixels);

			// Create the Texture2D from the SDL_Surface
			Texture2D result = new Texture2D(
				graphicsDevice,
				width,
				height
			);
			result.SetData(pixels);
			return result;
		}

		#endregion

		#region Public Static Texture2D Extensions
		
		/// <summary>
		/// Loads image data from a given stream.
		/// </summary>
		/// <remarks>
		/// This is an extension of XNA 4 and is not compatible with XNA. It exists to help with dynamically reloading
		/// textures while games are running. Games can use this method to read a stream into memory and then call
		/// SetData on a texture with that data, rather than having to dispose the texture and recreate it entirely.
		/// </remarks>
		/// <param name="stream">The stream from which to read the image data.</param>
		/// <param name="width">Outputs the width of the image.</param>
		/// <param name="height">Outputs the height of the image.</param>
		/// <param name="pixels">Outputs the pixel data of the image, in non-premultiplied RGBA format.</param>
		public static void TextureDataFromStreamEXT(Stream stream, out int width, out int height, out byte[] pixels)
		{
			// Load the Stream into an SDL_RWops*
			byte[] mem = new byte[stream.Length];
			stream.Read(mem, 0, mem.Length);
			IntPtr rwops = SDL.SDL_RWFromMem(mem, mem.Length);

			// Load the SDL_Surface* from RWops, get the image data
			IntPtr surface = SDL_image.IMG_Load_RW(rwops, 1);
			surface = INTERNAL_convertSurfaceFormat(surface);
			width = INTERNAL_getSurfaceWidth(surface);
			height = INTERNAL_getSurfaceHeight(surface);
			pixels = new byte[width * height * 4]; // MUST be SurfaceFormat.Color!
			Marshal.Copy(INTERNAL_getSurfacePixels(surface), pixels, 0, pixels.Length);

			/* Ensure that the alpha pixels are... well, actual alpha.
			 * You think this looks stupid, but be assured: Your paint program is
			 * almost certainly even stupider.
			 * -flibit
			 */
			for (int i = 0; i < pixels.Length; i += 4)
			{
				if (pixels[i + 3] == 0)
				{
					pixels[i] = 0;
					pixels[i + 1] = 0;
					pixels[i + 2] = 0;
				}
			}
		}

		#endregion

		#region Private Static SDL_Surface Interop

		[StructLayout(LayoutKind.Sequential)]
		private struct SDL_Surface
		{
#pragma warning disable 0169
			UInt32 flags;
			public IntPtr format;
			public Int32 w;
			public Int32 h;
			Int32 pitch;
			public IntPtr pixels;
			IntPtr userdata;
			Int32 locked;
			IntPtr lock_data;
			SDL.SDL_Rect clip_rect;
			IntPtr map;
			Int32 refcount;
#pragma warning restore 0169
		}

		private static unsafe IntPtr INTERNAL_convertSurfaceFormat(IntPtr surface)
		{
			IntPtr result = surface;
			unsafe
			{
				SDL_Surface* surPtr = (SDL_Surface*) surface;
				SDL.SDL_PixelFormat* pixelFormatPtr = (SDL.SDL_PixelFormat*) surPtr->format;

				// SurfaceFormat.Color is SDL_PIXELFORMAT_ABGR8888
				if (pixelFormatPtr->format != SDL.SDL_PIXELFORMAT_ABGR8888)
				{
					// Create a properly formatted copy, free the old surface
					result = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
					SDL.SDL_FreeSurface(surface);
				}
			}
			return result;
		}

		private static unsafe IntPtr INTERNAL_getSurfacePixels(IntPtr surface)
		{
			IntPtr result;
			unsafe
			{
				SDL_Surface* surPtr = (SDL_Surface*) surface;
				result = surPtr->pixels;
			}
			return result;
		}

		private static unsafe int INTERNAL_getSurfaceWidth(IntPtr surface)
		{
			int result;
			unsafe
			{
				SDL_Surface* surPtr = (SDL_Surface*) surface;
				result = surPtr->w;
			}
			return result;
		}

		private static unsafe int INTERNAL_getSurfaceHeight(IntPtr surface)
		{
			int result;
			unsafe
			{
				SDL_Surface* surPtr = (SDL_Surface*) surface;
				result = surPtr->h;
			}
			return result;
		}

		#endregion
	}
}
